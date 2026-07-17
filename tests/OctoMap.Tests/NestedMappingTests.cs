using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class NestedMappingTests
    {
        [Fact]
        public void Map_Uses_Configured_Nested_Map_For_Matching_Member()
        {
            var provider = CreateProvider<ConfiguredNestedProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, OrderDto>(new Order
            {
                Id = 10,
                Customer = new Customer
                {
                    Id = 5,
                    FirstName = "Ada",
                    LastName = "Lovelace"
                }
            });

            Assert.Equal(10, destination.Id);
            Assert.NotNull(destination.Customer);
            Assert.Equal(5, destination.Customer.Id);
            Assert.Equal("Ada Lovelace", destination.Customer.FullName);
        }

        [Fact]
        public void Map_Creates_Implicit_Nested_Map_When_Not_Configured()
        {
            var provider = CreateProvider<ImplicitNestedProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, ImplicitOrderDto>(new Order
            {
                Id = 11,
                Customer = new Customer
                {
                    Id = 6,
                    FirstName = "Grace",
                    LastName = "Hopper"
                }
            });

            Assert.Equal(11, destination.Id);
            Assert.NotNull(destination.Customer);
            Assert.Equal(6, destination.Customer.Id);
            Assert.Equal("Grace", destination.Customer.FirstName);
            Assert.Equal("Hopper", destination.Customer.LastName);
        }

        [Fact]
        public void Map_Assigns_Default_When_Nested_Source_Is_Null()
        {
            var provider = CreateProvider<ConfiguredNestedProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, OrderDto>(new Order
            {
                Id = 12,
                Customer = null
            });

            Assert.Equal(12, destination.Id);
            Assert.Null(destination.Customer);
        }

        [Fact]
        public void Map_Throws_When_Nested_Map_Is_Not_Configured_And_Implicit_Maps_Are_Disabled()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(
                options => options.EnableRuntimeImplicitMaps = false,
                typeof(ImplicitNestedProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var exception = Assert.Throws<InvalidOperationException>(() => mapper.Map<Order, ImplicitOrderDto>(new Order
            {
                Customer = new Customer()
            }));

            Assert.Contains("runtime implicit maps are disabled", exception.Message);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class ConfiguredNestedProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<Order, OrderDto>();
                builder.CreateMap<Customer, CustomerDto>()
                    .ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName));
            }
        }

        public sealed class ImplicitNestedProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<Order, ImplicitOrderDto>();
            }
        }

        public sealed class Order
        {
            public int Id { get; set; }

            public Customer Customer { get; set; }
        }

        public sealed class OrderDto
        {
            public int Id { get; set; }

            public CustomerDto Customer { get; set; }
        }

        public sealed class ImplicitOrderDto
        {
            public int Id { get; set; }

            public ImplicitCustomerDto Customer { get; set; }
        }

        public sealed class Customer
        {
            public int Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        public sealed class CustomerDto
        {
            public int Id { get; set; }

            public string FullName { get; set; }
        }

        public sealed class ImplicitCustomerDto
        {
            public int Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }
    }
}
