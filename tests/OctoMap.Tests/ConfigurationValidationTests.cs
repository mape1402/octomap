using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class ConfigurationValidationTests
    {
        [Fact]
        public void Validate_Returns_Issue_For_Invalid_Destination_Constructor()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(InvalidConstructorProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var report = configuration.Validate();

            Assert.False(report.IsValid);
            Assert.Contains(report.Issues, x => x.Message.Contains("public parameterless constructor"));
        }

        [Fact]
        public void AssertValid_Throws_For_Invalid_Configuration()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(InvalidConstructorProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            Assert.Throws<OctoMapValidationException>(() => configuration.AssertValid());
        }

        [Fact]
        public void Map_Throws_Validation_Exception_For_Unsupported_Expression()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(UnsupportedExpressionProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var exception = Assert.Throws<OctoMapValidationException>(() => mapper.Map<ValidationSource, ValidationDestination>(new ValidationSource { Name = "ada" }));

            Assert.Contains(exception.Report.Issues, x => x.MemberName == nameof(ValidationDestination.Name));
        }

        public sealed class InvalidConstructorProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ValidationSource, NoDefaultConstructorDestination>();
            }
        }

        public sealed class UnsupportedExpressionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ValidationSource, ValidationDestination>()
                    .ForMember(x => x.Name, x => x.MapFrom(s => s.Name.ToUpperInvariant()));
            }
        }

        public sealed class ValidationSource
        {
            public string Name { get; set; }
        }

        public sealed class ValidationDestination
        {
            public string Name { get; set; }
        }

        public sealed class NoDefaultConstructorDestination
        {
            public NoDefaultConstructorDestination(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }
    }
}
