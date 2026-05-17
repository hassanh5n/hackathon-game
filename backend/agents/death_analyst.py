import json
from models.combat_log import CombatLog
from services.ai_service import generate

# ─────────────────────────────────────────────
# DETERMINISTIC PATTERN ENRICHMENT
# No LLM — pure math from combat stats.
# Adds `frequency` and `death_context` to each pattern for
# the taunt generator to produce specific, concrete mockery.
# ─────────────────────────────────────────────
def _enrich_patterns(patterns: list, combat_log: CombatLog, death_cause: str) -> list:
    """
    Adds concrete behavioral data to each pattern dict.
    Called after LLM pattern identification — purely deterministic.
    """
    stats = combat_log.stats
    total_dodges = (
        stats.dodge_left_count + stats.dodge_right_count +
        stats.dodge_forward_count + stats.dodge_back_count
    )

    DEATH_CONTEXTS = {
        "PUNISH_LEFT_DODGE": (
            f"The warrior rolled left {stats.dodge_left_count} times. "
            f"They died {death_cause}."
        ),
        "PUNISH_RIGHT_DODGE": (
            f"The warrior rolled right {stats.dodge_right_count} times. "
            f"They died {death_cause}."
        ),
        "AGGRESS_AT_35PCT_HP": (
            f"The warrior retreated below {stats.retreat_hp_threshold:.0%} HP "
            "to use consumables. They died while doing so."
        ),
        "CHAIN_PUNISH_LIGHT": (
            f"The warrior used light attacks {stats.light_attack_ratio:.0%} of the time. "
            "They died mid-combo."
        ),
        "RANGE_PUNISH": (
            f"The warrior maintained an average distance of {stats.avg_distance_from_boss:.1f} units. "
            "They died playing it safe."
        ),
        "APPROACH_PUNISH": (
            "The warrior approached carelessly. "
            f"They died {death_cause}."
        ),
        "THROWABLE_BAIT": (
            f"The warrior used {stats.throwables_used.flask} flasks, "
            f"{stats.throwables_used.tablet} tablets, {stats.throwables_used.spear} spears. "
            "They died shortly after."
        ),
    }

    FREQUENCIES = {
        "PUNISH_LEFT_DODGE":  stats.dodge_left_count,
        "PUNISH_RIGHT_DODGE": stats.dodge_right_count,
        "AGGRESS_AT_35PCT_HP": 1,  # binary — either they did or didn't
        "CHAIN_PUNISH_LIGHT":  stats.light_attack_count,
        "RANGE_PUNISH":        1,
        "APPROACH_PUNISH":     1,
        "THROWABLE_BAIT":      (
            stats.throwables_used.flask +
            stats.throwables_used.tablet +
            stats.throwables_used.spear
        ),
    }

    enriched = []
    for p in patterns:
        name = p.get("name", "")
        p["frequency"] = FREQUENCIES.get(name, 0)
        p["death_context"] = DEATH_CONTEXTS.get(
            name,
            f"The warrior died from {death_cause}."
        )
        enriched.append(p)
    return enriched


async def analyze_death(combat_log: CombatLog, death_cause: str) -> dict:
    """
    Analyzes combat log to identify exploitable player patterns.
    Calls Groq with structured prompt.
    Returns dict with keys: patterns, reasoning_trace
    Prints full reasoning trace to console for demo purposes.
    """
    stats = combat_log.stats
    
    prompt = f"""
    You are analyzing a player's combat behavior in a boss fight game.
    
    Combat Statistics:
    - Dodge right: {stats.dodge_right_count} times
    - Dodge left: {stats.dodge_left_count} times  
    - Dodge forward: {stats.dodge_forward_count} times
    - Dodge back: {stats.dodge_back_count} times
    - Light attack ratio: {stats.light_attack_ratio:.0%}
    - Heavy attack ratio: {stats.heavy_attack_ratio:.0%}
    - Player retreated below {stats.retreat_hp_threshold:.0%} HP
    - Average distance from boss: {stats.avg_distance_from_boss:.1f} units
    - Death caused by: {death_cause}
    - Throwables used: flask={stats.throwables_used.flask}, tablet={stats.throwables_used.tablet}, spear={stats.throwables_used.spear}
    - Total fight duration: {stats.total_fight_duration_seconds} seconds
    
    Identify the 1-2 most exploitable behavioral patterns. 
    IMPORTANT: For the 'name' field, you MUST use EXACTLY one of these valid keys, based on what fits best:
    - PUNISH_RIGHT_DODGE
    - PUNISH_LEFT_DODGE
    - AGGRESS_AT_35PCT_HP
    - CHAIN_PUNISH_LIGHT
    - RANGE_PUNISH
    - APPROACH_PUNISH
    - THROWABLE_BAIT
    
    Respond ONLY in this JSON format:
    {{
      "patterns": [
        {{
          "name": "PATTERN_NAME",
          "observation": "What you observed",
          "confidence": 0.0-1.0,
          "exploitable_by": "Which boss attack can punish this"
        }}
      ],
      "reasoning_trace": "Step by step reasoning about what you found"
    }}
    """
    
    response_text = await generate(prompt)
    
    # Robust JSON extraction for LLMs
    try:
        start_idx = response_text.find('{')
        end_idx = response_text.rfind('}')
        
        if start_idx != -1 and end_idx != -1:
            clean_text = response_text[start_idx:end_idx+1]
        else:
            clean_text = response_text
            
        data = json.loads(clean_text)
        patterns = data.get("patterns", [])
        reasoning_trace = data.get("reasoning_trace", "No reasoning trace provided by AI.")
        
        # Enrich with deterministic behavioral detail
        patterns = _enrich_patterns(patterns, combat_log, death_cause)
        
        print("\n=== DEATH ANALYST REASONING TRACE ===")
        print(reasoning_trace)
        print("======================================\n")
        
        return {
            "patterns": patterns,
            "reasoning_trace": reasoning_trace
        }
    except Exception as e:
        print(f"Error parsing Groq JSON: {e}")
        # Fallback based on raw stats
        fallback_pattern = []
        reasoning = "Groq JSON parsing failed. Falling back to rule-based analysis."
        
        if stats.dodge_right_count > stats.dodge_left_count:
            fallback_pattern.append({
                "name": "PUNISH_RIGHT_DODGE",
                "observation": f"Player dodged right {stats.dodge_right_count} times vs {stats.dodge_left_count} left.",
                "confidence": 0.8,
                "exploitable_by": "MIRROR_ATTACK tracking right"
            })
            reasoning += f" Detected right-dodge bias ({stats.dodge_right_count} vs {stats.dodge_left_count})."
        
        if stats.light_attack_ratio > 0.7:
            fallback_pattern.append({
                "name": "CHAIN_PUNISH_LIGHT",
                "observation": f"Player used light attacks {stats.light_attack_ratio:.0%} of the time.",
                "confidence": 0.7,
                "exploitable_by": "Post-combo counter"
            })
            reasoning += " Detected light attack spam bias."

        # Enrich fallback patterns too
        fallback_pattern = _enrich_patterns(fallback_pattern, combat_log, death_cause)

        print("\n=== DEATH ANALYST REASONING TRACE (FALLBACK) ===")
        print(reasoning)
        print("================================================\n")
            
        return {
            "patterns": fallback_pattern,
            "reasoning_trace": reasoning
        }
