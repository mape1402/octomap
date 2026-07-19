namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a shipment event source model declared through IMapTo.
    /// </summary>
    public sealed class ShipmentEvent : IMapTo<ShipmentEventDto>
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
