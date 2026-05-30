// Explorer.cs
// Assets/Scripts/Explorers/Explorer.cs

using UnityEngine;

[System.Serializable]
public class Explorer
{
    // Identity
    public string Name;

    // Core stats (1–10 scale, set at generation)
    public int Endurance;   // → max Stamina
    public int Strength;    // → combat outcomes
    public int Speed;       // → exploration pace, event frequency
    public int Luck;        // → random chance modifier

    // Derived / runtime state
    public int  MaxStamina    => Endurance * 10;   // e.g. 10 Endurance = 100 stamina
    public int  Stamina       { get; set; }
    public int  CarriedIntel  { get; set; } = 0;   // intel on-person, lost if they die
    public ExplorerStatus Status { get; set; } = ExplorerStatus.InCamp;

    // Called at the start of each new day
    public void ResetForNewDay()
    {
        Stamina      = MaxStamina;
        CarriedIntel = 0;
        // Status stays InCamp — DayManager will set to Exploring when sent out
    }

    // Stat check helper — used by EventSystem
    // Returns true if the check succeeds
    public bool RollStatCheck(int statValue, float bonusChance = 0f)
    {
        float chance = (statValue / 10f) + bonusChance;
        return Random.value < Mathf.Clamp01(chance);
    }

    public override string ToString() => $"{Name} [{Status}] STM:{Stamina}/{MaxStamina} INTEL:{CarriedIntel}";
}