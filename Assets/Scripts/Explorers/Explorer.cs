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

    /// <summary>
    /// Applied when an explorer is rescued after being Lost.
    /// Each stat drops by 1, floored at 1 so they remain usable.
    /// </summary>
    public void ApplyRescuePenalty()
    {
        Endurance = Mathf.Max(1, Endurance - 1);
        Strength  = Mathf.Max(1, Strength  - 1);
        Speed     = Mathf.Max(1, Speed     - 1);
        Luck      = Mathf.Max(1, Luck      - 1);
        // Reset stamina to the new (lower) maximum
        Stamina   = MaxStamina;
        CarriedIntel = 0;
        Debug.Log($"[Rescue] {Name} penalty applied - END:{Endurance} STR:{Strength} SPD:{Speed} LCK:{Luck}");
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