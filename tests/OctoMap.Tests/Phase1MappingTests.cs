using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class Phase1MappingTests
    {
        [Fact]
        public void Map_Uses_Profile_Configured_Map()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(SalesProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var source = new Customer
            {
                Id = 7,
                Name = "Ada",
                Ignored = "source-only"
            };

            var destination = mapper.Map<Customer, CustomerDto>(source);

            Assert.Equal(7, destination.Id);
            Assert.Equal("Ada", destination.Name);
        }

        [Fact]
        public void Map_Creates_Implicit_Runtime_Map_When_Not_Configured()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(options => options.EnableRuntimeImplicitMaps = true);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ProductDto>(new Product
            {
                Sku = "ABC",
                Price = 12
            });

            Assert.Equal("ABC", destination.Sku);
            Assert.Equal(12, destination.Price);
        }

        [Fact]
        public void Map_Throws_When_Implicit_Runtime_Maps_Are_Disabled()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(options => options.EnableRuntimeImplicitMaps = false);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            Assert.Throws<InvalidOperationException>(() => mapper.Map<ProductDto>(new Product()));
        }

        public sealed class SalesProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<Customer, CustomerDto>();
            }
        }

        public sealed class Customer
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string Ignored { get; set; }
        }

        public sealed class CustomerDto
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public sealed class Product
        {
            public string Sku { get; set; }

            public decimal Price { get; set; }
        }

        public sealed class ProductDto
        {
            public string Sku { get; set; }

            public decimal Price { get; set; }
        }
    }
}
