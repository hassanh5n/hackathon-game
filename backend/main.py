from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from typing import Dict
from models.combat_log import DeathAnalysisRequest, CombatStats
from models.nemesis_state import NemesisState, AnalysisResult
from agents.death_analyst import analyze_death
from agents.adaptation_selector import select_adaptations
from agents.fairness_checker import check_fairness
from agents.taunt_generator import generate_taunt
from config import MAX_ACTIVE_ADAPTATIONS

def update_boss_memory(state: NemesisState, patterns: list, combat_stats: CombatStats) -> None:
    """
    Deterministic update of boss emotional memory after each death.
    No LLM involved — pure game logic.
    - confidence rises as Humbaba keeps winning
    - annoyance rises when the player repeats the same mistake
    - respect rises slightly if the player fought long before dying
    - dominant_weakness is set to whichever pattern has been seen most
    """
    mem = state.boss_memory

    # Track pattern repetition
    for p in patterns:
        key = p.get("name", "UNKNOWN")
        mem.pattern_counts[key] = mem.pattern_counts.get(key, 0) + 1

    # Dominant weakness = most repeated pattern key
    if mem.pattern_counts:
        mem.dominant_weakness = max(mem.pattern_counts, key=mem.pattern_counts.get)

    # Confidence: climbs with deaths, caps at 0.97
    mem.confidence = min(0.97, 0.5 + state.total_deaths * 0.04)

    # Annoyance: climbs when the same mistake is repeated (dominant count > 2)
    dominant_count = mem.pattern_counts.get(mem.dominant_weakness, 0)
    if dominant_count >= 3:
        mem.annoyance = min(1.0, mem.annoyance + 0.15)
    elif dominant_count >= 2:
        mem.annoyance = min(1.0, mem.annoyance + 0.07)

    # Respect: player gets credit for fighting long before dying
    if combat_stats.total_fight_duration_seconds > 90:
        mem.respect = min(1.0, mem.respect + 0.10)
    elif combat_stats.total_fight_duration_seconds > 45:
        mem.respect = min(1.0, mem.respect + 0.05)

app = FastAPI(title="Nemesis Brain API")

# Add CORS middleware for Unity WebRequest
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"], # In production, restrict this
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# In-memory storage for demo purposes
nemesis_states: Dict[str, NemesisState] = {}

@app.get("/health")
async def health():
    return {"status": "ok", "model": "gemini-2.0-flash"}

@app.get("/nemesis-state/{device_id}")
async def get_state(device_id: str):
    if device_id not in nemesis_states:
        nemesis_states[device_id] = NemesisState(device_id=device_id)
    return nemesis_states[device_id]

@app.delete("/nemesis-state/{device_id}")
async def reset_state(device_id: str):
    if device_id in nemesis_states:
        nemesis_states[device_id] = NemesisState(device_id=device_id)
        return {"reset": True}
    return {"reset": False, "reason": "Not found"}

@app.post("/analyze-death", response_model=AnalysisResult)
async def process_death(request: DeathAnalysisRequest):
    device_id = request.device_id
    
    # 0. Get or initialize state
    if device_id not in nemesis_states:
        nemesis_states[device_id] = NemesisState(device_id=device_id)
    
    state = nemesis_states[device_id]
    state.total_deaths += 1
    
    # Personality shift logic based on death count
    if state.total_deaths > 12:
        state.personality_shift = "wrathful"
    elif state.total_deaths > 5:
        state.personality_shift = "aggressive"
    else:
        state.personality_shift = "methodical"
        
    print(f"\n>>> PROCESSING DEATH FOR {device_id} (Attempt: {request.attempt_id}, Total Deaths: {state.total_deaths}) <<<")

    try:
        # 1. Pipeline Step 1: Death Analyst Agent
        analysis = await analyze_death(request.combat_log, request.death_cause)
        patterns = analysis["patterns"]
        reasoning_trace = analysis["reasoning_trace"]
        
        # 2. Pipeline Step 2: Adaptation Selector Agent
        new_adaptations = await select_adaptations(
            patterns, 
            state.adaptations_active, 
            state.total_deaths,
            MAX_ACTIVE_ADAPTATIONS
        )
        
        # 3. Pipeline Step 3: Fairness Checker Agent
        fairness_result = await check_fairness(new_adaptations)
        state.adaptations_active = fairness_result["adaptations"]
        
        # 4. Update boss emotional memory (deterministic, before taunt)
        update_boss_memory(state, patterns, request.combat_log.stats)

        # 5. Pipeline Step 4: Taunt Generator Agent
        taunt = await generate_taunt(
            patterns,
            state.total_deaths,
            state.personality_shift,
            state.boss_memory
        )
    except Exception as e:
        print(f"Pipeline error: {e}")
        # Fallback response so Unity doesn't hang
        return AnalysisResult(
            adaptations_applied=state.adaptations_active,
            taunt="The forest claims another...",
            total_deaths=state.total_deaths,
            reasoning_trace=f"Pipeline failed: {str(e)}",
            mercy_applied=state.mercy_mode
        )
        
    # 5. Update state
    state.taunt_queue.append(taunt)
    # Check for mercy mode flag (mirrors selector logic)
    state.mercy_mode = (state.total_deaths >= 8)

    print(f">>> PIPELINE COMPLETE: {len(state.adaptations_active)} adaptations active, Difficulty Score: {fairness_result['score']:.2f} <<<")

    return AnalysisResult(
        adaptations_applied=state.adaptations_active,
        taunt=taunt,
        total_deaths=state.total_deaths,
        reasoning_trace=reasoning_trace,
        mercy_applied=state.mercy_mode
    )

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
