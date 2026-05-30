// ExplorerStatus.cs
// Assets/Scripts/Explorers/ExplorerStatus.cs

public enum ExplorerStatus
{
    InCamp,     // Available to send out
    Exploring,  // Currently in the maze
    Returning,  // Recall issued walking the known path back to camp
    Lost,       // Didn't make it back overnight resolution pending
    Dead        // Confirmed dead, removed from active roster
}