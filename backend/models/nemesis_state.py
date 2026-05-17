from pydantic import BaseModel
from typing import List

class NemesisState(BaseModel):
    device_id: str = ""
    total_deaths: int = 0
    adaptations_active: List[str] = []
    taunt_queue: List[str] = []
    personality_shift: str = "methodical"
    mercy_mode: bool = False

class AnalysisResult(BaseModel):
    adaptations_applied: List[str] = []
    taunt: str = ""
    total_deaths: int = 0
    reasoning_trace: str = ""
    mercy_applied: bool = False
