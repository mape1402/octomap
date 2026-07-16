namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a sample customer destination model.
    /// </summary>
    public sealed class CustomerDto
    {
        /// <summary>
        /// Gets or sets the customer identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the customer full name.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the internal code.
        /// </summary>
        public string InternalCode { get; set; }
    }
}
