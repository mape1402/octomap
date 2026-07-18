using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class TypeConversionMappingTests
    {
        [Fact]
        public void Map_Uses_Built_In_String_To_Guid_And_Enum_Conversions()
        {
            var provider = CreateProvider<BuiltInConversionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ConversionOrder, ConversionOrderDto>(new ConversionOrder
            {
                Id = "2f2ff31a-8db6-44b9-a9bb-ea7bbf6a5f95",
                Status = "Created",
                Total = 10
            });

            Assert.Equal(Guid.Parse("2f2ff31a-8db6-44b9-a9bb-ea7bbf6a5f95"), destination.Id);
            Assert.Equal(ConversionStatus.Created, destination.Status);
            Assert.Equal(10m, destination.Total);
        }

        [Fact]
        public void Map_Uses_Configured_Global_Expression_Converter()
        {
            var provider = CreateProvider<ExpressionConversionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ExpressionConversionSource, ExpressionConversionDestination>(new ExpressionConversionSource
            {
                Code = "abc"
            });

            Assert.Equal("ABC", destination.Code.Value);
        }

        [Fact]
        public void Map_Uses_Configured_Global_DI_Converter()
        {
            var services = new ServiceCollection();
            services.AddTransient<MoneyTextConverter>();
            services.AddOctoMap(typeof(DiConversionProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<DiConversionSource, DiConversionDestination>(new DiConversionSource
            {
                Total = 19.95m
            });

            Assert.Equal("$19.95", destination.Total.Value);
        }

        [Fact]
        public void ProjectTo_Uses_Projectable_Global_Expression_Converter()
        {
            var provider = CreateProvider<ExpressionConversionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = new[]
            {
                new ExpressionConversionSource { Code = "abc" }
            }
            .AsQueryable()
            .ProjectTo<ExpressionConversionDestination>(mapper.ProjectionBuilder)
            .Single();

            Assert.Equal("ABC", destination.Code.Value);
        }

        [Fact]
        public void Map_Uses_Built_In_Conversion_For_Constructor_Parameters()
        {
            var provider = CreateProvider<ConstructorConversionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ConstructorConversionSource, ConstructorConversionDestination>(new ConstructorConversionSource
            {
                Id = "2f2ff31a-8db6-44b9-a9bb-ea7bbf6a5f95"
            });

            Assert.Equal(Guid.Parse("2f2ff31a-8db6-44b9-a9bb-ea7bbf6a5f95"), destination.Id);
        }

        [Fact]
        public void AssertValid_Fails_When_Matching_Member_Has_No_Type_Converter()
        {
            var provider = CreateProvider<MissingConversionProfile>();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var exception = Assert.Throws<OctoMapValidationException>(() => configuration.AssertValid());

            Assert.Contains("No type converter is registered", exception.Message);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class BuiltInConversionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ConversionOrder, ConversionOrderDto>();
            }
        }

        public sealed class ExpressionConversionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateConverter<string, UpperCode>(x => new UpperCode(x.ToUpperInvariant()));
                builder.CreateMap<ExpressionConversionSource, ExpressionConversionDestination>();
            }
        }

        public sealed class DiConversionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateConverter<MoneyTextConverter, decimal, MoneyText>();
                builder.CreateMap<DiConversionSource, DiConversionDestination>();
            }
        }

        public sealed class ConstructorConversionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ConstructorConversionSource, ConstructorConversionDestination>();
            }
        }

        public sealed class MissingConversionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<MissingConversionSource, MissingConversionDestination>();
            }
        }

        public sealed class ConversionOrder
        {
            public string Id { get; set; }

            public string Status { get; set; }

            public int Total { get; set; }
        }

        public sealed class ConversionOrderDto
        {
            public Guid Id { get; set; }

            public ConversionStatus Status { get; set; }

            public decimal Total { get; set; }
        }

        public enum ConversionStatus
        {
            Created
        }

        public sealed class ExpressionConversionSource
        {
            public string Code { get; set; }
        }

        public sealed class ExpressionConversionDestination
        {
            public UpperCode Code { get; set; }
        }

        public sealed class UpperCode
        {
            public UpperCode(string value)
            {
                Value = value;
            }

            public string Value { get; }

            public override string ToString()
                => Value;
        }

        public sealed class DiConversionSource
        {
            public decimal Total { get; set; }
        }

        public sealed class DiConversionDestination
        {
            public MoneyText Total { get; set; }
        }

        public sealed record MoneyText(string Value)
        {
            public override string ToString()
                => Value;
        }

        public sealed class MoneyTextConverter : IValueConverter<decimal, MoneyText>
        {
            public MoneyText Convert(decimal sourceMember, IMapContext context)
                => new($"${sourceMember:0.00}");
        }

        public sealed class ConstructorConversionSource
        {
            public string Id { get; set; }
        }

        public sealed record ConstructorConversionDestination(Guid Id);

        public sealed class MissingConversionSource
        {
            public string Value { get; set; }
        }

        public sealed class MissingConversionDestination
        {
            public Uri Value { get; set; }
        }
    }
}
