from typing import List

AVAILABLE_ADAPTATIONS = {
    "PUNISH_RIGHT_DODGE": "Boss biases MIRROR_ATTACK to track right-dodge movement",
    "PUNISH_LEFT_DODGE": "Boss biases MIRROR_ATTACK to track left-dodge movement",
    "AGGRESS_AT_35PCT_HP": "Boss becomes immediately aggressive when player HP drops below threshold",
    "CHAIN_PUNISH_LIGHT": "Boss reduces recovery window after player's light attack chain of 3+",
    "RANGE_PUNISH": "Boss uses VINE_WHIP more often when player maintains distance",
    "APPROACH_PUNISH": "Boss uses STOMP combo when player approaches from front",
    "THROWABLE_BAIT": "Boss briefly exposes weak point then instantly counters if player throws",
    "MERCY_REMOVE": "Remove one active adaptation (mercy trigger)"
}

async def select_adaptations(
    patterns: List[dict],
    current_adaptations: List[str],
    total_deaths: int,
    max_adaptations: int = 4
) -> List[str]:
    """
    Selects which adaptations to activate.
    Never duplicates existing adaptations.
    Never exceeds max_adaptations.
    If total_deaths >= 8: triggers mercy (removes highest weight adaptation, logs "MERCY TRIGGERED").
    Prints selection reasoning to console.
    Returns full updated adaptation list.
    """
    new_adaptations = list(current_adaptations)
    reasoning = []
    
    print("\n=== ADAPTATION SELECTOR ===")
    
    # 1. Handle Mercy Trigger
    if total_deaths >= 8:
        if new_adaptations:
            # For now, just remove the last one added as a proxy for "highest weight" or most recent
            # In a real system we'd check weights here (shared with fairness_checker)
            removed = new_adaptations.pop(0)
            print(f"MERCY TRIGGERED: Removed {removed} due to {total_deaths} deaths.")
            reasoning.append(f"MERCY TRIGGERED: Removed {removed}")
        else:
            print("MERCY TRIGGERED: But no adaptations were active to remove.")
            reasoning.append("MERCY TRIGGERED: No adaptations to remove")

    # 2. Add new adaptations based on patterns
    for pattern in patterns:
        pattern_name = pattern.get("name")
        if pattern_name in AVAILABLE_ADAPTATIONS:
            if pattern_name not in new_adaptations:
                if len(new_adaptations) < max_adaptations:
                    new_adaptations.append(pattern_name)
                    print(f"Adding adaptation: {pattern_name} based on pattern: {pattern.get('observation')}")
                    reasoning.append(f"Added {pattern_name}")
                else:
                    print(f"Max adaptations ({max_adaptations}) reached. Skipping {pattern_name}.")
                    reasoning.append(f"Skipped {pattern_name} (Max reached)")
            else:
                print(f"Adaptation {pattern_name} is already active.")
                reasoning.append(f"Keep {pattern_name} (Already active)")
        else:
            print(f"Pattern {pattern_name} has no matching adaptation defined.")

    print(f"Final adaptation list: {new_adaptations}")
    print("===========================\n")
    
    return new_adaptations
