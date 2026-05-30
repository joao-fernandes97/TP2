// ExplorerGenerator.cs
// Assets/Scripts/Explorers/ExplorerGenerator.cs

using UnityEngine;

public static class ExplorerGenerator
{
    private static readonly string[] FirstNames =
    {
        "Thomas", "Minho", "Newt", "Frypan", "Ben",
        "Alby", "Teresa", "Sonya", "Harriet", "Aris",
        "Zart", "Clint", "Jeff", "Winston", "Chuck"
    };

    private static readonly string[] LastNames =
    {
        "Kane", "Song", "Newton", "Braxton", "Cooper",
        "Ashby", "Walker", "Rhodes", "Flynn", "Paige"
    };

    public static Explorer Generate()
    {
        var e = new Explorer
        {
            Name      = $"{FirstNames[Random.Range(0, FirstNames.Length)]} " +
                        $"{LastNames [Random.Range(0, LastNames.Length)]}",
            Endurance = Random.Range(3, 10),
            Strength  = Random.Range(3, 10),
            Speed     = Random.Range(3, 10),
            Luck      = Random.Range(3, 10),
        };
        e.Stamina = e.MaxStamina;
        return e;
    }
}