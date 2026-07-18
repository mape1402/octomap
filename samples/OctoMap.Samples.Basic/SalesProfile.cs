using OctoMap.Samples.Basic.Models;

namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Defines sample maps for the sales domain.
    /// </summary>
    public sealed class SalesProfile : OctoMapProfile
    {
        /// <inheritdoc/>
        public override void Configure(IOctoMapConfigurationBuilder builder)
        {
            builder.CreateMap<Customer, CustomerDto>()
                .ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName))
                .ForMember(x => x.InternalCode, x => x.Ignore());

            builder.CreateMap<OrderItem, OrderItemDto>()
                .ForMember(x => x.Label, x => x.MapFrom(s => s.Sku + " x " + s.Quantity));

            builder.CreateMap<OrderLine, OrderLineDto>()
                .ForMember(x => x.DisplayName, x => x.MapFrom(s => (s.DisplayName ?? s.Sku ?? "Unknown").Trim().ToUpperInvariant()))
                .ForMember(x => x.Total, x => x.MapFrom(s => decimal.Round((s.UnitPrice * s.Quantity) - s.Discount, 2)))
                .ForMember(x => x.QuantityRemainder, x => x.MapFrom(s => s.Quantity % 2))
                .ForMember(x => x.CanShip, x => x.MapFrom(s => s.IsActive && s.Quantity > 0 && !s.IsDeleted));

            builder.CreateMap<Order, OrderDto>()
                .ForMember(x => x.Status, x => x.MapFrom(s => s.StatusCode))
                .ForMember(x => x.Description, x => x.NullSubstitute("No description"))
                .ForMember(x => x.StatusLabel, x => x.ResolveUsing<OrderStatusLabelResolver>())
                .ForMember(x => x.TotalText, x => x.ConvertUsing<OrderTotalTextConverter>(s => s.Total));

            builder.CreateMultiMap<OrderSummaryDto>()
                .From<Order>(map => map
                    .ForMember(x => x.OrderId, x => x.MapFrom(s => s.Id))
                    .ForMember(x => x.Description, x => x.MapFrom(s => s.Description)))
                .From<Customer>(map => map
                    .ForMember(x => x.CustomerName, x => x.MapFrom(s => s.FirstName)))
                .ForMember(x => x.Label, x => x.MapFrom(ctx =>
                    ctx.Get<Order>().Description + " - " + ctx.Get<Customer>().FirstName));
        }
    }
}
