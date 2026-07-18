namespace OctoMap
{
    /// <summary>
    /// Uses the specified value when the resolved source member is null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class NullSubstituteAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullSubstituteAttribute"/> class.
        /// </summary>
        /// <param name="value">The null substitute value.</param>
        public NullSubstituteAttribute(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the null substitute value.
        /// </summary>
        public string Value { get; }
    }
}
