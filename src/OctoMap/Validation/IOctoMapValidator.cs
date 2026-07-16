namespace OctoMap.Validation
{
    /// <summary>
    /// Validates OctoMap configuration.
    /// </summary>
    public interface IOctoMapValidator
    {
        /// <summary>
        /// Validates the specified maps.
        /// </summary>
        /// <param name="maps">The maps to validate.</param>
        /// <returns>The validation report.</returns>
        OctoMapValidationReport Validate(IReadOnlyCollection<ITypeMap> maps);
    }
}
