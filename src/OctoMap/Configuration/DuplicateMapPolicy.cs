namespace OctoMap
{
    /// <summary>
    /// Defines how OctoMap handles duplicate explicit map declarations.
    /// </summary>
    public enum DuplicateMapPolicy
    {
        /// <summary>
        /// Merges duplicate map declarations into the same type map.
        /// </summary>
        Merge,

        /// <summary>
        /// Throws when a duplicate map declaration is found.
        /// </summary>
        Throw,

        /// <summary>
        /// Replaces the previous type map with the latest declaration.
        /// </summary>
        Replace
    }
}
