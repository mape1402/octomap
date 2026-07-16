namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a warehouse item destination model declared through IMapFrom.
    /// </summary>
    public sealed class WarehouseItemDto : IMapFrom<WarehouseItem>
    {
        /// <summary>
        /// Gets or sets the warehouse item code.
        /// </summary>
        public string Code { get; set; }
    }
}
