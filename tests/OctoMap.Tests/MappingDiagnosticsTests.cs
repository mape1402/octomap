using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class MappingDiagnosticsTests
    {
        [Fact]
        public void GetPlan_Returns_Configured_Mapping_Plan()
        {
            var provider = CreateProvider<DiagnosticsProfile>();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var plan = configuration.GetPlan<DiagnosticsOrder, DiagnosticsOrderDto>();

            Assert.Equal(typeof(DiagnosticsOrder), plan.SourceType);
            Assert.Equal(typeof(DiagnosticsOrderDto), plan.DestinationType);
            Assert.Contains(plan.Assignments, x => x.DestinationProperty.Name == nameof(DiagnosticsOrderDto.Id));
            Assert.Contains(plan.Assignments, x => x.DestinationProperty.Name == nameof(DiagnosticsOrderDto.CustomerName));
        }

        [Fact]
        public void DescribeMap_Returns_Human_Readable_Assignment_Report()
        {
            var provider = CreateProvider<DiagnosticsProfile>();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var description = configuration.DescribeMap<DiagnosticsOrder, DiagnosticsOrderDto>();

            Assert.Contains("DiagnosticsOrderDto.Id <- DiagnosticsOrder.Id", description);
            Assert.Contains("DiagnosticsOrderDto.CustomerName <- DiagnosticsOrder.Customer.Name", description);
            Assert.Contains("DiagnosticsOrderDto.StatusLabel <- DiagnosticsStatusResolver", description);
            Assert.Contains("DiagnosticsOrderDto.TotalText <- DiagnosticsTotalConverter", description);
        }

        [Fact]
        public void DescribeMap_Returns_Multi_Source_Assignment_Report()
        {
            var provider = CreateProvider<DiagnosticsProfile>();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var description = configuration.DescribeMap(
                new[] { typeof(DiagnosticsOrder), typeof(DiagnosticsCustomer) },
                typeof(DiagnosticsOrderSummaryDto));

            Assert.Contains("DiagnosticsOrderSummaryDto.OrderId <- DiagnosticsOrder.Id", description);
            Assert.Contains("DiagnosticsOrderSummaryDto.CustomerName <- DiagnosticsCustomer.Name", description);
            Assert.Contains("DiagnosticsOrderSummaryDto.Label <-", description);
        }

        [Fact]
        public void GetPlan_Throws_When_Map_Is_Not_Configured()
        {
            var provider = CreateProvider<DiagnosticsProfile>();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var exception = Assert.Throws<InvalidOperationException>(() => configuration.GetPlan<DiagnosticsCustomer, DiagnosticsOrderDto>());

            Assert.Contains("is not configured", exception.Message);
        }

        [Fact]
        public void DescribeMap_Uses_Registered_Plan_Describer()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMappingPlanDescriber, CustomPlanDescriber>();
            services.AddOctoMap(typeof(DiagnosticsProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var description = configuration.DescribeMap<DiagnosticsOrder, DiagnosticsOrderDto>();

            Assert.Equal("custom plan", description);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddTransient<DiagnosticsStatusResolver>();
            services.AddTransient<DiagnosticsTotalConverter>();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class DiagnosticsProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<DiagnosticsOrder, DiagnosticsOrderDto>()
                    .ForMember(x => x.TotalText, x => x.ConvertUsing<DiagnosticsTotalConverter>(s => s.Total))
                    .ForMember(x => x.StatusLabel, x => x.ResolveUsing<DiagnosticsStatusResolver>());

                builder.CreateMultiMap<DiagnosticsOrderSummaryDto>()
                    .From<DiagnosticsOrder>(map => map
                        .ForMember(x => x.OrderId, x => x.MapFrom(s => s.Id)))
                    .From<DiagnosticsCustomer>(map => map
                        .ForMember(x => x.CustomerName, x => x.MapFrom(s => s.Name)))
                    .ForMember(x => x.Label, x => x.MapFrom(ctx =>
                        ctx.Get<DiagnosticsOrder>().Id + " - " + ctx.Get<DiagnosticsCustomer>().Name));
            }
        }

        public sealed class DiagnosticsOrder
        {
            public int Id { get; set; }

            public decimal Total { get; set; }

            public DiagnosticsCustomer Customer { get; set; }
        }

        public sealed class DiagnosticsCustomer
        {
            public string Name { get; set; }
        }

        public sealed class DiagnosticsOrderDto
        {
            public int Id { get; set; }

            public string CustomerName { get; set; }

            public string StatusLabel { get; set; }

            public string TotalText { get; set; }
        }

        public sealed class DiagnosticsOrderSummaryDto
        {
            public int OrderId { get; set; }

            public string CustomerName { get; set; }

            public string Label { get; set; }
        }

        public sealed class DiagnosticsStatusResolver : IValueResolver<DiagnosticsOrder, DiagnosticsOrderDto, string>
        {
            public string Resolve(DiagnosticsOrder source, DiagnosticsOrderDto destination, IMapContext context)
                => source.Id.ToString();
        }

        public sealed class DiagnosticsTotalConverter : IValueConverter<decimal, string>
        {
            public string Convert(decimal sourceMember, IMapContext context)
                => sourceMember.ToString();
        }

        public sealed class CustomPlanDescriber : IMappingPlanDescriber
        {
            public string Describe(OctoMap.Planning.MappingPlan plan)
                => "custom plan";
        }
    }
}
