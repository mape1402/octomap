using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class AttributeMappingTests
    {
        [Fact]
        public void MapFromAttribute_Registers_Map_And_Member_Attributes()
        {
            var provider = CreateProvider<AttributeProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<AttributeOrder, AttributeOrderDto>(new AttributeOrder
            {
                StatusCode = "ready",
                Description = null,
                InternalCode = "secret"
            });

            Assert.Equal("ready", destination.Status);
            Assert.Equal("No description", destination.Description);
            Assert.Null(destination.InternalCode);
        }

        [Fact]
        public void Fluent_Configuration_Overrides_Attribute_Member_Configuration()
        {
            var provider = CreateProvider<AttributeProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<AttributeOverrideOrder, AttributeOverrideOrderDto>(new AttributeOverrideOrder
            {
                StatusCode = "attribute",
                Label = "fluent"
            });

            Assert.Equal("fluent", destination.Status);
        }

        [Fact]
        public void MapConstructorAttribute_Selects_Preferred_Constructor()
        {
            var provider = CreateProvider<AttributeProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<AttributeConstructorSource, AttributeConstructorDestination>(new AttributeConstructorSource
            {
                Code = "OCTO"
            });

            Assert.Equal("OCTO", destination.Code);
            Assert.True(destination.UsedMarkedConstructor);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class AttributeProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<AttributeOverrideOrder, AttributeOverrideOrderDto>()
                    .ForMember(x => x.Status, x => x.MapFrom(s => s.Label));
            }
        }

        public sealed class AttributeOrder
        {
            public string StatusCode { get; set; }

            public string Description { get; set; }

            public string InternalCode { get; set; }
        }

        [MapFrom(typeof(AttributeOrder))]
        public sealed class AttributeOrderDto
        {
            [MapName("StatusCode")]
            public string Status { get; set; }

            [NullSubstitute("No description")]
            public string Description { get; set; }

            [IgnoreMap]
            public string InternalCode { get; set; }
        }

        public sealed class AttributeOverrideOrder
        {
            public string StatusCode { get; set; }

            public string Label { get; set; }
        }

        [MapFrom(typeof(AttributeOverrideOrder))]
        public sealed class AttributeOverrideOrderDto
        {
            [MapName("StatusCode")]
            public string Status { get; set; }
        }

        [MapTo(typeof(AttributeConstructorDestination))]
        public sealed class AttributeConstructorSource
        {
            public string Code { get; set; }
        }

        public sealed class AttributeConstructorDestination
        {
            public AttributeConstructorDestination(string value, string other)
            {
                Code = value + other;
            }

            [MapConstructor]
            public AttributeConstructorDestination(string code)
            {
                Code = code;
                UsedMarkedConstructor = true;
            }

            public string Code { get; }

            public bool UsedMarkedConstructor { get; }
        }
    }
}
