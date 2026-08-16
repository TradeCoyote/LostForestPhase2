namespace LostForest.Phase2.Pursuer
{
    /// <summary>
    /// Identifies the fiction and ruleset behind a pursuer. The Hunter is the
    /// first implementation; future pursuers can use the same hidden-field and
    /// feedback contracts without being treated as palette swaps.
    /// </summary>
    public enum PursuerArchetype
    {
        TheHunter,
        PackOfWolves,
        Banshee
    }
}
