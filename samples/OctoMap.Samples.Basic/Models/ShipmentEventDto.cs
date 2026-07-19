namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a shipment event destination model declared through IMapTo on the source model.
    /// </summary>
    public sealed class ShipmentEventDto
    {
        /// <summary>
        /// Gets or sets the shipment event code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the shipment event location.
        /// </summary>
        public string Location { get; set; }
    }
}
