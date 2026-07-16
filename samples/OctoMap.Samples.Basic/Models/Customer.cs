namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a sample customer source model.
    /// </summary>
    public sealed class Customer
    {
        /// <summary>
        /// Gets or sets the customer identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the customer first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the customer last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets an unmapped internal code.
        /// </summary>
        public string InternalCode { get; set; }
    }
}
