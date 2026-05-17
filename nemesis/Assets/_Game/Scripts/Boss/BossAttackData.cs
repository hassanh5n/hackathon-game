using System;
using UnityEngine;

[Serializable]
public class BossAttackData
{
    public string attackId;
    public float baseDamage;
    public float postureDamage;
    public float windupTime;
    public float cooldown;
    public float weight;
}
