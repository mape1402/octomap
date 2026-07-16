namespace OctoMap
{
    /// <summary>
    /// Represents an OctoMap configuration validation failure.
    /// </summary>
    public sealed class OctoMapValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapValidationException"/> class.
        /// </summary>
        /// <param name="report">The validation report.</param>
        public OctoMapValidationException(OctoMapValidationReport report)
            : base(BuildMessage(report))
        {
            Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        /// <summary>
        /// Gets the validation report.
        /// </summary>
        public OctoMapValidationReport Report { get; }

        private static string BuildMessage(OctoMapValidationReport report)
            => report == null || report.Issues.Count == 0
                ? "OctoMap configuration is invalid."
                : "OctoMap configuration is invalid: " + string.Join("; ", report.Issues.Select(x => x.Message));
    }
}
