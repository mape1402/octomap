namespace OctoMap.Analyzers
{
    /// <summary>
    /// Defines diagnostic identifiers emitted by OctoMap analyzers.
    /// </summary>
    internal static class OctoMapDiagnosticIds
    {
        /// <summary>
        /// Indicates that a map attribute points to the declaring type.
        /// </summary>
        public const string SelfReferencingMapAttribute = "OCMAP001";

        /// <summary>
        /// Indicates that a mapping rule uses a runtime-only feature.
        /// </summary>
        public const string RuntimeOnlyProjectionFeature = "OCMAP002";
    }
}
