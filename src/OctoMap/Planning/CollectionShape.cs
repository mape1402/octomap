namespace OctoMap.Planning
{
    /// <summary>
    /// Represents a supported collection shape in a mapping plan.
    /// </summary>
    public enum CollectionShape
    {
        /// <summary>
        /// Indicates that the member is not a supported collection.
        /// </summary>
        None,

        /// <summary>
        /// Indicates a one-dimensional array.
        /// </summary>
        Array,

        /// <summary>
        /// Indicates a closed generic <see cref="List{T}"/>.
        /// </summary>
        List,

        /// <summary>
        /// Indicates a closed generic <see cref="HashSet{T}"/>.
        /// </summary>
        Set,

        /// <summary>
        /// Indicates a generic enumerable that must be normalized before indexed iteration.
        /// </summary>
        Enumerable
    }
}
