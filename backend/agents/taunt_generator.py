from typing import List
from services.ai_service import generate

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
    
    HUMBABA_SYSTEM = """You are Humbaba the Eternal, an ancient god-beast who has guarded this forest since before memory. 
    You have killed ten thousand warriors. You are not angry. You are not dramatic. You are quietly, devastatingly contemptuous.
    You speak in short, surgical sentences. You mock with insults, not facts. You make the player feel small by stating the obliterting their confidence and insult them about their failure.
    You never explain, never advise, never lecture, never state what attacks they used, just insult them on their performance. You observe and you sneer and taunt them to death."""

    PERSONALITY_CONTEXT = {
        "methodical": "You are cold and clinical. Like a predator noting the weakness of prey.",
        "aggressive": "You are sharp and impatient. Their stupidity has grown tiresome.",
        "wrathful": "You are savage and contemptuous. Their continued existence offends you."
    }

    tone = PERSONALITY_CONTEXT.get(personality_shift, PERSONALITY_CONTEXT["methodical"])

    user_prompt = f"""The warrior died again (death #{total_deaths}). Here is what they did:
{patterns_str}

Tone: {tone}

Write ONE taunt. Maximum 12 words. Mock what they actually did. No ellipsis. No exclamation marks. No quotes. Output ONLY the taunt."""

    print("\n=== TAUNT GENERATOR ===")
    print(f"Generating taunt for personality: {personality_shift}, deaths: {total_deaths}")

    taunt = await generate(user_prompt, system=HUMBABA_SYSTEM)
    # Strip any <think>...</think> tags that deepseek sometimes outputs
    if "</think>" in taunt:
        taunt = taunt.split("</think>")[-1].strip()
    taunt = taunt.strip().strip('"').strip("'")
    
    if not taunt or len(taunt.split()) > 20: # Safety check on length
        taunt = "The forest remembers every step you have taken."
        print("Falling back to default taunt.")
    
    print(f"Generated Taunt: {taunt}")
    print("========================\n")
    
    return taunt
