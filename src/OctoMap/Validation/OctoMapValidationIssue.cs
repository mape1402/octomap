namespace OctoMap
{
    /// <summary>
    /// Represents one OctoMap validation issue.
    /// </summary>
    public sealed class OctoMapValidationIssue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapValidationIssue"/> class.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="memberName">The destination member name.</param>
        /// <param name="message">The validation message.</param>
        public OctoMapValidationIssue(Type sourceType, Type destinationType, string memberName, string message)
        {
            SourceType = sourceType;
            DestinationType = destinationType;
            MemberName = memberName;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>
        /// Gets the source type.
        /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        public Type DestinationType { get; }

        /// <summary>
        /// Gets the destination member name.
        /// </summary>
        public string MemberName { get; }

        /// <summary>
        /// Gets the validation message.
        /// </summary>
        public string Message { get; }
    }
}
