from typing import List
from backend.services.gemini_service import generate

async def generate_taunt(
    patterns: List[dict],
    total_deaths: int,
    personality_shift: str
) -> str:
    """
    Calls Gemini to generate a single boss taunt line.
    Uses the voice guidelines from AGENT_WORKFLOWS.md Pipeline Step 4 exactly.
    Maximum 15 words.
    Must reference specific player behavior from patterns.
    Falls back to a default taunt if Gemini fails:
      "The forest remembers every step you have taken."
    """
    
    # Format patterns for the prompt
    pattern_descriptions = [f"{p.get('name')}: {p.get('observation')}" for p in patterns]
    patterns_str = "\n".join(pattern_descriptions) if pattern_descriptions else "No clear patterns identified yet."
    
    prompt = f"""
    You are Humbaba the Eternal, an ancient guardian boss.
    Generate a single taunt line to say to the player after they just died.
    
    Context:
    - Player's Total Deaths: {total_deaths}
    - Observed Patterns: 
    {patterns_str}
    - Your Current Personality Shift: {personality_shift}
    
    Voice Guidelines:
    - Ancient, measured, not overly dramatic
    - Reference the player's SPECIFIC behavior from the patterns provided
    - Cold observation, not mocking — you have seen this ten thousand times
    - Maximum 15 words
    - No exclamation marks
    - Should feel like a divine judge, not a villain
    
    Personality Tone:
    - methodical: clinical observation
    - aggressive: slight impatience
    - wrathful: contempt mixed with recognition
    
    Respond ONLY with the taunt text.
    """
    
    print("\n=== TAUNT GENERATOR ===")
    print(f"Generating taunt for personality: {personality_shift}, deaths: {total_deaths}")
    
    taunt = await generate(prompt)
    taunt = taunt.strip().replace('"', '')
    
    if not taunt or len(taunt.split()) > 20: # Safety check on length
        taunt = "The forest remembers every step you have taken."
        print("Falling back to default taunt.")
    
    print(f"Generated Taunt: {taunt}")
    print("========================\n")
    
    return taunt
