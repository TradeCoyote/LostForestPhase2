namespace LostForest.Phase2.Pursuer
{
    /// <summary>
    /// Player-facing states stay deliberately broad. Exact paths and distances
    /// remain developer-only information on the hidden Field.
    /// </summary>
    public enum PursuerState
    {
        Dormant,
        Interest,
        Search,
        Stalk,
        ClosePressure,
        Catch,
        Disabled
    }
}
