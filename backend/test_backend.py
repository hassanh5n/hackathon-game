"""
test_backend.py
───────────────
Manual integration test for the Nemesis Brain API.

Simulates a realistic player session: 5 deaths with distinct behavioral
fingerprints. Run the backend first (uvicorn main:app --reload), then:

    python test_backend.py

Each run fires all 5 deaths in sequence for the same device_id and prints
the taunt + adaptations returned. Watch how the boss tone escalates.

Reset state between runs:
    python test_backend.py --reset
"""

import asyncio
import httpx
import sys
import json

BASE_URL = "http://localhost:8000"
DEVICE_ID = "test_player_001"


# ─────────────────────────────────────────────────────────────────────
# FAKE DEATH SCENARIOS
# Each represents a distinct behavioral fingerprint.
# ─────────────────────────────────────────────────────────────────────
DEATHS = [
    {
        "_label": "Death 1 — Dodge-left addict, short fight",
        "device_id": DEVICE_ID,
        "attempt_id": "attempt_001",
        "death_cause": "boss_sweep_left",
        "combat_log": {
            "events": [
                {"event": "player_dodge",  "direction": "left",  "attack_type": "",      "timestamp_ms": 1200},
                {"event": "player_attack", "direction": "",      "attack_type": "light", "timestamp_ms": 2100},
                {"event": "boss_attack",   "direction": "sweep", "attack_type": "heavy", "timestamp_ms": 3000},
                {"event": "player_dodge",  "direction": "left",  "attack_type": "",      "timestamp_ms": 3050},
                {"event": "player_attack", "direction": "",      "attack_type": "light", "timestamp_ms": 4200},
                {"event": "player_dodge",  "direction": "left",  "attack_type": "",      "timestamp_ms": 5300},
                {"event": "boss_attack",   "direction": "sweep", "attack_type": "heavy", "timestamp_ms": 8900},
            ],
            "stats": {
                "dodge_right_count":  2,
                "dodge_left_count":  14,
                "dodge_forward_count": 1,
                "dodge_back_count":   0,
                "light_attack_count": 9,
                "heavy_attack_count": 1,
                "light_attack_ratio": 0.90,
                "heavy_attack_ratio": 0.10,
                "throwables_used": {"flask": 0, "tablet": 0, "spear": 0},
                "retreat_hp_threshold": 0.0,
                "avg_distance_from_boss": 3.2,
                "total_fight_duration_seconds": 28
            }
        }
    },
    {
        "_label": "Death 2 — Same left-dodge habit, panic-healed at 30% HP",
        "device_id": DEVICE_ID,
        "attempt_id": "attempt_002",
        "death_cause": "boss_slam_during_heal",
        "combat_log": {
            "events": [
                {"event": "player_dodge",  "direction": "left",  "attack_type": "",      "timestamp_ms": 500},
                {"event": "player_attack", "direction": "",      "attack_type": "light", "timestamp_ms": 1100},
                {"event": "player_dodge",  "direction": "left",  "attack_type": "",      "timestamp_ms": 2200},
                {"event": "player_attack", "direction": "",      "attack_type": "heavy", "timestamp_ms": 3400},
                {"event": "player_dodge",  "direction": "left",  "attack_type": "",      "timestamp_ms": 5000},
                {"event": "boss_attack",   "direction": "slam",  "attack_type": "heavy", "timestamp_ms": 6800},
            ],
            "stats": {
                "dodge_right_count":  1,
                "dodge_left_count":  18,
                "dodge_forward_count": 2,
                "dodge_back_count":   0,
                "light_attack_count": 11,
                "heavy_attack_count":  2,
                "light_attack_ratio": 0.85,
                "heavy_attack_ratio": 0.15,
                "throwables_used": {"flask": 2, "tablet": 0, "spear": 0},
                "retreat_hp_threshold": 0.30,
                "avg_distance_from_boss": 2.9,
                "total_fight_duration_seconds": 42
            }
        }
    },
    {
        "_label": "Death 3 — Switched to heavy spam, range-hugging",
        "device_id": DEVICE_ID,
        "attempt_id": "attempt_003",
        "death_cause": "boss_lunge_at_range",
        "combat_log": {
            "events": [
                {"event": "player_dodge",  "direction": "back",  "attack_type": "",      "timestamp_ms": 800},
                {"event": "player_attack", "direction": "",      "attack_type": "heavy", "timestamp_ms": 1900},
                {"event": "player_dodge",  "direction": "back",  "attack_type": "",      "timestamp_ms": 3100},
                {"event": "player_attack", "direction": "",      "attack_type": "heavy", "timestamp_ms": 4200},
                {"event": "boss_attack",   "direction": "lunge", "attack_type": "heavy", "timestamp_ms": 7500},
                {"event": "player_dodge",  "direction": "right", "attack_type": "",      "timestamp_ms": 7520},
                {"event": "boss_attack",   "direction": "lunge", "attack_type": "heavy", "timestamp_ms": 9000},
            ],
            "stats": {
                "dodge_right_count":  3,
                "dodge_left_count":   4,
                "dodge_forward_count": 0,
                "dodge_back_count":   8,
                "light_attack_count": 2,
                "heavy_attack_count": 10,
                "light_attack_ratio": 0.17,
                "heavy_attack_ratio": 0.83,
                "throwables_used": {"flask": 1, "tablet": 1, "spear": 0},
                "retreat_hp_threshold": 0.20,
                "avg_distance_from_boss": 7.8,
                "total_fight_duration_seconds": 55
            }
        }
    },
    {
        "_label": "Death 4 — Throwable-spammer, moderate distance",
        "device_id": DEVICE_ID,
        "attempt_id": "attempt_004",
        "death_cause": "boss_counter_after_spear",
        "combat_log": {
            "events": [
                {"event": "player_attack", "direction": "",       "attack_type": "light",  "timestamp_ms": 600},
                {"event": "player_attack", "direction": "",       "attack_type": "light",  "timestamp_ms": 1300},
                {"event": "boss_attack",   "direction": "sweep",  "attack_type": "heavy",  "timestamp_ms": 2000},
                {"event": "player_dodge",  "direction": "right",  "attack_type": "",       "timestamp_ms": 2050},
                {"event": "player_attack", "direction": "",       "attack_type": "light",  "timestamp_ms": 3100},
                {"event": "player_attack", "direction": "",       "attack_type": "light",  "timestamp_ms": 4000},
                {"event": "boss_attack",   "direction": "counter","attack_type": "heavy",  "timestamp_ms": 5500},
            ],
            "stats": {
                "dodge_right_count":  5,
                "dodge_left_count":   3,
                "dodge_forward_count": 1,
                "dodge_back_count":   1,
                "light_attack_count": 14,
                "heavy_attack_count":  1,
                "light_attack_ratio": 0.93,
                "heavy_attack_ratio": 0.07,
                "throwables_used": {"flask": 3, "tablet": 2, "spear": 4},
                "retreat_hp_threshold": 0.40,
                "avg_distance_from_boss": 5.1,
                "total_fight_duration_seconds": 70
            }
        }
    },
    {
        "_label": "Death 5 — Long fight (respect trigger), finally tried parrying",
        "device_id": DEVICE_ID,
        "attempt_id": "attempt_005",
        "death_cause": "boss_grab_attack",
        "combat_log": {
            "events": [
                {"event": "player_attack", "direction": "",      "attack_type": "light", "timestamp_ms": 800},
                {"event": "player_dodge",  "direction": "right", "attack_type": "",      "timestamp_ms": 2000},
                {"event": "player_attack", "direction": "",      "attack_type": "heavy", "timestamp_ms": 3200},
                {"event": "boss_attack",   "direction": "grab",  "attack_type": "heavy", "timestamp_ms": 6000},
                {"event": "player_dodge",  "direction": "left",  "attack_type": "",      "timestamp_ms": 6100},
                {"event": "player_attack", "direction": "",      "attack_type": "heavy", "timestamp_ms": 7400},
                {"event": "player_dodge",  "direction": "right", "attack_type": "",      "timestamp_ms": 9200},
                {"event": "player_attack", "direction": "",      "attack_type": "light", "timestamp_ms": 10100},
                {"event": "boss_attack",   "direction": "grab",  "attack_type": "heavy", "timestamp_ms": 14300},
            ],
            "stats": {
                "dodge_right_count": 12,
                "dodge_left_count":   5,
                "dodge_forward_count": 3,
                "dodge_back_count":   2,
                "light_attack_count":  8,
                "heavy_attack_count":  7,
                "light_attack_ratio": 0.53,
                "heavy_attack_ratio": 0.47,
                "throwables_used": {"flask": 1, "tablet": 0, "spear": 1},
                "retreat_hp_threshold": 0.15,
                "avg_distance_from_boss": 4.0,
                "total_fight_duration_seconds": 112   # > 90s triggers respect
            }
        }
    },
]


# ─────────────────────────────────────────────────────────────────────
# RUNNER
# ─────────────────────────────────────────────────────────────────────
def _box(title: str) -> str:
    bar = "─" * (len(title) + 4)
    return f"\n┌{bar}┐\n│  {title}  │\n└{bar}┘"


async def reset_state(client: httpx.AsyncClient) -> None:
    resp = await client.delete(f"{BASE_URL}/nemesis-state/{DEVICE_ID}")
    print(f"[RESET] {resp.json()}")


async def run_death(client: httpx.AsyncClient, death: dict, index: int) -> None:
    label = death.pop("_label")
    print(_box(f"#{index + 1} — {label}"))

    try:
        resp = await client.post(
            f"{BASE_URL}/analyze-death",
            json=death,
            timeout=30.0,
        )
        resp.raise_for_status()
        result = resp.json()

        print(f'\n  🗡  TAUNT    : "{result["taunt"]}"')
        print(f'  💀  Deaths  : {result["total_deaths"]}')
        print(f'  ⚔  Adapts  : {result["adaptations_applied"]}')
        print(f'  🛡  Mercy   : {result["mercy_applied"]}')
        print(f'  📋 Trace   : {result["reasoning_trace"][:120]}...')
    except httpx.HTTPStatusError as e:
        print(f"  ❌ HTTP {e.response.status_code}: {e.response.text}")
    except httpx.ConnectError:
        print("  ❌ Cannot connect. Is the backend running? (uvicorn main:app --reload)")
        sys.exit(1)
    except Exception as e:
        print(f"  ❌ Error: {e}")


async def main() -> None:
    async with httpx.AsyncClient() as client:

        # Health check
        try:
            health = await client.get(f"{BASE_URL}/health", timeout=5.0)
            print(f"[HEALTH] {health.json()}")
        except httpx.ConnectError:
            print("❌  Backend is not running.")
            print("    Start it with:  uvicorn main:app --reload")
            sys.exit(1)

        # Optional reset flag
        if "--reset" in sys.argv:
            await reset_state(client)
            return

        print(f"\n[SESSION] device_id = {DEVICE_ID}  |  {len(DEATHS)} simulated deaths\n")

        for i, death in enumerate(DEATHS):
            await run_death(client, death, i)
            # Brief pause so logs are readable and rate limits are respected
            await asyncio.sleep(1.5)

        print("\n[DONE] Session complete.\n")

        # Print final nemesis state
        state_resp = await client.get(f"{BASE_URL}/nemesis-state/{DEVICE_ID}")
        state = state_resp.json()
        print("Final Nemesis State:")
        print(json.dumps(state, indent=2))


if __name__ == "__main__":
    asyncio.run(main())
