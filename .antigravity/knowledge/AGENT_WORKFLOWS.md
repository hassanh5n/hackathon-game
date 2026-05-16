# ANTIGRAVITY AGENT WORKFLOWS & RULES
## NEMESIS: The Eternal Guardian
### Drop this file into .antigravity/knowledge/ — agents read it automatically

---

## CORE RULE: HOW TO WORK ON THIS PROJECT

You are building a 3D mobile action RPG called NEMESIS: The Eternal Guardian.
The project is split into two parts:
1. A Unity mobile game (C# scripts, scenes, prefabs, animations)
2. A Python FastAPI backend (Nemesis AI agent system)

Always read GAME_DESIGN_DOCUMENT.md first before starting any task.
Always read TECHNICAL_ARCHITECTURE.md for folder structure and script specs.
Never invent game mechanics not described in the GDD. Ask before adding features.
Do not use deprecated Unity APIs. Target Unity 2022.3 LTS.
Do not use legacy Unity UI (Canvas/UGUI) — use UI Toolkit (UXML/USS).
All Unity scripts must be null-safe. Check for null before accessing components.
All API calls from Unity must handle: timeout (5s), network failure, and empty response gracefully.
Python backend code must include docstrings, type hints, and error handling on every function.

---

## AGENT ROLE DEFINITIONS

### AGENT 1: Unity Frontend Agent
**Handles:** All Unity C# scripts, scenes, prefabs, animations, VFX, UI
**Model to use:** Gemini 3 Pro (complex, multi-file tasks) or Claude Sonnet 4.6 (architecture reasoning)
**Workflow:**
1. Read GDD section relevant to the task
2. Check TECHNICAL_ARCHITECTURE.md for the correct script location
3. Write the script with full implementation (not stubs)
4. After writing, verify: Does it handle null? Does it follow naming conventions? Does it match the spec?
5. Generate a task artifact explaining what was built and how to test it in Unity

**Naming Conventions:**
- Classes: PascalCase (PlayerController, BossAdaptation)
- Methods: PascalCase (LogDodge, ApplyAdaptations)
- Private fields: camelCase with underscore prefix (_currentHp, _attackCooldown)
- Public fields/properties: PascalCase
- Constants: UPPER_SNAKE_CASE (MAX_ADAPTATIONS, BASE_URL)

### AGENT 2: Nemesis Backend Agent
**Handles:** Python FastAPI backend, agent logic, Gemini API calls, Firebase integration
**Model to use:** Gemini 3 Pro with Deep Think for agent reasoning design
**Workflow:**
1. Read AGENT_WORKFLOWS.md section on Nemesis Agent Pipeline
2. Build each agent function as a separate, testable function
3. Each agent function must log its reasoning as a string (for hackathon trace demo)
4. After writing, run a dry-test with mock combat log data
5. Generate an artifact showing example input → reasoning trace → output

### AGENT 3: Cinematic & Timeline Agent
**Handles:** Unity Timeline sequences, Cinemachine cameras, cutscene scripts
**Model to use:** Gemini 3 Flash (these tasks are well-defined, don't need heavy reasoning)
**Workflow:**
1. Use Unity's Timeline API for all cutscenes (not coroutines for sequencing)
2. Use Cinemachine Virtual Cameras for all cinematic camera moves
3. Cutscene sequences must have skip functionality (input listener during playback)

### AGENT 4: UI/UX Agent
**Handles:** All UXML/USS files, HUD, menus, death screen, settings
**Model to use:** Gemini 3 Flash
**Workflow:**
1. Design in UXML with USS styling — not inline styles
2. All colors must reference USS variables (--color-health-red, --color-boss-amber, etc.)
3. No hard-coded pixel sizes — use relative units (%, flex) for mobile scaling
4. Test layout at 375x812 (iPhone SE) and 412x915 (Android standard)

---

## NEMESIS AGENT PIPELINE (DETAILED)

This is the most critical system. Build this FIRST after basic player/boss movement works.

### Pipeline Step 1: Death Analyst Agent

```python
async def analyze_death(combat_log: CombatLog) -> AnalysisResult:
    """
    Analyzes a combat log and identifies dominant player behavioral patterns.
    
    Input: Full combat log from one fight attempt
    Output: List of identified patterns with confidence scores
    
    This function calls Gemini with a structured prompt and parses JSON response.
    The reasoning trace MUST be saved to AnalysisResult.reasoning_trace for demo purposes.
    """
    
    prompt = f"""
    You are analyzing a player's combat behavior in a boss fight game.
    
    Combat Statistics:
    - Dodge right: {combat_log.stats.dodge_right_count} times
    - Dodge left: {combat_log.stats.dodge_left_count} times  
    - Light attack ratio: {combat_log.stats.light_attack_ratio:.0%}
    - Heavy attack ratio: {combat_log.stats.heavy_attack_ratio:.0%}
    - Player retreated below {combat_log.stats.retreat_hp_threshold:.0%} HP
    - Average distance from boss: {combat_log.stats.avg_distance:.1f} units
    - Death caused by: {combat_log.death_cause}
    - Throwables used: {combat_log.stats.throwables_used}
    
    Identify the 2-3 most exploitable behavioral patterns. 
    For each pattern, name it and explain why it is exploitable.
    
    Respond ONLY in this JSON format:
    {{
      "patterns": [
        {{
          "name": "PATTERN_NAME",
          "observation": "What you observed",
          "confidence": 0.0-1.0,
          "exploitable_by": "Which boss attack can punish this"
        }}
      ],
      "reasoning_trace": "Step by step reasoning about what you found"
    }}
    """
    # ... call Gemini, parse response, return AnalysisResult
```

### Pipeline Step 2: Adaptation Selector Agent

```python
AVAILABLE_ADAPTATIONS = {
    "PUNISH_RIGHT_DODGE": "Boss biases MIRROR_ATTACK to track right-dodge movement",
    "PUNISH_LEFT_DODGE": "Boss biases MIRROR_ATTACK to track left-dodge movement",
    "AGGRESS_AT_35PCT_HP": "Boss becomes immediately aggressive when player HP drops below threshold",
    "CHAIN_PUNISH_LIGHT": "Boss reduces recovery window after player's light attack chain of 3+",
    "RANGE_PUNISH": "Boss uses VINE_WHIP more often when player maintains distance",
    "APPROACH_PUNISH": "Boss uses STOMP combo when player approaches from front",
    "THROWABLE_BAIT": "Boss briefly exposes weak point then instantly counters if player throws",
    "MERCY_REMOVE": "Remove one active adaptation (mercy trigger)"
}

async def select_adaptations(
    patterns: List[Pattern], 
    current_adaptations: List[str],
    total_deaths: int
) -> List[str]:
    """
    Selects which adaptations to activate based on identified patterns.
    
    Rules:
    - Maximum 4 total active adaptations
    - Do not duplicate existing adaptations
    - If total_deaths >= 8: trigger MERCY_REMOVE on one adaptation
    - Return the full updated list
    """
```

### Pipeline Step 3: Fairness Checker Agent

```python
DIFFICULTY_WEIGHTS = {
    "PUNISH_RIGHT_DODGE": 1.5,
    "PUNISH_LEFT_DODGE": 1.5,
    "AGGRESS_AT_35PCT_HP": 2.0,
    "CHAIN_PUNISH_LIGHT": 1.8,
    "RANGE_PUNISH": 1.2,
    "APPROACH_PUNISH": 1.3,
    "THROWABLE_BAIT": 2.5,  # Very punishing
}
MAX_DIFFICULTY_SCORE = 7.5  # If exceeded, remove weakest adaptation

async def check_fairness(adaptations: List[str]) -> FairnessResult:
    """
    Ensures the combination of active adaptations does not make the fight unwinnable.
    
    Calculates a difficulty score. If > MAX_DIFFICULTY_SCORE:
    - Remove the highest-weight adaptation
    - Log: "Fairness override applied: removed X because total score was Y"
    
    Always include reasoning_trace in output for hackathon demo.
    """
```

### Pipeline Step 4: Taunt Generator Agent

```python
async def generate_taunt(
    patterns: List[Pattern],
    total_deaths: int,
    personality_shift: str
) -> str:
    """
    Generates a single line of boss dialogue in the voice of Humbaba the Eternal.
    
    Voice guidelines:
    - Ancient, measured, not overly dramatic
    - References the player's SPECIFIC behavior (not generic)
    - Cold observation, not mocking — the boss has seen this ten thousand times
    - Maximum 15 words
    - No exclamation marks
    - Should feel like a divine judge, not a villain
    
    Examples of GOOD taunts:
    - "You always break right. The forest has memorized your shadow."
    - "Four times. Each death faster than the last."
    - "You throw your spear when afraid. I have learned to expect it."
    
    Examples of BAD taunts (too generic, avoid):
    - "You cannot defeat me." 
    - "Your efforts are futile."
    - "Try again, little warrior."
    
    Personality shift affects tone:
    - methodical: clinical observation
    - aggressive: slight impatience
    - wrathful: contempt mixed with recognition
    """
```

---

## BUILD ORDER (Follow This Exactly)

### Phase 1: Foundation (Day 1-2)
```
Task 1: Set up Unity project with URP, install all packages
Task 2: Create BossFight.unity scene with basic arena plane, lighting
Task 3: Import Mixamo hero character, set up Animator with: idle, walk, dodge, light_attack, heavy_attack, death
Task 4: Implement PlayerController.cs — movement with left joystick + lock-on target
Task 5: Implement PlayerStats.cs — HP bar, stamina bar, death event
Task 6: Implement CombatLogger.cs — basic logging skeleton
```

### Phase 2: Boss (Day 2-3)
```
Task 7: Import boss character mesh, set up Animator
Task 8: Implement BossController.cs state machine (3 stages)
Task 9: Implement BossAttackSystem.cs for Stage 1 attacks (GROUND_SLAM, VINE_WHIP, FOREST_ROAR, STOMP)
Task 10: Add Stage 1 → Stage 2 transition (HP threshold, visual material swap)
Task 11: Implement Stage 2 attacks (ROOT_BURST, SAPLING_SUMMON, TREE_SLAM)
Task 12: Add Stage 2 → Stage 3 transition (material swap to translucent)
Task 13: Implement Stage 3 attacks (DIVINE_FIRE, PHASE_STEP, TABLET_STORM)
```

### Phase 3: Nemesis Backend (Day 3-4)
```
Task 14: Set up Python FastAPI project skeleton
Task 15: Implement Gemini API service wrapper
Task 16: Implement Firebase service (read/write nemesis_state)
Task 17: Implement death_analyst.py agent with Gemini call
Task 18: Implement adaptation_selector.py agent
Task 19: Implement fairness_checker.py agent
Task 20: Implement taunt_generator.py agent
Task 21: Wire all agents into POST /analyze-death endpoint
Task 22: Implement GET /nemesis-state/{device_id} endpoint
Task 23: Deploy backend to Railway or Cloud Run
Task 24: Implement NemesisAPIManager.cs in Unity, test calls
Task 25: Implement BossAdaptation.cs — apply nemesis_state adaptations to BossAttackSystem
```

### Phase 4: Cinematics & Polish (Day 4-5)
```
Task 26: Implement DeathScreenUI — YOU FELL screen, taunt display
Task 27: Implement Hero Intro cinematic (Unity Timeline)
Task 28: Implement Boss Intro cinematic (Timeline)
Task 29: Implement Victory cinematic (Timeline)
Task 30: Implement MainMenuUI with animated background
Task 31: Implement SettingsUI
Task 32: Add all VFX prefabs (fire flask, root burst, divine glow)
Task 33: Add audio — background music per stage, SFX
Task 34: Post-processing profile — bloom, vignette, chromatic aberration
Task 35: Mobile input polish — joystick feel, haptic feedback
```

### Phase 5: Demo Prep (Day 5-6)
```
Task 36: Build APK and test on physical Android device
Task 37: Fix any crashes or performance issues
Task 38: Verify nemesis system works end-to-end (die → backend call → adaptation → retry)
Task 39: Record demo video showing: first fight → death → agent trace visible → retry with adapted boss → taunt
Task 40: Polish: remove debug logs, clean up UI, ensure 60fps
```

---

## VERIFICATION CHECKLIST (Run Before Submitting)

- [ ] Game launches without errors on Android
- [ ] Player can move, attack, dodge in all directions
- [ ] All 3 boss stages trigger at correct HP thresholds
- [ ] Stage transition cutscenes play correctly
- [ ] On player death: combat log is sent to backend
- [ ] Backend returns adaptation decisions with reasoning trace
- [ ] On retry: boss has new behavior matching adaptation
- [ ] Taunt displays on death screen referencing player behavior
- [ ] After 8 deaths: mercy mechanic triggers (adaptation removed)
- [ ] Victory cinematic plays after boss death
- [ ] Main menu, settings, pause menu all function
- [ ] No memory leaks (use Unity Profiler to verify)
- [ ] Runs at 30+ FPS on mid-range Android (Snapdragon 700 series)

---

## WHAT TO SHOW JUDGES (AGENTIC TRANSPARENCY)

The judges will ask: "Show us the agent thinking."

In the demo:
1. Open a debug panel showing the reasoning_trace from the last /analyze-death call
2. Show it as formatted text: OBSERVATION → PATTERN → DECISION → FAIRNESS CHECK → TAUNT
3. This panel can be toggled with a debug button hidden in settings

In the backend logs, every agent call should print:
```
[DEATH_ANALYST] Input: 17 dodge events, 82% right bias
[DEATH_ANALYST] Reasoning: Player shows strong right-dodge preference...
[DEATH_ANALYST] Output: PUNISH_RIGHT_DODGE (confidence: 0.91)
[ADAPTATION_SELECTOR] Current: [] → Adding: PUNISH_RIGHT_DODGE
[FAIRNESS_CHECK] Score: 1.5/7.5 — OK
[TAUNT_GEN] Generated: "You always break right. The forest has memorized your shadow."
```

This log is your agent trace artifact for the submission.

---

## END OF AGENT WORKFLOWS
