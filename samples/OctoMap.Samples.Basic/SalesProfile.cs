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
