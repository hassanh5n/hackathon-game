"""
Head-to-head model comparison for OpenRouter free models.
"""
import httpx, os, sys
from dotenv import load_dotenv
load_dotenv()

KEY = os.getenv("OPENROUTER_API_KEY")
URL = "https://openrouter.ai/api/v1/chat/completions"
HEADERS = {
    "Authorization": f"Bearer {KEY}", 
    "Content-Type": "application/json",
    "HTTP-Referer": "http://localhost:8000",
    "X-Title": "Nemesis Boss Fight"
}

SYSTEM = """You are Humbaba.

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

SCENARIOS = [
    {
        "label": "Dodge-left addict, short fight",
        "prompt": """EXAMPLES (behavior -> taunt):

Behavior: The warrior always retreated after missing heavy attacks.
Taunt: "You flee from your own failures."

Behavior: The warrior panic-healed at low health every time.
Taunt: "You begged medicine to forgive incompetence."

Behavior: The warrior repeatedly dodged left into attacks.
Taunt: "Your left side belongs to me now."

Behavior: The warrior relied almost entirely on light attacks.
Taunt: "Small strikes for a small hope."

---

Now generate ONE taunt for this warrior:

Behavior:
- The warrior rolled left 14 times. They died boss_sweep_left.
- The warrior approached carelessly. They died boss_sweep_left.
- This was their first death.

Tone: Clinical observation. You are not angry. You are noting a weakness the way a predator notes a limp.

Rules:
- Maximum 10 words.
- Output ONLY the taunt.
- No quotes. No ellipsis. No exclamation marks.
- Do NOT narrate. Do NOT explain.
- Be specific to what the warrior actually did."""
    },
    {
        "label": "Panic healer, light spam",
        "prompt": """EXAMPLES (behavior -> taunt):

Behavior: The warrior always retreated after missing heavy attacks.
Taunt: "You flee from your own failures."

Behavior: The warrior panic-healed at low health every time.
Taunt: "You begged medicine to forgive incompetence."

Behavior: The warrior kept dying in the same phase.
Taunt: "Your panic arrives earlier every death."

---

Now generate ONE taunt for this warrior:

Behavior:
- The warrior used light attacks 90% of the time. They died mid-combo.
- The warrior panic-healed below 30% HP.
- The warrior made the exact same mistake (CHAIN_PUNISH_LIGHT) in a previous life.
- This is death number 3.

Tone: Sharp and contemptuous. Their stupidity has grown personal. Speak with the impatience of something ancient being wasted.

Rules:
- Maximum 10 words.
- Output ONLY the taunt.
- No quotes. No ellipsis. No exclamation marks.
- Do NOT narrate. Do NOT explain.
- Be specific to what the warrior actually did."""
    },
]

BANNED = {"mistake", "error", "again", "pathetic", "mortal", "foolish", "cannot", "defeat", "left", "right", "times", "numbers"}

MODELS = [
    "openai/gpt-oss-120b:free",
    "google/gemma-4-31b-it:free",
    "deepseek/deepseek-v4-flash:free",
    "qwen/qwen3-next-80b-a3b-instruct:free",
    "minimax/minimax-m2.5:free"
]

def check_violations(text):
    words = text.lower().replace(",", "").replace(".", "").replace('"', '').split()
    return [w for w in words if w in BANNED]

def call_model(model, scenario):
    try:
        r = httpx.post(URL, headers=HEADERS, json={
            "model": model,
            "messages": [
                {"role": "system", "content": SYSTEM},
                {"role": "user", "content": scenario["prompt"]},
            ],
            "temperature": 0.75,
        }, timeout=20.0)
        if r.status_code != 200:
            return f"[HTTP {r.status_code}]"
        data = r.json()
        text = data["choices"][0]["message"]["content"]
        if "</think>" in text:
            text = text.split("</think>")[-1].strip()
        return text.strip().strip('"').strip("'")
    except Exception as e:
        return f"[ERROR: {e}]"

if __name__ == "__main__":
    sys.stdout = open(sys.stdout.fileno(), mode='w', encoding='utf-8', buffering=1)
    print("=" * 80)
    print("OPENROUTER FREE MODEL COMPARISON")
    print("=" * 80)
    
    for scenario in SCENARIOS:
        print(f"\n{'='*60}")
        print(f"SCENARIO: {scenario['label']}")
        print(f"{'='*60}")
        
        for model in MODELS:
            taunt = call_model(model, scenario)
            violations = check_violations(taunt)
            word_count = len(taunt.split())
            
            status = "OK" if not violations and word_count <= 12 else "FAIL"
            v_str = f" [BANNED: {', '.join(violations)}]" if violations else ""
            w_str = f" [TOO LONG: {word_count}w]" if word_count > 12 else ""
            
            model_short = model.split("/")[-1].replace(":free", "")
            print(f"  {model_short:30s} | {status:4s} | \"{taunt}\"{v_str}{w_str}")
