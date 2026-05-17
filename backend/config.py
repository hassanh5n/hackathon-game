import os
from dotenv import load_dotenv

load_dotenv()

GEMINI_API_KEY = os.getenv("GEMINI_API_KEY", "")
GROQ_API_KEY = os.getenv("GROQ_API_KEY", "")
FIREBASE_PROJECT_ID = os.getenv("FIREBASE_PROJECT_ID", "")

MAX_ACTIVE_ADAPTATIONS = 4
MERCY_TRIGGER_DEATH_COUNT = 8
MAX_DIFFICULTY_SCORE = 7.5
