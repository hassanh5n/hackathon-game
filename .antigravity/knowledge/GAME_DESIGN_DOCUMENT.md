# NEMESIS: THE ETERNAL GUARDIAN
## Game Design Document v1.0
### For Antigravity Agent Reference — Read This First

---

## 1. OVERVIEW

**Game Name:** NEMESIS: The Eternal Guardian
**Genre:** 3D Action RPG / Soulslike — Single Boss Fight
**Platform:** Mobile (Android & iOS) — Unity 2022 LTS + URP
**Mythology Setting:** Ancient Mesopotamian (Sumerian/Akkadian) — completely untouched in mainstream gaming
**Core Hook:** A boss that learns how YOU fight. Every time you die and return, the boss has adapted specifically to counter your personal playstyle. It taunts you with your own habits.

---

## 2. NARRATIVE

### 2.1 World & Setting
Ancient Mesopotamia, circa 3000 BCE. The Cedar Forest — a divine realm at the edge of the known world — has been sealed by Enlil, god of wind and storms. A creature called **Humbaba the Eternal** guards it. He is not simply a monster. He is a divine instrument — a god-made judge who has seen ten thousand warriors fall.

The Cedar Forest bleeds a curse into the lands of Uruk. Crops die. Rivers dry. Children fall into dreamless sleep.

### 2.2 Hero: ZILAR
- A soldier of Uruk's sacred guard
- His daughter was taken by the Cedar Forest's curse — her body lives but her soul is trapped inside the Forest
- Armed with a **Khopesh** (curved ceremonial blade, fixed weapon) and a satchel of **throwables**
- Not a chosen hero. A desperate father.
- His purpose: reach the heart of the Cedar Forest and shatter the divine seal. The boss is the only thing between him and his daughter.

### 2.3 Throwables (Limited Use Per Fight)
| Item | Effect | Visual |
|------|--------|--------|
| Sacred Oil Flask | Fire damage over time, 5 seconds | Shatters into orange flame on contact |
| Clay Tablet | Stun the boss for 1.5 seconds | Glows with cuneiform script, bursts into dust |
| Bone Spear | High single-target damage, ranged | Ancient carved bone, pierces through |

Throwable count: Flask x3, Tablet x2, Spear x3 per attempt. Not replenished mid-fight.

---

## 3. BOSS DESIGN: HUMBABA THE ETERNAL

### 3.1 Overview
Humbaba is a 3-stage boss fought in a single continuous encounter. Each stage transition happens at HP thresholds. He evolves visually and mechanically at each stage. He never resets between stages — it is one fight.

### 3.2 Stage 1 — "The Warden" (100% → 60% HP)
**Appearance:** Massive humanoid, 3x Zilar's height. Skin like ancient bark. Eyes of amber fire. Carries a cedar-wood club. Slow but devastating.

**Attack Set:**
- `GROUND_SLAM` — raises club, slams down in a cone. High damage, telegraphed.
- `VINE_WHIP` — extends cedar vines from arm. Long range. Medium damage.
- `FOREST_ROAR` — AOE shockwave, pushes player back. No damage but staggers.
- `STOMP` — short range, high damage. Used when player is directly underfoot.

**Behavior:** Methodical. Deliberate. Tracking player patiently.

### 3.3 Stage 2 — "The Merged" (60% → 30% HP)
**Transition Cutscene (4 seconds):** Humbaba plunges his fist into the ground. The earth cracks. Cedar roots erupt across the arena. His left arm becomes a living tree. His roar shakes the camera.

**New Appearance:** Left arm replaced by massive tree trunk. Roots visible through skin. Eyes now glow brighter. Moves faster.

**Added Attacks:**
- `ROOT_BURST` — roots erupt from ground around player position. Must dodge before they surface (0.8 second warning on ground).
- `SAPLING_SUMMON` — summons 2 small cedar saplings (mini-enemies). They chase player. Boss regenerates 2% HP per sapling alive.
- `TREE_SLAM` — uses tree-arm to create a large shockwave. Covers half the arena.

**Behavior:** More aggressive. Less predictable. Starts learning player patterns from this stage onward (Nemesis System activates).

### 3.4 Stage 3 — "The Divine" (30% → 0% HP)
**Transition Cutscene (5 seconds):** The arena shakes violently. Cedar Forest energy pours from Humbaba's chest. His body becomes translucent, ethereal. Ancient cuneiform glows across his form. His voice becomes layered — divine and terrible. The music changes to something overwhelming.

**New Appearance:** Semi-translucent. Glowing cuneiform script across body. Fire-amber eyes. Floats slightly off ground.

**Added Attacks:**
- `DIVINE_FIRE` — channels golden fire in a sweeping beam. Must be dodged through (not away).
- `PHASE_STEP` — teleports behind player. Immediate follow-up attack.
- `TABLET_STORM` — rains ancient clay tablets from above. Area denial.
- `MIRROR_ATTACK` — uses a move specifically countering the player's most used dodge direction (Nemesis data used here).

**Behavior:** Fast. Relentless. Fully weaponizes Nemesis data. This is where the boss feels personal.

---

## 4. THE NEMESIS SYSTEM

This is the AI brain of the game. It is NOT hard-coded rule-based behavior. It uses structured agent reasoning.

### 4.1 Data Collected Per Attempt
Every fight attempt logs:
```json
{
  "attempt_id": "uuid",
  "timestamp": "...",
  "death_cause": "ATTACK_NAME",
  "combat_log": [
    {
      "event": "player_dodge",
      "direction": "right",
      "timestamp_ms": 1200
    },
    {
      "event": "player_attack",
      "type": "light",
      "timestamp_ms": 1450
    }
  ],
  "stats": {
    "dodge_right_count": 14,
    "dodge_left_count": 3,
    "light_attack_ratio": 0.78,
    "heavy_attack_ratio": 0.22,
    "throwable_usage": {"flask": 1, "tablet": 0, "spear": 2},
    "retreat_below_hp_pct": 0.35,
    "avg_distance_from_boss": 4.2
  }
}
```

### 4.2 Agent Reasoning (Post-Death)
The **Death Analyst Agent** runs after every player death:

**Input:** Full combat log  
**Process:**
1. Identify dominant behavioral patterns (dodge bias, range preference, etc.)
2. Cross-reference patterns with available boss adaptations
3. Select 2-3 adaptations to apply next encounter
4. Check fairness: ensure the fight is still winnable with adaptations applied
5. Generate taunt dialogue reflecting observed behavior
6. Write adaptation decisions with reasoning to `nemesis_state.json`

**Output Example:**
```
OBSERVATION: Player dodged right in 14/17 dodge events (82%).
OBSERVATION: Player prefers light attacks (78% of attacks).
OBSERVATION: Player retreats when HP drops below 35%.

DECISION: Apply MIRROR_ATTACK biased to punish right-dodge.
DECISION: Boss will track player position at 35% HP threshold and use VINE_WHIP immediately.
DECISION: Increase Boss aggression window after light attack chains > 3.

FAIRNESS_CHECK: With these adaptations, difficulty score = 6.8/10. Acceptable.

TAUNT: "You always break right. The forest has memorized your shadow."
```

### 4.3 Adaptation Storage
Stored in `nemesis_state.json` (Firebase Firestore in production, local JSON for prototype):
```json
{
  "total_deaths": 4,
  "adaptations_active": [
    "PUNISH_RIGHT_DODGE",
    "AGGRESS_AT_35PCT_HP",
    "CHAIN_PUNISH_AFTER_LIGHT_x3"
  ],
  "taunt_queue": [
    "You always break right. The forest has memorized your shadow.",
    "Four times. Four lessons. When will you learn?"
  ],
  "boss_personality_shift": "aggressive"
}
```

### 4.4 Escalation Cap
- Max active adaptations: 4 (to prevent unfairness)
- After 8 deaths, a **mercy mechanic** triggers: one active adaptation is removed. The boss still taunts but becomes slightly more beatable.
- This is also agent-decided, with logged reasoning.

---

## 5. CINEMATIC SEQUENCES

### 5.1 Hero Intro (Plays on first launch, skippable after)
**Duration:** ~45 seconds  
**Style:** In-engine cutscene with slow camera push + voiceover (Sumerian-accented English)

1. Black screen. Sound of wind through cedar trees.
2. Slow pan across a dying village. Crops withered. Sky amber.
3. Close-up of Zilar's hands wrapping a child's bracelet around his wrist.
4. He looks up — a towering cedar forest in the distance, unnatural green against the dead landscape.
5. Cut: Zilar walking toward the forest entrance. Dust billowing.
6. Text appears: *"My daughter's soul is inside the forest. One god stands between us."*
7. Cut to black. Then: Boss roar heard. Eyes open in darkness.
8. Title card: **NEMESIS: THE ETERNAL GUARDIAN**

### 5.2 Boss Intro (Plays on first encounter, shortened on retry)
**Duration:** ~20 seconds (7 seconds on retry)

**First encounter:**
1. Zilar enters the arena — a clearing inside the Cedar Forest. Ancient braziers lit with amber fire.
2. The trees part. Humbaba emerges slowly. Massive. Deliberate.
3. Camera circles him slowly. Cuneiform text appears naming him: **HUMBABA — GUARDIAN OF THE CEDAR**
4. He looks down at Zilar. No words. Just a slow tilt of his massive head — almost curious.
5. The arena locks. Fight begins.

**Retry (after death):**
1. Zilar rises at the arena entrance.
2. Humbaba already there, waiting. His amber eyes narrow.
3. Taunt dialogue appears as cinematic subtitle, AI-generated.
4. Fight begins.

### 5.3 Stage Transition Cutscenes
Short (3-5 seconds each) — described in Boss Design section.  
Must be interruptible only by stage HP threshold being hit.  
Camera: dramatic angle, slow motion, particle burst.

### 5.4 Victory Ending Cinematic
**Duration:** ~60 seconds  
**Triggers:** Boss HP reaches 0 in Stage 3.

1. Slow-motion final strike. Time nearly stops. Cuneiform script shatters off Humbaba's body.
2. He falls to one knee. Not defeated in rage — defeated with recognition. He looks at Zilar with something like respect.
3. **Humbaba speaks (only time he speaks):** *"Ten thousand warriors. None fought for something real. You... are different."*
4. He dissolves into amber light and cedar leaves.
5. The forest parts. A golden path opens.
6. Zilar walks forward. At the end: his daughter, standing in golden light, bracelet on her wrist matching his.
7. He kneels. Takes her hand. She opens her eyes.
8. Cut to black. A single cedar leaf falls across the screen.
9. Credits roll over the Cedar Forest at peace — green, alive, beautiful.

---

## 6. SCREENS & MENUS

### 6.1 Start Menu
- Background: Animated Cedar Forest at dusk, amber sky, wind through leaves.
- Humbaba's silhouette barely visible deep in the forest.
- Buttons: **BEGIN JOURNEY** | **SETTINGS** | **CREDITS**
- No distracting UI. Minimal. Dark. Atmospheric.

### 6.2 Settings Menu
- Audio: Master Volume, Music Volume, SFX Volume
- Graphics: Quality (Low/Medium/High) — auto-detected on first launch
- Controls: Toggle haptic feedback, adjust joystick sensitivity
- Accessibility: Subtitle size, color-blind mode (boss attack indicators)
- Language: English only for prototype

### 6.3 In-Fight HUD
- **Player HP:** Bottom left. Segmented red bar. When critical (<20%), pulses and screen edge vignette deepens.
- **Stamina:** Below HP bar. Blue. Drains on dodge and heavy attack. Regenerates passively.
- **Throwables:** Bottom right. Icon + count for each throwable.
- **Boss HP Bar:** Full width at top. Segmented into 3 sections (one per stage). Name above it.
- **Boss Stage Indicator:** Small icon near boss HP bar changes per stage.
- **NO floating damage numbers** — keep it immersive. Screen feedback is the damage indicator.

### 6.4 Death Screen
- Slow fade to red-black.
- Text: **YOU FELL** — appears letter by letter in ancient script style.
- Below it: AI-generated taunt from boss (one sentence, in italics).
- Two buttons: **RISE AGAIN** | **RETURN TO SANCTUM** (main menu)
- Sound: A low, resonant horn. Silence. Then the boss's breathing.

### 6.5 Pause Menu
- Translucent overlay on game.
- Options: **RESUME** | **SETTINGS** | **ABANDON** (back to main menu with confirmation)

---

## 7. CONTROLS (MOBILE)

```
[ LEFT JOYSTICK ]     — Move Zilar
[ TAP screen right ]  — Light Attack
[ HOLD screen right ] — Heavy Attack  
[ SWIPE any dir ]     — Dodge Roll (direction = swipe direction)
[ THROWABLE ICONS ]   — Tap to throw (bottom right HUD)
[ LOCK-ON BUTTON ]    — Auto-lock camera to boss (top right, small)
```

Lock-on is HIGHLY recommended and should be default. Without it, tracking Humbaba in 3D on mobile is disorienting.

---

## 8. AUDIO DIRECTION

### Music
- **Exploration/Menu:** Ambient Sumerian flute and duduk. Slow, haunting.
- **Stage 1 Combat:** Driving drums + low brass. Ancient Mesopotamian percussion.
- **Stage 2 Combat:** Adds distorted strings. More intense, less structured.
- **Stage 3 Combat:** Full orchestral chaos. Choir. Overwhelming. This music should make the player feel small.
- **Victory:** Single note sustained. Then gentle cedar-wind theme. Emotional release.

Source: Use royalty-free tracks from Pixabay / freemusicarchive that match this description. Label as placeholder for prototype.

### SFX
- Boss attacks: Deep impact sounds. Cedar cracks. Earth splits.
- Player attacks: Sharp metal, cloth movement.
- Throwables: Glass shatter (flask), stone break (tablet), spear whistle (spear).
- Hit feedback: Haptic feedback on mobile. Short vibration burst.
- Death: Single low drone. No music. Just silence and breathing.

---

## 9. VISUAL DIRECTION

### Color Palette
| Context | Primary | Accent | Environment |
|---------|---------|--------|-------------|
| Menu/Outside forest | Deep navy, black | Amber, gold | Sandy terracotta |
| Stage 1 Arena | Dark brown, forest green | Amber fire from braziers | Cedar bark textures |
| Stage 2 Arena | Deeper green, shadow | Orange-white root glow | Roots erupting |
| Stage 3 Arena | Near-black | Translucent gold, divine white | Floating cedar leaves |

### Post-Processing (Unity URP)
- Bloom: Medium intensity. Especially on boss's eyes and fire.
- Vignette: Constant low. Deepens when player HP is low.
- Chromatic Aberration: Triggers on taking damage. Short burst.
- Film Grain: Subtle. Adds weight.
- Color Grading: Warm amber bias. Shadows cool toward blue.

### Camera Behavior
- Locked behind player (over-shoulder, 3rd person)
- Lock-on mode: circles both player and boss in frame
- On boss attack: slight camera shake (0.2 sec)
- On player death: slow pull-back from player body, then fade

---

## 10. ASSET SOURCES (FOR PROTOTYPE)

| Asset Type | Source | Notes |
|------------|--------|-------|
| Hero character | Mixamo (free) — pick warrior mesh | Apply Mixamo animations (idle, walk, attack, dodge, death) |
| Boss character | Unity Asset Store — creature/monster pack OR Sketchfab (free CC) | Needs rigging if raw mesh |
| Arena environment | Unity Asset Store — "Dark Fantasy Environment" or "Ancient Ruins" | Budget ~$20-30 |
| Particle effects | Unity VFX Graph (built-in) | Fire, root burst, divine glow |
| UI elements | Design in Figma, export as PNG | Ancient stone/gold aesthetic |
| Music | Pixabay — search "Mesopotamian" or "ancient battle" | Label as placeholder |
| SFX | Freesound.org | All free, CC licensed |

---

## END OF GDD
## Next: See TECHNICAL_ARCHITECTURE.md for build details
