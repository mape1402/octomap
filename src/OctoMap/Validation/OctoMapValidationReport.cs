namespace OctoMap
{
    /// <summary>
    /// Represents the result of an OctoMap validation operation.
    /// </summary>
    public sealed class OctoMapValidationReport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapValidationReport"/> class.
        /// </summary>
        /// <param name="issues">The validation issues.</param>
        public OctoMapValidationReport(IReadOnlyList<OctoMapValidationIssue> issues)
        {
            Issues = issues ?? throw new ArgumentNullException(nameof(issues));
        }

        /// <summary>
        /// Gets the validation issues.
        /// </summary>
        public IReadOnlyList<OctoMapValidationIssue> Issues { get; }

        /// <summary>
        /// Gets whether the report has no validation issues.
        /// </summary>
        public bool IsValid => Issues.Count == 0;
    }
}
