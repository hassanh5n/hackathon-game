import json
from backend.models.combat_log import CombatLog
from backend.services.gemini_service import generate

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
    
    Identify the 2-3 most exploitable behavioral patterns. 
    For each pattern, name it and explain why it is exploitable.
    
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
    
    # Try to parse JSON from response
    try:
        # Simple cleanup in case Gemini wraps in ```json ... ```
        clean_text = response_text.strip()
        if clean_text.startswith("```json"):
            clean_text = clean_text[7:]
        if clean_text.endswith("```"):
            clean_text = clean_text[:-3]
        clean_text = clean_text.strip()
        
        data = json.loads(clean_text)
        patterns = data.get("patterns", [])
        reasoning_trace = data.get("reasoning_trace", "No reasoning trace provided by Gemini.")
        
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
