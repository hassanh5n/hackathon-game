from typing import List
from services.ai_service import generate
from models.nemesis_state import BossMemory

# ─────────────────────────────────────────────
# VOICE DEFINITION
# Describes cadence, sentence structure, forbidden patterns, and examples.
# Vibe alone is not enough — this defines HOW Humbaba speaks.
# ─────────────────────────────────────────────
HUMBABA_SYSTEM = """You are Humbaba.

You do not rant.
You do not explain mechanics.
You do not narrate what happened.
You speak like execution is inevitable.

Your insults are:
- short
- personal
- predatory
- metaphorical

You speak as though the warrior disappointed you personally.
You speak as though you have been watching them for a very long time.

Banned words: mistake, error, again, pathetic, mortal, foolish, cannot, defeat, left, right, times, numbers

Bad:
"Your swordplay is weak."
"Pathetic mortal."
"You rolled left 14 times."
"Your left roll is a repeating mistake."
"Light attacks are your only choice."

Good:
"You died reaching for habits."
"Even your panic became predictable."
"You hid inside the same rhythm."
"Your fear always turned left."
"You rehearse failure beautifully."
"Every dodge became a confession."

Maximum 10 words.
Output only the taunt. Nothing else."""

# ─────────────────────────────────────────────
# FEW-SHOT EXAMPLES
# Models imitate examples far more reliably than instructions.
# These anchor tone, length, and specificity.
# ─────────────────────────────────────────────
FEW_SHOT_EXAMPLES = """EXAMPLES (behavior → taunt):

Behavior: The warrior always retreated after missing heavy attacks.
Taunt: "You flee from your own failures."

Behavior: The warrior panic-healed at low health every time.
Taunt: "You begged medicine to forgive incompetence."

Behavior: The warrior repeatedly dodged left into attacks.
Taunt: "Your left side belongs to me now."

Behavior: The warrior hesitated before every attack.
Taunt: "Even your courage needed permission."

Behavior: The warrior died six times attempting the same approach.
Taunt: "I have memorized your mistakes. You have not."

Behavior: The warrior fought well for ninety seconds then panicked.
Taunt: "You almost convinced me."

Behavior: The warrior relied almost entirely on light attacks.
Taunt: "Small strikes for a small hope."

Behavior: The warrior spent the entire fight maintaining distance.
Taunt: "Safety was a lie you believed."

Behavior: The warrior kept dying in the same phase.
Taunt: "Your panic arrives earlier every death."

Behavior: The warrior died while retreating.
Taunt: "You fled into the end."""


# ─────────────────────────────────────────────
# PATTERN → NARRATIVE COMPRESSION
# Converts structured analytics into prose for the LLM.
# "Concrete behavior → concrete taunt" as the advice specified.
# ─────────────────────────────────────────────
def _compress_patterns_to_narrative(
    patterns: List[dict],
    pattern_counts: dict,
    dominant_weakness: str,
    total_deaths: int,
    fight_duration_s: int,
) -> str:
    """
    Converts structured pattern data into vivid prose for the LLM.
    We use the rich 'death_context' provided by the death analyst.
    """
    lines = []

    for p in patterns:
        death_context = p.get("death_context", "")
        if death_context:
            lines.append(f"- {death_context}")
        else:
            obs = p.get("observation", "")
            if obs:
                lines.append(f"- {obs}")

    # Explicitly highlight if they keep repeating the same mistake across deaths
    if dominant_weakness:
        repeated_count = pattern_counts.get(dominant_weakness, 0)
        if repeated_count >= 3:
            lines.append(f"- The warrior has been punished for {dominant_weakness} in {repeated_count} separate lives.")
        elif repeated_count == 2:
            lines.append(f"- The warrior made the exact same mistake ({dominant_weakness}) in a previous life.")

    if not lines:
        lines.append("- The warrior died without revealing a clear pattern.")

    # Add death count context
    if total_deaths == 1:
        lines.append("- This was their first death.")
    elif total_deaths <= 4:
        lines.append(f"- This is death number {total_deaths}.")
    elif total_deaths <= 8:
        lines.append(f"- The warrior has died {total_deaths} times. The same errors repeat.")
    else:
        lines.append(f"- The warrior has died {total_deaths} times. They return as though hope is a strategy.")

    # Add duration context for respect taunts
    if fight_duration_s > 90:
        lines.append("- They fought longer than most before dying.")

    return "\n".join(lines)


def _build_tone_instruction(
    personality_shift: str,
    boss_memory: BossMemory,
    total_deaths: int,
) -> str:
    """
    Derives a tone directive from boss emotional state.
    This maps the deterministic boss_memory scalars into LLM tone guidance.
    """
    # High respect + decent skill → reluctant acknowledgment
    if boss_memory.respect >= 0.3 and boss_memory.confidence < 0.8:
        return (
            "Tone: Cold acknowledgment. The warrior earned brief notice before dying. "
            "One line, restrained — almost a compliment, but not quite."
        )

    # High annoyance (same mistake, many times) → exhausted contempt
    if boss_memory.annoyance >= 0.7:
        return (
            "Tone: Exhausted contempt. The warrior bores you. "
            "They keep returning to die the same way. "
            "Speak like someone who has grown tired of being right."
        )

    # High confidence, many deaths → absolute dominance
    if boss_memory.confidence >= 0.85 and total_deaths > 8:
        return (
            "Tone: Absolute. You do not mock — you observe. "
            "The outcome was never in doubt. State it plainly."
        )

    # Default personality mapping
    if personality_shift == "wrathful":
        return (
            "Tone: Sharp and contemptuous. Their stupidity has grown personal. "
            "Speak with the impatience of something ancient being wasted."
        )
    if personality_shift == "aggressive":
        return (
            "Tone: Cold and impatient. The pattern has been identified. "
            "Address it directly."
        )
    # methodical (default)
    return (
        "Tone: Clinical observation. You are not angry. "
        "You are noting a weakness the way a predator notes a limp."
    )


async def generate_taunt(
    patterns: List[dict],
    total_deaths: int,
    personality_shift: str,
    boss_memory: "BossMemory | None" = None,
    fight_duration_s: int = 0,
) -> str:
    """
    Generates a single Humbaba taunt line via the LLM.

    Architecture (per ChatGPT recommendation):
    - Combat adaptation is handled deterministically (NOT here).
    - This function is ONLY for psychological flavor.
    - Input is compressed into vivid narrative prose before sending to LLM.
    - Few-shot examples anchor style far more reliably than instructions alone.
    - Boss emotional memory drives tone escalation across deaths.

    Max 10 words. Falls back to a curated default on failure.
    """
    from models.nemesis_state import BossMemory as _BM
    if boss_memory is None:
        boss_memory = _BM()

    # Build behavior narrative
    pattern_counts = boss_memory.pattern_counts if boss_memory else {}
    dominant_weakness = boss_memory.dominant_weakness if boss_memory else ""
    behavior_narrative = _compress_patterns_to_narrative(
        patterns, pattern_counts, dominant_weakness, total_deaths, fight_duration_s
    )

    # Build tone instruction from emotional state
    tone_instruction = _build_tone_instruction(
        personality_shift, boss_memory, total_deaths
    )

    user_prompt = f"""{FEW_SHOT_EXAMPLES}

---

Now generate ONE taunt for this warrior:

Behavior:
{behavior_narrative}

{tone_instruction}

Rules:
- Maximum 10 words.
- Output ONLY the taunt.
- No quotes. No ellipsis. No exclamation marks.
- Do NOT narrate. Do NOT explain.
- Be specific to what the warrior actually did."""

    print("\n=== TAUNT GENERATOR ===")
    print(f"Deaths: {total_deaths} | Personality: {personality_shift}")
    print(f"Boss Memory -> Confidence: {boss_memory.confidence:.2f} | "
          f"Annoyance: {boss_memory.annoyance:.2f} | Respect: {boss_memory.respect:.2f}")
    print(f"Dominant Weakness: {dominant_weakness or 'none yet'}")
    print(f"Behavior Narrative:\n{behavior_narrative}")

    taunt = await generate(user_prompt, system=HUMBABA_SYSTEM)

    # Strip any <think>...</think> tags (DeepSeek / some models emit these)
    if "</think>" in taunt:
        taunt = taunt.split("</think>")[-1].strip()

    taunt = taunt.strip().strip('"').strip("'")

    # Safety: if too long or empty, fall back to a curated contextual default
    if not taunt or len(taunt.split()) > 15:
        taunt = _contextual_fallback(boss_memory, total_deaths)
        print("Falling back to contextual default taunt.")

    print(f"Generated Taunt: {taunt}")
    print("========================\n")

    return taunt


# ─────────────────────────────────────────────
# FALLBACK LIBRARY
# Better than a single hardcoded line.
# Chosen deterministically from boss emotional state.
# ─────────────────────────────────────────────
def _contextual_fallback(boss_memory: "BossMemory", total_deaths: int) -> str:
    if boss_memory.respect >= 0.3:
        return "You almost made it matter."
    if boss_memory.annoyance >= 0.7:
        return "You return as though hope is a strategy."
    if total_deaths > 10:
        return "I have memorized your mistakes. You have not."
    if total_deaths > 5:
        return "Even your panic became predictable."
    return "The forest remembers every step you have taken."
