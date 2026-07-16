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
                .ForMember(x => x.Status, x => x.UseValue("Created"))
                .ForMember(x => x.Description, x => x.NullSubstitute("No description"));
        }
    }
}
