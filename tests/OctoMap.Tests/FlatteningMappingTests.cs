using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class FlatteningMappingTests
    {
        [Fact]
        public void Map_Flattens_Nested_Source_Members_By_Convention()
        {
            var provider = CreateProvider<FlatteningProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<FlattenedOrder, FlattenedOrderDto>(new FlattenedOrder
            {
                Id = 10,
                Customer = new FlattenedCustomer
                {
                    Name = "Ada",
                    Address = new FlattenedAddress
                    {
                        City = "London"
                    }
                }
            });

            Assert.Equal(10, destination.Id);
            Assert.Equal("Ada", destination.CustomerName);
            Assert.Equal("London", destination.CustomerAddressCity);
        }

        [Fact]
        public void Map_Assigns_Default_When_Flattened_Intermediate_Source_Is_Null()
        {
            var provider = CreateProvider<FlatteningProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<FlattenedOrder, FlattenedOrderDto>(new FlattenedOrder
            {
                Id = 11,
                Customer = null
            });

            Assert.Equal(11, destination.Id);
            Assert.Null(destination.CustomerName);
            Assert.Null(destination.CustomerAddressCity);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class FlatteningProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<FlattenedOrder, FlattenedOrderDto>();
            }
        }

        public sealed class FlattenedOrder
        {
            public int Id { get; set; }

            public FlattenedCustomer Customer { get; set; }
        }

        public sealed class FlattenedCustomer
        {
            public string Name { get; set; }

            public FlattenedAddress Address { get; set; }
        }

        public sealed class FlattenedAddress
        {
            public string City { get; set; }
        }

        public sealed class FlattenedOrderDto
        {
            public int Id { get; set; }

            public string CustomerName { get; set; }

            public string CustomerAddressCity { get; set; }
        }
    }
}
