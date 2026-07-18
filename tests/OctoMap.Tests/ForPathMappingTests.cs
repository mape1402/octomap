using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class ForPathMappingTests
    {
        [Fact]
        public void Map_Creates_Destination_Path_And_Assigns_Source_Value()
        {
            var provider = CreateProvider<ForPathProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<OrderDto, Order>(new OrderDto
            {
                Id = 10,
                CustomerName = "Ada Lovelace"
            });

            Assert.Equal(10, destination.Id);
            Assert.NotNull(destination.Customer);
            Assert.Equal("Ada Lovelace", destination.Customer.Name);
        }

        [Fact]
        public void Map_Creates_Multiple_Destination_Path_Levels()
        {
            var provider = CreateProvider<DeepForPathProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<OrderDto, Order>(new OrderDto
            {
                CustomerCity = "London"
            });

            Assert.NotNull(destination.Customer);
            Assert.NotNull(destination.Customer.Address);
            Assert.Equal("London", destination.Customer.Address.City);
        }

        [Fact]
        public void AssertValid_Fails_When_Destination_Path_Intermediate_Cannot_Be_Created()
        {
            var provider = CreateProvider<InvalidForPathProfile>();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var exception = Assert.Throws<OctoMapValidationException>(() => configuration.AssertValid());

            Assert.Contains("public parameterless constructor", exception.Message);
        }

        [Fact]
        public void MultiSource_Map_Creates_Destination_Path_From_Source_Contribution()
        {
            var provider = CreateProvider<MultiSourceForPathProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order>(SourceSet.Of(new OrderDto
            {
                CustomerName = "Katherine Johnson"
            }));

            Assert.NotNull(destination.Customer);
            Assert.Equal("Katherine Johnson", destination.Customer.Name);
        }

        [Fact]
        public void AssertValid_Fails_When_Destination_Path_Conflicts_With_Configured_Root_Member()
        {
            var provider = CreateProvider<ConflictingForPathProfile>();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var exception = Assert.Throws<OctoMapValidationException>(() => configuration.AssertValid());

            Assert.Contains("conflicts with configured destination member", exception.Message);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class ForPathProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<OrderDto, Order>()
                    .ForPath(x => x.Customer.Name, x => x.MapFrom(s => s.CustomerName));
            }
        }

        public sealed class DeepForPathProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<OrderDto, Order>()
                    .ForPath(x => x.Customer.Address.City, x => x.MapFrom(s => s.CustomerCity));
            }
        }

        public sealed class InvalidForPathProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<InvalidOrderDto, InvalidOrder>()
                    .ForPath(x => x.Customer.Name, x => x.MapFrom(s => s.CustomerName));
            }
        }

        public sealed class MultiSourceForPathProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMultiMap<Order>()
                    .From<OrderDto>(map => map
                        .ForPath(x => x.Customer.Name, x => x.MapFrom(s => s.CustomerName)));
            }
        }

        public sealed class ConflictingForPathProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ConflictOrderDto, ConflictOrder>()
                    .ForMember(x => x.Customer, x => x.UseValue(new ConflictCustomer()))
                    .ForPath(x => x.Customer.Name, x => x.MapFrom(s => s.CustomerName));
            }
        }

        public sealed class OrderDto
        {
            public int Id { get; set; }

            public string CustomerName { get; set; }

            public string CustomerCity { get; set; }
        }

        public sealed class Order
        {
            public int Id { get; set; }

            public Customer Customer { get; set; }
        }

        public sealed class Customer
        {
            public string Name { get; set; }

            public Address Address { get; set; }
        }

        public sealed class Address
        {
            public string City { get; set; }
        }

        public sealed class InvalidOrderDto
        {
            public string CustomerName { get; set; }
        }

        public sealed class InvalidOrder
        {
            public InvalidCustomer Customer { get; set; }
        }

        public sealed class InvalidCustomer
        {
            public InvalidCustomer(string name)
            {
                Name = name;
            }

            public string Name { get; set; }
        }

        public sealed class ConflictOrderDto
        {
            public string CustomerName { get; set; }
        }

        public sealed class ConflictOrder
        {
            public ConflictCustomer Customer { get; set; }
        }

        public sealed class ConflictCustomer
        {
            public string Name { get; set; }
        }
    }
}
