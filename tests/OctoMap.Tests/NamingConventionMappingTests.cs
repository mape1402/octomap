using Microsoft.Extensions.DependencyInjection;
using OctoMap.Naming;

namespace OctoMap.Tests
{
    public class NamingConventionMappingTests
    {
        [Fact]
        public void Map_Uses_Configured_Naming_Conventions_From_Options()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(options =>
            {
                options.SourceNamingConvention = SnakeCaseNamingConvention.Instance;
                options.DestinationNamingConvention = PascalCaseNamingConvention.Instance;
            }, typeof(NamingConventionOptionsProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<SnakeOrder, PascalOrderDto>(new SnakeOrder
            {
                customer_name = "Ada",
                order_total = 12.34m
            });

            Assert.Equal("Ada", destination.CustomerName);
            Assert.Equal(12.34m, destination.OrderTotal);
        }

        [Fact]
        public void Map_Uses_Configured_Naming_Conventions_From_Profile()
        {
            var provider = CreateProvider<NamingConventionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ProfileSnakeOrder, ProfilePascalOrderDto>(new ProfileSnakeOrder
            {
                customer_name = "Grace"
            });

            Assert.Equal("Grace", destination.CustomerName);
        }

        [Fact]
        public void Map_Removes_Configured_Prefixes_And_Suffixes_Before_Matching()
        {
            var provider = CreateProvider<NamingAffixProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<AffixSource, AffixDestination>(new AffixSource
            {
                m_status = "ready"
            });

            Assert.Equal("ready", destination.StatusDto);
        }

        [Fact]
        public void Map_Uses_Naming_Conventions_For_Constructor_Parameters()
        {
            var provider = CreateProvider<NamingConventionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ConstructorSnakeOrder, ConstructorPascalOrderDto>(new ConstructorSnakeOrder
            {
                customer_name = "Katherine"
            });

            Assert.Equal("Katherine", destination.CustomerName);
        }

        [Fact]
        public void Map_Uses_Naming_Conventions_For_Flattening()
        {
            var provider = CreateProvider<NamingConventionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<SnakeEnvelope, PascalEnvelopeDto>(new SnakeEnvelope
            {
                customer = new SnakeCustomer
                {
                    first_name = "Dorothy"
                }
            });

            Assert.Equal("Dorothy", destination.CustomerFirstName);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class NamingConventionOptionsProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<SnakeOrder, PascalOrderDto>();
            }
        }

        public sealed class NamingConventionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.UseSourceNamingConvention(SnakeCaseNamingConvention.Instance);
                builder.UseDestinationNamingConvention(PascalCaseNamingConvention.Instance);
                builder.CreateMap<ProfileSnakeOrder, ProfilePascalOrderDto>();
                builder.CreateMap<ConstructorSnakeOrder, ConstructorPascalOrderDto>();
                builder.CreateMap<SnakeEnvelope, PascalEnvelopeDto>();
            }
        }

        public sealed class NamingAffixProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.RecognizeSourcePrefixes("m_");
                builder.RecognizeDestinationSuffixes("Dto");
                builder.CreateMap<AffixSource, AffixDestination>();
            }
        }

        public sealed class SnakeOrder
        {
            public string customer_name { get; set; }

            public decimal order_total { get; set; }
        }

        public sealed class PascalOrderDto
        {
            public string CustomerName { get; set; }

            public decimal OrderTotal { get; set; }
        }

        public sealed class ProfileSnakeOrder
        {
            public string customer_name { get; set; }
        }

        public sealed class ProfilePascalOrderDto
        {
            public string CustomerName { get; set; }
        }

        public sealed class AffixSource
        {
            public string m_status { get; set; }
        }

        public sealed class AffixDestination
        {
            public string StatusDto { get; set; }
        }

        public sealed class ConstructorSnakeOrder
        {
            public string customer_name { get; set; }
        }

        public sealed class ConstructorPascalOrderDto
        {
            public ConstructorPascalOrderDto(string customerName)
            {
                CustomerName = customerName;
            }

            public string CustomerName { get; }
        }

        public sealed class SnakeEnvelope
        {
            public SnakeCustomer customer { get; set; }
        }

        public sealed class SnakeCustomer
        {
            public string first_name { get; set; }
        }

        public sealed class PascalEnvelopeDto
        {
            public string CustomerFirstName { get; set; }
        }
    }
}
