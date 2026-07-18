using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class ConstructorMappingTests
    {
        [Fact]
        public void Map_Uses_Convention_Constructor_For_Record_Destination()
        {
            var provider = CreateProvider<ConstructorConventionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ConstructorCustomer, ConstructorCustomerDto>(new ConstructorCustomer
            {
                Id = 7,
                Name = "Ada"
            });

            Assert.Equal(7, destination.Id);
            Assert.Equal("Ada", destination.Name);
        }

        [Fact]
        public void Map_Uses_Constructor_And_Assigns_Settable_Members()
        {
            var provider = CreateProvider<ConstructorConventionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ConstructorOrder, ConstructorOrderDto>(new ConstructorOrder
            {
                Id = 42,
                Number = "ORD-42",
                Status = "Ready"
            });

            Assert.Equal(42, destination.Id);
            Assert.Equal("ORD-42", destination.Number);
            Assert.Equal("Ready", destination.Status);
        }

        [Fact]
        public void Map_Uses_Configured_Construction_Expression()
        {
            var provider = CreateProvider<ConstructUsingProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ConstructorCustomer, ExplicitCustomerDto>(new ConstructorCustomer
            {
                Id = 8,
                Name = " grace hopper "
            });

            Assert.Equal(8, destination.Id);
            Assert.Equal("GRACE HOPPER", destination.Name);
            Assert.Equal("configured", destination.Source);
        }

        [Fact]
        public void Validate_Returns_Issue_When_Constructor_Parameter_Cannot_Be_Mapped()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(MissingConstructorParameterProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var report = configuration.Validate();

            Assert.False(report.IsValid);
            Assert.Contains(report.Issues, x => x.Message.Contains("constructor parameters", StringComparison.Ordinal));
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class ConstructorConventionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ConstructorCustomer, ConstructorCustomerDto>();
                builder.CreateMap<ConstructorOrder, ConstructorOrderDto>();
            }
        }

        public sealed class ConstructUsingProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ConstructorCustomer, ExplicitCustomerDto>()
                    .ConstructUsing(s => new ExplicitCustomerDto(s.Id, s.Name.Trim().ToUpperInvariant()))
                    .ForMember(x => x.Source, x => x.UseValue("configured"));
            }
        }

        public sealed class MissingConstructorParameterProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ConstructorCustomer, MissingParameterDto>();
            }
        }

        public sealed class ConstructorCustomer
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public sealed record ConstructorCustomerDto(int Id, string Name);

        public sealed class ConstructorOrder
        {
            public int Id { get; set; }

            public string Number { get; set; }

            public string Status { get; set; }
        }

        public sealed class ConstructorOrderDto
        {
            public ConstructorOrderDto(int id, string number)
            {
                Id = id;
                Number = number;
            }

            public int Id { get; }

            public string Number { get; }

            public string Status { get; set; }
        }

        public sealed class ExplicitCustomerDto
        {
            public ExplicitCustomerDto(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public int Id { get; }

            public string Name { get; }

            public string Source { get; set; }
        }

        public sealed class MissingParameterDto
        {
            public MissingParameterDto(int id, string missingName)
            {
                Id = id;
                MissingName = missingName;
            }

            public int Id { get; }

            public string MissingName { get; }
        }
    }
}
