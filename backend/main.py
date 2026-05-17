from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from typing import Dict
from models.combat_log import DeathAnalysisRequest
from models.nemesis_state import NemesisState, AnalysisResult
from agents.death_analyst import analyze_death
from agents.adaptation_selector import select_adaptations
from agents.fairness_checker import check_fairness
from agents.taunt_generator import generate_taunt
from config import MAX_ACTIVE_ADAPTATIONS

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
        
        # 4. Pipeline Step 4: Taunt Generator Agent
        taunt = await generate_taunt(
            patterns, 
            state.total_deaths, 
            state.personality_shift
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
