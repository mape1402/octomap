using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class ExistingDestinationMappingTests
    {
        [Fact]
        public void Map_Updates_Existing_Destination_Instance()
        {
            var provider = CreateProvider<ExistingDestinationProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var destination = new ExistingOrderDto
            {
                Preserved = "keep"
            };

            var result = mapper.Map(new ExistingOrder
            {
                Id = 10,
                Description = "updated"
            }, destination);

            Assert.Same(destination, result);
            Assert.Equal(10, result.Id);
            Assert.Equal("updated", result.Description);
            Assert.Equal("keep", result.Preserved);
        }

        [Fact]
        public void Map_Returns_Existing_Destination_When_Source_Is_Null()
        {
            var provider = CreateProvider<ExistingDestinationProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var destination = new ExistingOrderDto
            {
                Id = 20,
                Description = "existing"
            };

            var result = mapper.Map<ExistingOrder, ExistingOrderDto>(null, destination);

            Assert.Same(destination, result);
            Assert.Equal(20, result.Id);
            Assert.Equal("existing", result.Description);
        }

        [Fact]
        public void Map_Respects_Conditions_When_Updating_Existing_Destination()
        {
            var provider = CreateProvider<ConditionalExistingDestinationProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var destination = new ExistingOrderDto
            {
                Description = "keep"
            };

            var result = mapper.Map(new ExistingOrder
            {
                Status = "Cancelled",
                Description = "skip"
            }, destination);

            Assert.Same(destination, result);
            Assert.Equal("keep", result.Description);
        }

        [Fact]
        public void Map_Updates_ForPath_On_Existing_Destination()
        {
            var provider = CreateProvider<ExistingDestinationPathProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var customer = new ExistingCustomer
            {
                Name = "old"
            };
            var destination = new ExistingOrderEntity
            {
                Customer = customer
            };

            var result = mapper.Map(new ExistingOrderDto
            {
                CustomerName = "new"
            }, destination);

            Assert.Same(destination, result);
            Assert.Same(customer, result.Customer);
            Assert.Equal("new", result.Customer.Name);
        }

        [Fact]
        public void Map_Creates_ForPath_Intermediate_When_Missing_On_Existing_Destination()
        {
            var provider = CreateProvider<ExistingDestinationPathProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var destination = new ExistingOrderEntity();

            var result = mapper.Map(new ExistingOrderDto
            {
                CustomerName = "created"
            }, destination);

            Assert.Same(destination, result);
            Assert.NotNull(result.Customer);
            Assert.Equal("created", result.Customer.Name);
        }

        [Fact]
        public void Map_Updates_Existing_Nested_Destination_When_Present()
        {
            var provider = CreateProvider<ExistingNestedDestinationProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var customer = new ExistingCustomerDto
            {
                Code = "preserve"
            };
            var destination = new ExistingOrderWithCustomerDto
            {
                Customer = customer
            };

            var result = mapper.Map(new ExistingOrderWithCustomer
            {
                Customer = new ExistingCustomerSource
                {
                    Name = "Ada"
                }
            }, destination);

            Assert.Same(destination, result);
            Assert.Same(customer, result.Customer);
            Assert.Equal("Ada", result.Customer.Name);
            Assert.Equal("preserve", result.Customer.Code);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class ExistingDestinationProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ExistingOrder, ExistingOrderDto>();
            }
        }

        public sealed class ConditionalExistingDestinationProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ExistingOrder, ExistingOrderDto>()
                    .ForMember(x => x.Description, x =>
                    {
                        x.MapFrom(s => s.Description);
                        x.PreCondition(s => s.Status != "Cancelled");
                    });
            }
        }

        public sealed class ExistingDestinationPathProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ExistingOrderDto, ExistingOrderEntity>()
                    .ForPath(x => x.Customer.Name, x => x.MapFrom(s => s.CustomerName));
            }
        }

        public sealed class ExistingNestedDestinationProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ExistingOrderWithCustomer, ExistingOrderWithCustomerDto>();
                builder.CreateMap<ExistingCustomerSource, ExistingCustomerDto>();
            }
        }

        public sealed class ExistingOrder
        {
            public int Id { get; set; }

            public string Status { get; set; }

            public string Description { get; set; }
        }

        public sealed class ExistingOrderDto
        {
            public int Id { get; set; }

            public string Description { get; set; }

            public string Preserved { get; set; }

            public string CustomerName { get; set; }
        }

        public sealed class ExistingOrderEntity
        {
            public ExistingCustomer Customer { get; set; }
        }

        public sealed class ExistingCustomer
        {
            public string Name { get; set; }
        }

        public sealed class ExistingOrderWithCustomer
        {
            public ExistingCustomerSource Customer { get; set; }
        }

        public sealed class ExistingOrderWithCustomerDto
        {
            public ExistingCustomerDto Customer { get; set; }
        }

        public sealed class ExistingCustomerSource
        {
            public string Name { get; set; }
        }

        public sealed class ExistingCustomerDto
        {
            public string Name { get; set; }

            public string Code { get; set; }
        }
    }
}
