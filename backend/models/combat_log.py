from pydantic import BaseModel
from typing import List, Optional

class ThrowablesUsed(BaseModel):
    flask: int = 0
    tablet: int = 0
    spear: int = 0

class CombatEvent(BaseModel):
    event: str        # "player_dodge", "player_attack", "boss_attack"
    direction: str = ""   # "right", "left", "forward", "back"
    attack_type: str = "" # "light", "heavy"
    timestamp_ms: int = 0

class CombatStats(BaseModel):
    dodge_right_count: int = 0
    dodge_left_count: int = 0
    dodge_forward_count: int = 0
    dodge_back_count: int = 0
    light_attack_count: int = 0
    heavy_attack_count: int = 0
    light_attack_ratio: float = 0.0
    heavy_attack_ratio: float = 0.0
    throwables_used: ThrowablesUsed = ThrowablesUsed()
    retreat_hp_threshold: float = 0.0
    avg_distance_from_boss: float = 0.0
    total_fight_duration_seconds: int = 0

class CombatLog(BaseModel):
    events: List[CombatEvent] = []
    stats: CombatStats = CombatStats()

class DeathAnalysisRequest(BaseModel):
    device_id: str
    attempt_id: str
    death_cause: str
    combat_log: CombatLog
