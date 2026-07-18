using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class ReverseMappingTests
    {
        [Fact]
        public void ReverseMap_Creates_Convention_Map_In_Both_Directions()
        {
            var provider = CreateProvider<ReverseConventionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ReverseCustomerDto, ReverseCustomer>(new ReverseCustomerDto
            {
                Id = 5,
                Name = "Ada"
            });

            Assert.Equal(5, destination.Id);
            Assert.Equal("Ada", destination.Name);
        }

        [Fact]
        public void ReverseMap_Reverses_Direct_MapFrom_Member()
        {
            var provider = CreateProvider<ReverseMapFromProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ReversePersonDto, ReversePerson>(new ReversePersonDto
            {
                DisplayName = "Grace"
            });

            Assert.Equal("Grace", destination.Name);
        }

        [Fact]
        public void ReverseMap_Allows_Additional_Reverse_Configuration()
        {
            var provider = CreateProvider<ReverseConfigurationProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ReverseOrderDto, ReverseOrder>(new ReverseOrderDto
            {
                Id = 9,
                Label = "Ready"
            });

            Assert.Equal(9, destination.Id);
            Assert.Equal("READY", destination.Status);
        }

        [Fact]
        public void ReverseMap_Reverses_Explicit_ForPath_Unflattening()
        {
            var provider = CreateProvider<ReverseForPathProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ReversePathOrder, ReversePathOrderDto>(new ReversePathOrder
            {
                Customer = new ReversePathCustomer
                {
                    FirstName = "Katherine"
                }
            });

            Assert.Equal("Katherine", destination.CustomerFirstName);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class ReverseConventionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ReverseCustomer, ReverseCustomerDto>()
                    .ReverseMap();
            }
        }

        public sealed class ReverseMapFromProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ReversePerson, ReversePersonDto>()
                    .ForMember(x => x.DisplayName, x => x.MapFrom(s => s.Name))
                    .ReverseMap();
            }
        }

        public sealed class ReverseConfigurationProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ReverseOrder, ReverseOrderDto>()
                    .ReverseMap()
                    .ForMember(x => x.Status, x => x.MapFrom(s => s.Label.ToUpperInvariant()));
            }
        }

        public sealed class ReverseForPathProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ReversePathOrderDto, ReversePathOrder>()
                    .ForPath(x => x.Customer.FirstName, x => x.MapFrom(s => s.CustomerFirstName))
                    .ReverseMap();
            }
        }

        public sealed class ReverseCustomer
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public sealed class ReverseCustomerDto
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public sealed class ReversePerson
        {
            public string Name { get; set; }
        }

        public sealed class ReversePersonDto
        {
            public string DisplayName { get; set; }
        }

        public sealed class ReverseOrder
        {
            public int Id { get; set; }

            public string Status { get; set; }
        }

        public sealed class ReverseOrderDto
        {
            public int Id { get; set; }

            public string Label { get; set; }
        }

        public sealed class ReversePathOrder
        {
            public ReversePathCustomer Customer { get; set; }
        }

        public sealed class ReversePathCustomer
        {
            public string FirstName { get; set; }
        }

        public sealed class ReversePathOrderDto
        {
            public string CustomerFirstName { get; set; }
        }
    }
}
