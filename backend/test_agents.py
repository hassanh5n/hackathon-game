import asyncio
from models.combat_log import CombatLog, CombatStats
from agents.death_analyst import analyze_death
from agents.taunt_generator import generate_taunt

async def main():
    log = CombatLog(stats=CombatStats(
        dodge_left_count=14,
        dodge_right_count=2,
        light_attack_ratio=0.9,
    ))
    
    print("Testing death_analyst...")
    analysis = await analyze_death(log, "boss_sweep_left")
    print(analysis)
    
    print("\nTesting taunt_generator...")
    patterns = analysis["patterns"]
    taunt = await generate_taunt(patterns, 1, "methodical")
    print(taunt)

if __name__ == "__main__":
    asyncio.run(main())
