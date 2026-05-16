# Antigravity Project Rules — NEMESIS: The Eternal Guardian

## Identity
You are building a 3D mobile action RPG boss fight with an AI-driven Nemesis system.
Project files are in two parts: a Unity game and a Python FastAPI backend.

## Always Read First
Before starting any task, read the relevant knowledge base file:
- Game mechanics/design → .antigravity/knowledge/GAME_DESIGN_DOCUMENT.md
- File structure/scripts → .antigravity/knowledge/TECHNICAL_ARCHITECTURE.md  
- Build order/agents → .antigravity/knowledge/AGENT_WORKFLOWS.md

## Unity Rules
- Unity version: 2022.3 LTS only
- Render pipeline: Universal RP (URP) — never Built-in RP
- UI: UI Toolkit (UXML/USS) — never legacy Canvas/UGUI
- Input: Unity Input System package — never legacy Input.GetKey
- Animation: Animator Controller with named triggers — never Animation.Play()
- Null safety: Always null-check component references before use
- ScriptableObjects: Use for all tunable values (HP, damage, cooldowns, timings)
- Coroutines: Only for timing/sequencing. Use async/await for API calls.
- Naming: PascalCase classes/methods, _camelCase private fields, UPPER_SNAKE constants

## Python Backend Rules
- Python 3.11+
- FastAPI with Pydantic v2 models
- Type hints on every function parameter and return type
- Docstring on every function explaining input, output, and side effects
- Every Gemini API call must have: try/except, timeout handling, JSON validation
- Every Firebase call must have: error handling, offline fallback
- Agent reasoning_trace must be preserved in every response object (for demo)

## Game Design Rules
- Do not add mechanics not described in GAME_DESIGN_DOCUMENT.md
- Do not change mythology setting, hero name (ZILAR), or boss name (HUMBABA)
- Boss has exactly 3 stages at 100→60→30→0% HP thresholds
- Throwables: Flask x3, Tablet x2, Spear x3 — not replenished mid-fight
- Nemesis adaptations: maximum 4 active at any time
- Mercy mechanic triggers at death count >= 8 (removes one adaptation)
- All taunts must reference specific player behavior, maximum 15 words

## Never
- Commit API keys, .env files, or Firebase service account JSON to git
- Use AI-generated placeholder assets for final build (label all placeholders)
- Write magic numbers — everything goes in a ScriptableObject or config file
- Add console.log / Debug.Log spam in production paths
- Change the build order in AGENT_WORKFLOWS.md without team agreement
