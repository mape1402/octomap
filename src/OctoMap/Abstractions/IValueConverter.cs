namespace OctoMap
{
    /// <summary>
    /// Converts one source member value into a destination member value during a mapping operation.
    /// </summary>
    /// <typeparam name="TSourceMember">The source member type.</typeparam>
    /// <typeparam name="TDestinationMember">The destination member type.</typeparam>
    public interface IValueConverter<TSourceMember, TDestinationMember>
    {
        /// <summary>
        /// Converts the specified source member value into a destination member value.
        /// </summary>
        /// <param name="sourceMember">The source member value.</param>
        /// <param name="context">The current map context.</param>
        /// <returns>The converted destination member value.</returns>
        TDestinationMember Convert(TSourceMember sourceMember, IMapContext context);
    }
}
