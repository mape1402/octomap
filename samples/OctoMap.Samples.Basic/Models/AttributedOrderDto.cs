namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents an order destination model configured through mapping attributes.
    /// </summary>
    [MapFrom(typeof(AttributedOrder))]
    public sealed class AttributedOrderDto
    {
        /// <summary>
        /// Gets or sets the status mapped from a differently named source member.
        /// </summary>
        [MapName("StatusCode")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the description with an attribute-defined null substitute.
        /// </summary>
        [NullSubstitute("No attributed description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets an ignored internal code.
        /// </summary>
        [IgnoreMap]
        public string InternalCode { get; set; }
    }
}
