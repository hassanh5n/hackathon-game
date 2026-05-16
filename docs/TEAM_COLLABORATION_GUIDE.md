# TEAM COLLABORATION GUIDE
## Working with Google Antigravity — Free, Multi-Account, Team Setup
### Read this before touching any code

---

## 1. WHAT IS ANTIGRAVITY (QUICK RECAP)

Antigravity is Google's free agentic IDE. It's a VS Code fork where instead of writing code line by line, you describe what you want and AI agents plan, write, test, and verify it autonomously.

**Download:** https://antigravity.google/download  
**Login:** Personal Gmail account (free, no credit card)  
**Primary model:** Gemini 3 Pro (free, generous rate limits)  
**Also available:** Claude Sonnet 4.6, Gemini 3 Flash, GPT-OSS 120B

---

## 2. HOW THE FREE TIER WORKS

- **Gemini 3 Pro:** Free with rate limit refresh every 5 hours
- **Gemini 3 Flash:** Faster, higher rate limits, use for simpler tasks
- **Claude Sonnet 4.6:** Available in Antigravity — use for architecture/reasoning tasks
- **Rate limit hit?** Switch to another model or wait for refresh

**Pro tip:** If you're burning through rate limits on a complex task, switch to Claude Sonnet 4.6 mid-session. Antigravity lets you toggle models per-task without losing context.

---

## 3. TEAM SETUP (DIFFERENT ACCOUNTS, SAME PROJECT)

Antigravity does NOT have native team collaboration yet. Here's how to work together:

### Step 1: Set Up the Shared Repo
```bash
# One person (team lead) creates the repo
git init nemesis-game
cd nemesis-game

# Create the folder structure
mkdir -p .antigravity/knowledge
mkdir -p Unity/Assets
mkdir -p backend

# Add ALL knowledge base files here (GDD, Architecture, Workflows, this guide)
# These files go into .antigravity/knowledge/
# Antigravity reads them automatically when you open the folder

git add .
git commit -m "Initial project structure + Antigravity knowledge base"
git remote add origin https://github.com/YOUR_ORG/nemesis-game.git
git push -u origin main
```

### Step 2: Each Team Member
```bash
# Clone the repo
git clone https://github.com/YOUR_ORG/nemesis-game.git

# Open in Antigravity
# File → Open Folder → select nemesis-game/

# Antigravity will auto-read all files in .antigravity/knowledge/
# It now knows the full GDD, architecture, and agent workflows
```

### Step 3: Branch Strategy
Each person works on their own branch. Never commit to main directly.

```
main           — stable, demo-ready code only
dev            — integration branch
feature/unity-player      — Person A
feature/unity-boss        — Person B  
feature/nemesis-backend   — Person C
feature/cinematics-ui     — Person D
```

```bash
# Start a task
git checkout -b feature/unity-player

# After Antigravity builds something and you verify it works
git add .
git commit -m "feat: PlayerController movement + lock-on"
git push origin feature/unity-player

# Create PR on GitHub → team lead reviews → merges to dev
# When dev is stable → merge to main
```

---

## 4. WHO BUILDS WHAT (TEAM DIVISION)

Assuming a team of 3-4 people. Adjust as needed.

### Person A — Unity Frontend (Player + Boss)
**Tasks:** 1–13 from Build Order  
**Antigravity tasks to delegate:**
- "Implement PlayerController.cs for a mobile 3D action RPG with left joystick movement and lock-on target system. Reference TECHNICAL_ARCHITECTURE.md for the script spec."
- "Implement BossController.cs as a 3-stage state machine. Stage transitions at 60% and 30% HP. Reference GAME_DESIGN_DOCUMENT.md section 3 for full boss behavior."
- "Implement all Stage 1 boss attacks: GROUND_SLAM, VINE_WHIP, FOREST_ROAR, STOMP. Each attack needs an animation trigger, hitbox activation window, and damage event."

### Person B — Nemesis Backend
**Tasks:** 14–25 from Build Order  
**Antigravity tasks to delegate:**
- "Set up a Python FastAPI project with the structure in TECHNICAL_ARCHITECTURE.md section 5. Install all dependencies from requirements.txt."
- "Implement the death_analyst.py agent. It calls Gemini 3 Flash with the prompt in AGENT_WORKFLOWS.md Pipeline Step 1 and returns a parsed JSON AnalysisResult."
- "Implement POST /analyze-death endpoint that runs the full 4-step Nemesis pipeline and saves result to Firebase."

### Person C — Cinematics + UI
**Tasks:** 26–35 from Build Order  
**Antigravity tasks to delegate:**
- "Create the hero intro cinematic using Unity Timeline. Scene description in GDD section 5.1. Use Cinemachine virtual cameras for camera movement."
- "Implement DeathScreenUI using Unity UI Toolkit (UXML). Shows YOU FELL text, taunt string from NemesisAPIManager, and two buttons: RISE AGAIN and RETURN TO SANCTUM."
- "Create the MainMenu.unity scene with animated cedar forest background, three buttons, and the atmospheric audio setup from GDD section 8."

### Person D (or split with others) — Integration + QA
**Tasks:** 36–40 from Build Order  
- Merge branches, resolve conflicts
- Test end-to-end nemesis flow on physical device
- Record demo video
- Prepare architecture map for judges

---

## 5. HOW TO PROMPT ANTIGRAVITY EFFECTIVELY

This is "vibe coding" — you describe intent, agent executes.

### DO THIS ✅
```
"Implement CombatLogger.cs for Unity 2022.3. It should attach to the Player 
GameObject and log every dodge event (with direction vector) and every attack 
event (light or heavy) to a CombatLog object. On player death, FinalizeAndGetLog() 
returns the complete log. Reference TECHNICAL_ARCHITECTURE.md section 4.1 for 
the full spec. Use null-safe patterns throughout."
```

```
"The BossAdaptation.cs script needs to read the nemesis_state JSON loaded by 
NemesisAPIManager and modify BossAttackSystem behavior. For PUNISH_RIGHT_DODGE: 
bias the MIRROR_ATTACK to trigger when player dodges right (dodge direction.x > 0). 
Reference AGENT_WORKFLOWS.md AVAILABLE_ADAPTATIONS for the full list of what 
each adaptation does."
```

### DON'T DO THIS ❌
```
"Make the boss harder"  ← too vague, agent invents things not in GDD
"Build the game"        ← too broad, agent won't know where to start
"Fix the bug"           ← give it the error message and the file
```

### Handling Rate Limits
When you see "Rate limit reached":
1. Switch model: in Agent Manager, click the model selector, switch to Gemini 3 Flash
2. Or: switch to Claude Sonnet 4.6 (also free in Antigravity)
3. Or: wait ~5 hours for Gemini 3 Pro refresh
4. Large tasks: break into smaller sub-tasks to use less tokens per call

### Using Deep Think
For complex architecture decisions (not routine coding), enable Deep Think:
- Click the brain icon next to the model selector
- Use for: designing the Nemesis pipeline, planning the boss state machine
- Don't use for: writing CSS, simple scripts — it's slow and wastes tokens

---

## 6. ANTIGRAVITY MANAGER VIEW (YOUR MISSION CONTROL)

The Manager View is where the real power is. Use it to run multiple agents in parallel.

```
Manager View → New Task → [describe task] → Assign to agent

Example parallel setup:
Agent 1: "Implement PlayerController.cs and PlayerCombat.cs"
Agent 2: "Implement death_analyst.py and adaptation_selector.py"
Agent 3: "Create HUD.uxml with all elements from GDD section 6.3"

All three run simultaneously. You monitor from Manager View.
```

Each agent produces **Artifacts** — task lists, implementation plans, file diffs.  
You review the Artifact, leave a comment if something is wrong, and the agent adjusts **without stopping**.

---

## 7. RULES FILE FOR ANTIGRAVITY

Create this file in your project root. Antigravity reads it automatically.

**File: `.antigravity/rules.md`**
```markdown
# Project Rules

## Always
- Read .antigravity/knowledge/GAME_DESIGN_DOCUMENT.md before building any feature
- Read .antigravity/knowledge/TECHNICAL_ARCHITECTURE.md for file locations and script specs
- Follow the build order in .antigravity/knowledge/AGENT_WORKFLOWS.md
- Use Unity 2022.3 LTS APIs only
- Use UI Toolkit (UXML/USS) not legacy Canvas
- Add error handling on every API call (timeout, null, network failure)
- Add XML doc comments on every public method

## Never
- Add game mechanics not described in GDD without asking
- Use deprecated Unity APIs (FindObjectOfType in performance-critical code, etc.)
- Hard-code any values that should be configurable (HP, damage, cooldowns → ScriptableObjects)
- Commit API keys or secrets to git
- Write code without null checks on Unity component references

## When Stuck
- Check if the GDD describes the exact behavior
- Check if TECHNICAL_ARCHITECTURE.md has the script spec
- Ask in a new agent task: "I'm implementing X. The GDD says Y but I'm seeing Z. How should I handle this?"
```

---

## 8. FREE TOOLS STACK (ZERO COST)

| Tool | Purpose | Free Tier |
|------|---------|-----------|
| Google Antigravity | IDE + agent coding | Fully free (Gmail account) |
| Unity 2022.3 LTS | Game engine | Free for teams < $200K revenue |
| Mixamo | Hero animations | Fully free (Adobe account) |
| Sketchfab | 3D assets | Free CC-licensed models |
| Firebase Firestore | Nemesis state storage | Free (1GB, 50K reads/day) |
| Google AI Studio | Gemini API directly | Free (15 req/min, Gemini 2.0 Flash) |
| Railway.app | Backend hosting | Free tier (500 hrs/month) |
| GitHub | Code repo + collaboration | Free public/private repos |
| Pixabay | Royalty-free music | Fully free |
| Freesound.org | SFX | Fully free (CC licensed) |
| Figma | UI design | Free (3 projects) |

**Total cost: $0** for the prototype.

---

## 9. DEMO VIDEO GUIDE

The demo is 3 minutes. Structure it exactly like this:

```
0:00–0:20  Start menu → tap BEGIN JOURNEY → hero intro cutscene plays
0:20–0:45  First fight attempt. Play normally. Die deliberately.
0:45–1:15  SHOW THE AGENT TRACE. Toggle debug panel. Camera lingers on:
           - OBSERVATION lines (what patterns were found)
           - DECISION lines (what adaptations were applied)
           - Generated taunt displayed on death screen
1:15–1:50  Second attempt. Boss now has adapted behavior. Show the boss 
           doing the specific adaptation (punishing your dodge direction, etc.)
           Let Humbaba's taunt appear.
1:50–2:30  Continue fight through all 3 stages. Stage transitions are cinematic.
           Die again if needed — show mercy mechanic (adaptation removed).
2:30–3:00  Win the fight. Victory cinematic plays. Daughter scene.
           Brief architectural callout: "Nemesis backend ran on Railway, 
           5 agents in pipeline, zero hard-coded rules."
```

---

## 10. COMMON ANTIGRAVITY ISSUES & FIXES

| Issue | Fix |
|-------|-----|
| Agent keeps rewriting the same file | Tell it: "The file already exists at X. Only modify function Y." |
| Agent hallucinates a Unity API | Tell it: "That API doesn't exist in Unity 2022.3. Use [correct API] instead." |
| Rate limit hit mid-task | Switch model to Gemini 3 Flash or Claude Sonnet 4.6 |
| Agent writes code but doesn't save | Check editor → file should auto-save; if not, ask agent to "save all modified files" |
| Context gets too long (UI lag) | Start a new task. Put relevant file content in the task description. |
| Agent makes too many changes at once | Use Plan Mode first: "Plan only, don't write code yet" → review plan → approve |

---

## END OF TEAM GUIDE
## You are ready to build. Start with Task 1 in AGENT_WORKFLOWS.md Build Order.
