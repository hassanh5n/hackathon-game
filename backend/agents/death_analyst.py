import json
from models.combat_log import CombatLog
from services.ai_service import generate

async def analyze_death(combat_log: CombatLog, death_cause: str) -> dict:
    """
    Analyzes combat log to identify exploitable player patterns.
    Calls Gemini with structured prompt.
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
    - Throwables used: {stats.throwables_used}
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
        
        print("\n=== DEATH ANALYST REASONING TRACE ===")
        print(reasoning_trace)
        print("======================================\n")
        
        return {
            "patterns": patterns,
            "reasoning_trace": reasoning_trace
        }
    except Exception as e:
        print(f"Error parsing Gemini JSON: {e}")
        # Fallback based on raw stats
        fallback_pattern = []
        reasoning = "Gemini JSON parsing failed. Falling back to rule-based analysis."
        
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

        print("\n=== DEATH ANALYST REASONING TRACE (FALLBACK) ===")
        print(reasoning)
        print("================================================\n")
            
        return {
            "patterns": fallback_pattern,
            "reasoning_trace": reasoning
        }
