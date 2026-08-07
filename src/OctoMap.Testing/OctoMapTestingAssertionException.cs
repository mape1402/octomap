namespace OctoMap.Testing
{
    /// <summary>
    /// Represents an assertion failure produced by OctoMap testing helpers.
    /// </summary>
    public sealed class OctoMapTestingAssertionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapTestingAssertionException"/> class.
        /// </summary>
        /// <param name="message">The assertion failure message.</param>
        public OctoMapTestingAssertionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapTestingAssertionException"/> class.
        /// </summary>
        /// <param name="message">The assertion failure message.</param>
        /// <param name="innerException">The exception that caused this assertion failure.</param>
        public OctoMapTestingAssertionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
