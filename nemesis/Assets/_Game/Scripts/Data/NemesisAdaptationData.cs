using System;
using UnityEngine;

[Serializable]
public class NemesisAdaptationData
{
    public float aggressionWeight = 1f;
    public float patternShiftWeight = 1f;
    public float punishCounterWeight = 1f;
    public float defenseWeight = 1f;
    public float adaptationConfidence = 0f;

    public static NemesisAdaptationData Default => new NemesisAdaptationData();
}
