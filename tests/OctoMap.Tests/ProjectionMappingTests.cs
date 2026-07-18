using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class ProjectionMappingTests
    {
        [Fact]
        public void ProjectTo_Uses_Configured_Direct_And_MapFrom_Members()
        {
            var provider = CreateProvider<ProjectionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = new[]
            {
                new ProjectionOrder
                {
                    Id = 1,
                    Code = "ord-1",
                    Total = 12.345m,
                    Customer = new ProjectionCustomer
                    {
                        Name = "Ada",
                        Address = new ProjectionAddress { City = "London" }
                    }
                }
            }.AsQueryable().ProjectTo<ProjectionOrderDto>(mapper.ProjectionBuilder).Single();

            Assert.Equal(1, destination.Id);
            Assert.Equal("ORD-1", destination.Code);
            Assert.Equal(12.34m, destination.RoundedTotal);
            Assert.Equal("Ada", destination.CustomerName);
            Assert.Equal("London", destination.CustomerAddressCity);
        }

        [Fact]
        public void ProjectTo_Uses_Constructor_Projection()
        {
            var provider = CreateProvider<ProjectionConstructorProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = new[]
            {
                new ProjectionCustomer
                {
                    Id = 7,
                    Name = "Grace"
                }
            }.AsQueryable().ProjectTo<ProjectionCustomerDto>(mapper.ProjectionBuilder).Single();

            Assert.Equal(7, destination.Id);
            Assert.Equal("Grace", destination.Name);
        }

        [Fact]
        public void Build_Throws_When_Map_Uses_Runtime_Resolver()
        {
            var provider = CreateProvider<RuntimeResolverProjectionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var exception = Assert.Throws<NotSupportedException>(() => Array.Empty<ProjectionOrder>().AsQueryable().ProjectTo<RuntimeResolverProjectionDto>(mapper.ProjectionBuilder).ToArray());

            Assert.Contains("runtime-only", exception.Message);
        }

        [Fact]
        public void ProjectTo_Uses_Expression_Converter_For_Collection_Items()
        {
            var provider = CreateProvider<ProjectionCollectionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = new[]
            {
                new ProjectionCollectionOrder
                {
                    Codes = new List<string> { "a-1", "b-2" }
                }
            }.AsQueryable().ProjectTo<ProjectionCollectionOrderDto>(mapper.ProjectionBuilder).Single();

            Assert.Equal(new[] { "A-1", "B-2" }, destination.Codes.Select(x => x.Value));
        }

        [Fact]
        public void ProjectTo_Throws_When_Collection_Item_Conversion_Uses_Service_Converter()
        {
            var provider = CreateProvider<ProjectionCollectionServiceConverterProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var exception = Assert.Throws<NotSupportedException>(() => new[]
            {
                new ProjectionCollectionAmountOrder
                {
                    Amounts = new List<decimal> { 12.3m }
                }
            }.AsQueryable().ProjectTo<ProjectionCollectionAmountOrderDto>(mapper.ProjectionBuilder).ToArray());

            Assert.Contains("runtime-only", exception.Message);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddTransient<ProjectionCodeResolver>();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class ProjectionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ProjectionOrder, ProjectionOrderDto>()
                    .ForMember(x => x.Code, x => x.MapFrom(s => s.Code.ToUpperInvariant()))
                    .ForMember(x => x.RoundedTotal, x => x.MapFrom(s => decimal.Round(s.Total, 2)));
            }
        }

        public sealed class ProjectionConstructorProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ProjectionCustomer, ProjectionCustomerDto>();
            }
        }

        public sealed class RuntimeResolverProjectionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ProjectionOrder, RuntimeResolverProjectionDto>()
                    .ForMember(x => x.Code, x => x.ResolveUsing<ProjectionCodeResolver>());
            }
        }

        public sealed class ProjectionCollectionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateConverter<string, ProjectionSkuCode>(x => new ProjectionSkuCode(x.ToUpperInvariant()));
                builder.CreateMap<ProjectionCollectionOrder, ProjectionCollectionOrderDto>();
            }
        }

        public sealed class ProjectionCollectionServiceConverterProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateConverter<ProjectionAmountTextConverter, decimal, ProjectionAmountText>();
                builder.CreateMap<ProjectionCollectionAmountOrder, ProjectionCollectionAmountOrderDto>();
            }
        }

        public sealed class ProjectionOrder
        {
            public int Id { get; set; }

            public string Code { get; set; }

            public decimal Total { get; set; }

            public ProjectionCustomer Customer { get; set; }
        }

        public sealed class ProjectionCustomer
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public ProjectionAddress Address { get; set; }
        }

        public sealed class ProjectionAddress
        {
            public string City { get; set; }
        }

        public sealed class ProjectionOrderDto
        {
            public int Id { get; set; }

            public string Code { get; set; }

            public decimal RoundedTotal { get; set; }

            public string CustomerName { get; set; }

            public string CustomerAddressCity { get; set; }
        }

        public sealed record ProjectionCustomerDto(int Id, string Name);

        public sealed class RuntimeResolverProjectionDto
        {
            public string Code { get; set; }
        }

        public sealed class ProjectionCollectionOrder
        {
            public List<string> Codes { get; set; }
        }

        public sealed class ProjectionCollectionOrderDto
        {
            public List<ProjectionSkuCode> Codes { get; set; }
        }

        public sealed record ProjectionSkuCode(string Value);

        public sealed class ProjectionCollectionAmountOrder
        {
            public List<decimal> Amounts { get; set; }
        }

        public sealed class ProjectionCollectionAmountOrderDto
        {
            public List<ProjectionAmountText> Amounts { get; set; }
        }

        public sealed record ProjectionAmountText(string Value);

        public sealed class ProjectionCodeResolver : IValueResolver<ProjectionOrder, RuntimeResolverProjectionDto, string>
        {
            public string Resolve(ProjectionOrder source, RuntimeResolverProjectionDto destination, IMapContext context)
                => source.Code;
        }

        public sealed class ProjectionAmountTextConverter : IValueConverter<decimal, ProjectionAmountText>
        {
            public ProjectionAmountText Convert(decimal source, IMapContext context)
                => new(source.ToString("$0.00"));
        }
    }
}
