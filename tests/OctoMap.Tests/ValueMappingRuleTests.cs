using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class ValueMappingRuleTests
    {
        [Fact]
        public void UseValue_Assigns_Configured_Constant_Value()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(ValueRuleProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, OrderDto>(new Order { Id = 9 });

            Assert.Equal(9, destination.Id);
            Assert.Equal("Created", destination.Status);
        }

        [Fact]
        public void NullSubstitute_Replaces_Null_Source_Value()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(ValueRuleProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, OrderDto>(new Order { Id = 10, Description = null });

            Assert.Equal("No description", destination.Description);
        }

        public sealed class ValueRuleProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<Order, OrderDto>()
                    .ForMember(x => x.Status, x => x.UseValue("Created"))
                    .ForMember(x => x.Description, x => x.NullSubstitute("No description"));
            }
        }

        public sealed class Order
        {
            public int Id { get; set; }

            public string Description { get; set; }
        }

        public sealed class OrderDto
        {
            public int Id { get; set; }

            public string Status { get; set; }

            public string Description { get; set; }
        }
    }
}
