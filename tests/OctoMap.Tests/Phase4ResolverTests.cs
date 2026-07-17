using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class Phase4ResolverTests
    {
        [Fact]
        public void ResolveUsing_Resolves_Member_From_ServiceProvider()
        {
            ResolverConstructionCount = 0;
            var services = new ServiceCollection();
            services.AddTransient<OrderTotalTextResolver>();
            services.AddOctoMap(typeof(ResolverProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, OrderDto>(new Order { Total = 25.5m });

            Assert.Equal("Total: 25.50", destination.TotalText);
            Assert.Equal(1, ResolverConstructionCount);
        }

        [Fact]
        public void ResolveUsing_Resolves_Resolver_On_Each_Map_Call()
        {
            ResolverConstructionCount = 0;
            var services = new ServiceCollection();
            services.AddTransient<OrderTotalTextResolver>();
            services.AddOctoMap(typeof(ResolverProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            mapper.Map<Order, OrderDto>(new Order { Total = 10m });
            mapper.Map<Order, OrderDto>(new Order { Total = 20m });

            Assert.Equal(2, ResolverConstructionCount);
        }

        public static int ResolverConstructionCount { get; private set; }

        public sealed class ResolverProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<Order, OrderDto>()
                    .ForMember(x => x.TotalText, x => x.ResolveUsing<OrderTotalTextResolver>());
            }
        }

        public sealed class OrderTotalTextResolver : IValueResolver<Order, OrderDto, string>
        {
            public OrderTotalTextResolver()
            {
                ResolverConstructionCount++;
            }

            public string Resolve(Order source, OrderDto destination, IMapContext context)
                => $"Total: {source.Total:0.00}";
        }

        public sealed class Order
        {
            public decimal Total { get; set; }
        }

        public sealed class OrderDto
        {
            public string TotalText { get; set; }
        }
    }
}
