# TECHNICAL ARCHITECTURE
## NEMESIS: The Eternal Guardian
### For Antigravity Agent Reference

---

## 1. TECH STACK

| Layer | Technology | Why |
|-------|-----------|-----|
| Game Engine | Unity 2022.3 LTS | Stable, mobile-optimized, URP support |
| Render Pipeline | Universal Render Pipeline (URP) | Best post-processing on mobile |
| Target Platform | Android (primary), iOS (secondary) | APK for demo, TestFlight for iOS |
| Backend / Nemesis Brain | Python FastAPI server | Lightweight, fast to build |
| Nemesis State Storage | Firebase Firestore (free tier) | Persistent per-device boss state |
| AI Reasoning | Gemini 3 Flash via API (free) | Boss adaptation decisions + taunt generation |
| Animation | Unity Animator + Mixamo | Free rigged animations |
| UI | Unity UI Toolkit (UXML/USS) | Cleaner than legacy Canvas for complex HUDs |

---

## 2. SYSTEM ARCHITECTURE DIAGRAM

```
┌─────────────────────────────────────────────────────┐
│                  MOBILE CLIENT (Unity)               │
│                                                     │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│  │ Game Loop │  │  Boss    │  │  Combat Logger   │  │
│  │ Manager  │  │  Controller│ │  (logs all events)│  │
│  └────┬─────┘  └────┬─────┘  └────────┬─────────┘  │
│       │             │                  │             │
│       └─────────────┴──────────────────┘             │
│                       │                              │
│              ┌─────────▼──────────┐                  │
│              │   APIManager.cs    │                  │
│              │ (Unity HTTP Client) │                 │
│              └─────────┬──────────┘                  │
└────────────────────────│────────────────────────────┘
                         │ REST API calls
                         ▼
┌─────────────────────────────────────────────────────┐
│             PYTHON FASTAPI BACKEND                   │
│                                                     │
│  POST /analyze-death   → Death Analyst Agent         │
│  GET  /nemesis-state   → Load current adaptations    │
│  POST /generate-taunt  → Taunt Generator Agent       │
│  POST /fairness-check  → Fairness Validator Agent    │
│                                                     │
│  ┌──────────────────────────────────────────────┐   │
│  │           NEMESIS AGENT SYSTEM               │   │
│  │                                              │   │
│  │  [Death Analyst] → [Adaptation Selector]     │   │
│  │       ↓                    ↓                 │   │
│  │  [Fairness Check]    [Taunt Generator]       │   │
│  │       ↓                    ↓                 │   │
│  │  [State Writer] ←──────────┘                 │   │
│  └──────────────────────────────────────────────┘   │
│                        │                            │
│              Gemini 3 Flash API                     │
└────────────────────────│────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────┐
│              FIREBASE FIRESTORE                      │
│                                                     │
│  Collection: nemesis_states                         │
│  Document: {device_id}                              │
│  Fields: total_deaths, adaptations_active,          │
│          taunt_queue, personality_shift, stats      │
└─────────────────────────────────────────────────────┘
```

---

## 3. UNITY PROJECT STRUCTURE

```
Assets/
├── _Game/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs          # Game state machine
│   │   │   ├── SceneLoader.cs          # Async scene transitions
│   │   │   └── AudioManager.cs         # Global audio control
│   │   ├── Player/
│   │   │   ├── PlayerController.cs     # Movement, dodge, lock-on
│   │   │   ├── PlayerCombat.cs         # Attack system, throwables
│   │   │   ├── PlayerStats.cs          # HP, stamina, death handling
│   │   │   └── CombatLogger.cs         # Logs every combat event
│   │   ├── Boss/
│   │   │   ├── BossController.cs       # Boss state machine (3 stages)
│   │   │   ├── BossAttackSystem.cs     # All attack behaviors
│   │   │   ├── BossAdaptation.cs       # Reads nemesis_state, applies adaptations
│   │   │   ├── BossStageManager.cs     # Stage transitions + cutscenes
│   │   │   └── SaplingEnemy.cs         # Stage 2 mini-enemy
│   │   ├── Nemesis/
│   │   │   ├── NemesisAPIManager.cs    # HTTP calls to Python backend
│   │   │   ├── CombatEventData.cs      # Data classes for logging
│   │   │   └── NemesisStateCache.cs    # Local cache of nemesis_state
│   │   ├── Cinematics/
│   │   │   ├── CinematicDirector.cs    # Triggers Unity Timeline sequences
│   │   │   ├── IntroSequence.cs        # Hero intro logic
│   │   │   ├── BossIntroSequence.cs    # Boss intro + retry taunt
│   │   │   └── VictorySequence.cs      # Victory cinematic
│   │   └── UI/
│   │       ├── HUDController.cs        # In-fight HUD updates
│   │       ├── DeathScreenUI.cs        # YOU FELL screen + taunt display
│   │       ├── MainMenuUI.cs           # Start menu logic
│   │       └── SettingsUI.cs           # Settings panel
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   ├── HeroIntro.unity             # Opening cinematic scene
│   │   └── BossFight.unity             # The main arena
│   ├── Animations/
│   │   ├── Player/                     # Mixamo animations retargeted
│   │   └── Boss/                       # Boss stage animations
│   ├── VFX/
│   │   ├── FireFlask_VFX.prefab
│   │   ├── RootBurst_VFX.prefab
│   │   ├── DivineGlow_VFX.prefab
│   │   └── StageTransition_VFX.prefab
│   ├── Materials/
│   │   ├── BossStage1_Mat.mat
│   │   ├── BossStage2_Mat.mat
│   │   ├── BossStage3_Mat.mat          # Translucent shader
│   │   └── ArenaGround_Mat.mat
│   └── UI/
│       ├── HUD.uxml
│       ├── MainMenu.uxml
│       ├── DeathScreen.uxml
│       └── Settings.uxml
├── Plugins/
│   └── Firebase/                       # Firebase Unity SDK
└── StreamingAssets/
    └── nemesis_state_local.json        # Fallback if backend unreachable
```

---

## 4. KEY SCRIPT SPECIFICATIONS

### 4.1 CombatLogger.cs
Attach to: Player GameObject  
Purpose: Records every combat event during the fight.

```csharp
// Records every 100ms and on every player action
public class CombatLogger : MonoBehaviour
{
    public static CombatLog CurrentSession;
    
    // Called by PlayerCombat.cs on every dodge
    public void LogDodge(Vector2 direction) { ... }
    
    // Called by PlayerCombat.cs on every attack
    public void LogAttack(AttackType type) { ... }
    
    // Called by PlayerStats.cs when HP drops
    public void LogRetreat(float hpPercent, float distanceFromBoss) { ... }
    
    // Called by PlayerStats.cs on death
    public CombatLog FinalizeAndGetLog(string deathCause) { ... }
}
```

### 4.2 NemesisAPIManager.cs
Attach to: GameManager GameObject  
Purpose: All communication with Python backend.

```csharp
public class NemesisAPIManager : MonoBehaviour
{
    private const string BASE_URL = "https://your-backend-url.com";
    
    // Called immediately on player death, before death screen
    public async Task<NemesisResponse> AnalyzeDeath(CombatLog log) { ... }
    
    // Called when boss fight scene loads
    public async Task<NemesisState> LoadNemesisState(string deviceId) { ... }
    
    // Returns current taunt from queue
    public async Task<string> GetNextTaunt() { ... }
}
```

### 4.3 BossAdaptation.cs
Attach to: Boss GameObject  
Purpose: Reads active adaptations from NemesisState and modifies BossAttackSystem behavior.

```csharp
public class BossAdaptation : MonoBehaviour
{
    // Called by BossController on fight start, after NemesisState is loaded
    public void ApplyAdaptations(NemesisState state)
    {
        foreach (var adaptation in state.adaptations_active)
        {
            switch (adaptation)
            {
                case "PUNISH_RIGHT_DODGE":
                    // Bias MIRROR_ATTACK to track right-dodge
                    break;
                case "AGGRESS_AT_35PCT_HP":
                    // Set aggression flag at 35% player HP
                    break;
                // etc.
            }
        }
    }
}
```

### 4.4 BossController.cs (State Machine)
```
States:
  IDLE → STAGE_1_COMBAT → STAGE_1_TRANSITION
  → STAGE_2_COMBAT → STAGE_2_TRANSITION
  → STAGE_3_COMBAT → DEATH

Transitions triggered by: HP thresholds (60%, 30%, 0%)
Each transition: pause combat → play cutscene → resume
```

---

## 5. PYTHON BACKEND STRUCTURE

```
nemesis-backend/
├── main.py                  # FastAPI app, all routes
├── agents/
│   ├── death_analyst.py     # Analyzes combat log, identifies patterns
│   ├── adaptation_selector.py # Chooses which adaptations to apply
│   ├── fairness_checker.py  # Ensures fight is still winnable
│   └── taunt_generator.py   # Generates contextual boss dialogue
├── models/
│   ├── combat_log.py        # Pydantic models for all data
│   └── nemesis_state.py
├── services/
│   ├── firebase_service.py  # Firestore read/write
│   └── gemini_service.py    # Gemini API wrapper
├── config.py                # API keys, constants
└── requirements.txt
```

---

## 6. API CONTRACTS

### POST /analyze-death
```json
Request:
{
  "device_id": "abc123",
  "attempt_id": "uuid",
  "death_cause": "VINE_WHIP",
  "combat_log": {
    "events": [...],
    "stats": {
      "dodge_right_count": 14,
      "dodge_left_count": 3,
      "light_attack_ratio": 0.78,
      "heavy_attack_ratio": 0.22,
      "retreat_hp_threshold": 0.35,
      "avg_distance": 4.2,
      "throwables_used": {"flask": 1, "tablet": 0, "spear": 2}
    }
  }
}

Response:
{
  "adaptations_applied": ["PUNISH_RIGHT_DODGE", "AGGRESS_AT_35PCT_HP"],
  "taunt": "You always break right. The forest has memorized your shadow.",
  "total_deaths": 4,
  "reasoning_trace": "Player dodged right 82% of the time...",
  "mercy_applied": false
}
```

### GET /nemesis-state/{device_id}
```json
Response:
{
  "total_deaths": 4,
  "adaptations_active": ["PUNISH_RIGHT_DODGE", "AGGRESS_AT_35PCT_HP"],
  "taunt_queue": ["You always break right..."],
  "personality_shift": "aggressive",
  "mercy_mode": false
}
```

---

## 7. FIREBASE SCHEMA

```
Collection: nemesis_states
  Document: {device_id}
    - total_deaths: number
    - adaptations_active: string[]     (max 4)
    - taunt_queue: string[]            (consume from index 0)
    - personality_shift: "methodical" | "aggressive" | "wrathful"
    - mercy_mode: boolean
    - last_updated: timestamp
    - attempt_history: subcollection
        Document: {attempt_id}
          - death_cause: string
          - stats: object
          - timestamp: timestamp
```

---

## 8. ENVIRONMENT SETUP

```bash
# Unity version
Unity 2022.3.x LTS

# Required Unity packages (install via Package Manager)
- Universal RP (14.x)
- Visual Effect Graph (14.x)
- Timeline (1.7.x)
- Cinemachine (2.9.x)         ← CRITICAL for cinematic cameras
- Input System (1.6.x)        ← For mobile touch input
- Firebase SDK for Unity       ← From Firebase console

# Python backend
Python 3.11+
pip install fastapi uvicorn firebase-admin google-generativeai pydantic python-dotenv

# Deploy backend to
Google Cloud Run (free tier: 2M requests/month)
OR
Railway.app (free tier sufficient for hackathon)
```

---

## 9. BUILD TARGETS

```
Android:
- Min SDK: API 26 (Android 8.0)
- Target SDK: API 34
- Architecture: ARM64
- Graphics API: Vulkan (primary), OpenGL ES 3.0 (fallback)
- Compression: LZ4HC

iOS:
- Min iOS: 14.0
- Architecture: ARM64
- Graphics API: Metal
```

---

## END OF ARCHITECTURE DOCUMENT
## Next: See AGENT_WORKFLOWS.md for Nemesis agent definitions
