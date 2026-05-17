from typing import List
from backend.config import MAX_DIFFICULTY_SCORE

DIFFICULTY_WEIGHTS = {
    "PUNISH_RIGHT_DODGE": 1.5,
    "PUNISH_LEFT_DODGE": 1.5,
    "AGGRESS_AT_35PCT_HP": 2.0,
    "CHAIN_PUNISH_LIGHT": 1.8,
    "RANGE_PUNISH": 1.2,
    "APPROACH_PUNISH": 1.3,
    "THROWABLE_BAIT": 2.5,  # Very punishing
}

async def check_fairness(adaptations: List[str]) -> dict:
    """
    Calculates total difficulty score from active adaptations.
    If score > MAX_DIFFICULTY_SCORE (7.5):
      Removes highest-weight adaptation
      Logs: "FAIRNESS OVERRIDE: removed X, score was Y"
    Returns: {
      "adaptations": final list,
      "score": float,
      "override_applied": bool,
      "reasoning": str
    }
    """
    current_adaptations = list(adaptations)
    total_score = sum(DIFFICULTY_WEIGHTS.get(a, 1.0) for a in current_adaptations)
    override_applied = False
    removed_name = ""
    original_score = total_score
    
    print("\n=== FAIRNESS CHECKER ===")
    print(f"Initial Difficulty Score: {total_score:.2f} / {MAX_DIFFICULTY_SCORE}")

    while total_score > MAX_DIFFICULTY_SCORE and current_adaptations:
        # Find adaptation with highest weight
        highest_weight = -1.0
        highest_adaptation = ""
        for a in current_adaptations:
            weight = DIFFICULTY_WEIGHTS.get(a, 1.0)
            if weight > highest_weight:
                highest_weight = weight
                highest_adaptation = a
        
        if highest_adaptation:
            current_adaptations.remove(highest_adaptation)
            print(f"FAIRNESS OVERRIDE: removed {highest_adaptation}, original score was {total_score:.2f}")
            total_score = sum(DIFFICULTY_WEIGHTS.get(a, 1.0) for a in current_adaptations)
            override_applied = True
            removed_name = highest_adaptation
        else:
            break

    reasoning = f"Total score {original_score:.2f} was " + ("above" if override_applied else "within") + f" limit of {MAX_DIFFICULTY_SCORE}."
    if override_applied:
        reasoning += f" Removed {removed_name} to maintain fairness. Final score: {total_score:.2f}"
    
    print(f"Final Difficulty Score: {total_score:.2f}")
    print(f"Reasoning: {reasoning}")
    print("========================\n")

    return {
        "adaptations": current_adaptations,
        "score": total_score,
        "override_applied": override_applied,
        "reasoning": reasoning
    }
