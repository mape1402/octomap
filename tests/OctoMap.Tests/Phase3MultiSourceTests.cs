using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class Phase3MultiSourceTests
    {
        [Fact]
        public void Explicit_Multi_Source_Map_Maps_Source_Local_And_Context_Members()
        {
            var provider = CreateProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<OrderSummaryDto>(SourceSet.Of(
                new OrderSummaryOrder
                {
                    Id = 42,
                    Code = "ORD-42",
                    Total = 125.50m
                },
                new OrderSummaryCustomer
                {
                    Name = "Ada Lovelace"
                }));

            Assert.Equal(42, destination.OrderId);
            Assert.Equal(125.50m, destination.Total);
            Assert.Equal("Ada Lovelace", destination.CustomerName);
            Assert.Equal("ORD-42 - Ada Lovelace", destination.Label);
        }

        [Fact]
        public void Multi_Source_Map_Must_Be_Configured_Explicitly()
        {
            var provider = CreateProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var exception = Assert.Throws<InvalidOperationException>(() => mapper.Map<UnconfiguredMultiSourceDto>(SourceSet.Of(
                new OrderSummaryOrder(),
                new OrderSummaryCustomer())));

            Assert.Contains("must be registered explicitly", exception.Message);
        }

        private static ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(MultiSourceProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class MultiSourceProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMultiMap<OrderSummaryDto>()
                    .From<OrderSummaryOrder>(map => map
                        .ForMember(x => x.OrderId, x => x.MapFrom(s => s.Id))
                        .ForMember(x => x.Total, x => x.MapFrom(s => s.Total)))
                    .From<OrderSummaryCustomer>(map => map
                        .ForMember(x => x.CustomerName, x => x.MapFrom(s => s.Name)))
                    .ForMember(x => x.Label, x => x.MapFrom(ctx =>
                        ctx.Get<OrderSummaryOrder>().Code + " - " + ctx.Get<OrderSummaryCustomer>().Name));
            }
        }

        public sealed class OrderSummaryOrder
        {
            public int Id { get; set; }

            public string Code { get; set; }

            public decimal Total { get; set; }
        }

        public sealed class OrderSummaryCustomer
        {
            public string Name { get; set; }
        }

        public sealed class OrderSummaryDto
        {
            public int OrderId { get; set; }

            public decimal Total { get; set; }

            public string CustomerName { get; set; }

            public string Label { get; set; }
        }

        public sealed class UnconfiguredMultiSourceDto
        {
            public int Id { get; set; }
        }
    }
}
