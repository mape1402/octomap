namespace OctoMap.Naming
{
    /// <summary>
    /// Normalizes member names so convention mapping can compare different naming styles.
    /// </summary>
    public interface INamingConvention
    {
        /// <summary>
        /// Normalizes a member name into a comparable key.
        /// </summary>
        /// <param name="name">The member name.</param>
        /// <returns>The normalized member name.</returns>
        string Normalize(string name);
    }
}
