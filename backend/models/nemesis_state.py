from pydantic import BaseModel, Field
from typing import List, Dict

class BossMemory(BaseModel):
    """Stateful emotional profile of Humbaba. Drives taunt tone escalation."""
    confidence: float = 0.5    # 0.0 = unimpressed, 1.0 = dominant
    annoyance: float = 0.2     # rises when player keeps making same mistake
    respect: float = 0.0       # rises when player performs well before dying
    # Tracks how many times each pattern key was observed (for repetition mockery)
    pattern_counts: Dict[str, int] = Field(default_factory=dict)
    # Most punished behavior — used to anchor the most personal taunts
    dominant_weakness: str = ""

class NemesisState(BaseModel):
    device_id: str = ""
    total_deaths: int = 0
    adaptations_active: List[str] = []
    taunt_queue: List[str] = []
    personality_shift: str = "methodical"
    mercy_mode: bool = False
    boss_memory: BossMemory = Field(default_factory=BossMemory)

class AnalysisResult(BaseModel):
    adaptations_applied: List[str] = []
    taunt: str = ""
    total_deaths: int = 0
    reasoning_trace: str = ""
    mercy_applied: bool = False
