using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class MultiSourceMappingTests
    {
        [Fact]
        public void Map_Uses_Explicit_Multi_Source_Configuration()
        {
            var provider = CreateProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<OrderSummaryDto>(SourceSet.Of(
                new OrderSummaryOrder
                {
                    Id = 42,
                    Code = "ORD-42",
                    Total = 125.50m
                },
                new OrderSummaryCustomer
                {
                    Name = "Ada Lovelace"
                }));

            Assert.Equal(42, destination.OrderId);
            Assert.Equal(125.50m, destination.Total);
            Assert.Equal("Ada Lovelace", destination.CustomerName);
            Assert.Equal("ORD-42 - ADA LOVELACE", destination.Label);
        }

        [Fact]
        public void Map_Uses_Typed_Two_Source_Configuration()
        {
            var provider = CreateProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<OrderSummaryOrder, OrderSummaryCustomer, OrderSummaryDto>(
                new OrderSummaryOrder
                {
                    Id = 42,
                    Code = "ORD-42",
                    Total = 125.50m
                },
                new OrderSummaryCustomer
                {
                    Name = "Ada Lovelace"
                });

            Assert.Equal(42, destination.OrderId);
            Assert.Equal(125.50m, destination.Total);
            Assert.Equal("Ada Lovelace", destination.CustomerName);
            Assert.Equal("ORD-42 - ADA LOVELACE", destination.Label);
        }

        [Fact]
        public void CompileMap_Compiles_Typed_Two_Source_Map()
        {
            var provider = CreateProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            mapper.CompileMap<OrderSummaryOrder, OrderSummaryCustomer, OrderSummaryDto>();

            var destination = mapper.Map<OrderSummaryOrder, OrderSummaryCustomer, OrderSummaryDto>(
                new OrderSummaryOrder { Id = 7, Code = "ORD-7", Total = 25m },
                new OrderSummaryCustomer { Name = "Grace Hopper" });

            Assert.Equal("ORD-7 - GRACE HOPPER", destination.Label);
        }

        [Fact]
        public void Map_Uses_Typed_Ten_Source_Configuration()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(registration => registration.AddProfile<TenSourceProfile>());
            var mapper = services.BuildServiceProvider().GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Source1, Source2, Source3, Source4, Source5, Source6, Source7, Source8, Source9, Source10, TenSourceDto>(
                new Source1 { Value = 1 },
                new Source2 { Value = 2 },
                new Source3 { Value = 3 },
                new Source4 { Value = 4 },
                new Source5 { Value = 5 },
                new Source6 { Value = 6 },
                new Source7 { Value = 7 },
                new Source8 { Value = 8 },
                new Source9 { Value = 9 },
                new Source10 { Value = 10 });

            Assert.Equal(55, destination.Sum);
        }

        [Fact]
        public void Map_Throws_When_Multi_Source_Map_Is_Not_Configured()
        {
            var provider = CreateProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var exception = Assert.Throws<InvalidOperationException>(() => mapper.Map<UnconfiguredMultiSourceDto>(SourceSet.Of(
                new OrderSummaryOrder(),
                new OrderSummaryCustomer())));

            Assert.Contains("must be registered explicitly", exception.Message);
        }

        [Fact]
        public void SourceSet_Get_Throws_When_Source_Type_Is_Ambiguous()
        {
            var sources = SourceSet.Of(new AmbiguousOrder(), new SpecialAmbiguousOrder());

            var exception = Assert.Throws<InvalidOperationException>(() => sources.Get<AmbiguousOrder>());

            Assert.Contains("more than one source assignable", exception.Message);
        }

        [Fact]
        public void Validate_Returns_Issue_When_Destination_Member_Is_Configured_More_Than_Once()
        {
            var configuration = BuildConfiguration<DuplicateDestinationProfile>();

            var report = configuration.Validate();

            Assert.False(report.IsValid);
            Assert.Contains(report.Issues, x => x.Message.Contains("configured more than once", StringComparison.Ordinal));
        }

        [Fact]
        public void Validate_Returns_Issue_When_Context_Source_Is_Ambiguous()
        {
            var configuration = BuildConfiguration<AmbiguousContextProfile>();

            var report = configuration.Validate();

            Assert.False(report.IsValid);
            Assert.Contains(report.Issues, x => x.Message.Contains("more than one source assignable", StringComparison.Ordinal));
        }

        private static ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(MultiSourceProfile).Assembly);
            return services.BuildServiceProvider();
        }

        private static IOctoMapConfiguration BuildConfiguration<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var builder = new OctoMap.Configuration.OctoMapConfigurationBuilder();
            new TProfile().Configure(builder);
            return builder.Build();
        }

        public sealed class MultiSourceProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMultiMap<OrderSummaryDto>()
                    .From<OrderSummaryOrder>(map => map
                        .ForMember(x => x.OrderId, x => x.MapFrom(s => s.Id))
                        .ForMember(x => x.Total, x => x.MapFrom(s => s.Total)))
                    .From<OrderSummaryCustomer>(map => map
                        .ForMember(x => x.CustomerName, x => x.MapFrom(s => s.Name)))
                    .ForMember(x => x.Label, x => x.MapFrom(ctx =>
                        string.Concat(ctx.Get<OrderSummaryOrder>().Code.Trim(), " - ", ctx.Get<OrderSummaryCustomer>().Name.ToUpperInvariant())));
            }
        }

        public sealed class OrderSummaryOrder
        {
            public int Id { get; set; }

            public string Code { get; set; }

            public decimal Total { get; set; }
        }

        public sealed class OrderSummaryCustomer
        {
            public string Name { get; set; }
        }

        public sealed class OrderSummaryDto
        {
            public int OrderId { get; set; }

            public decimal Total { get; set; }

            public string CustomerName { get; set; }

            public string Label { get; set; }
        }

        public sealed class UnconfiguredMultiSourceDto
        {
            public int Id { get; set; }
        }

        public class AmbiguousOrder
        {
            public string Code { get; set; }
        }

        public sealed class SpecialAmbiguousOrder : AmbiguousOrder
        {
        }

        public sealed class AmbiguousCustomer
        {
            public string Name { get; set; }
        }

        public sealed class AmbiguousDto
        {
            public string Label { get; set; }
        }

        public sealed class DuplicateDestinationProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMultiMap<AmbiguousDto>()
                    .From<AmbiguousOrder>(map => map
                        .ForMember(x => x.Label, x => x.MapFrom(s => s.Code)))
                    .From<AmbiguousCustomer>(map => map
                        .ForMember(x => x.Label, x => x.MapFrom(s => s.Name)));
            }
        }

        public sealed class AmbiguousContextProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMultiMap<AmbiguousDto>()
                    .From<AmbiguousOrder>(map => map
                        .ForMember(x => x.Label, x => x.MapFrom(s => s.Code)))
                    .From<SpecialAmbiguousOrder>(map => map
                        .ForMember(x => x.Label, x => x.UseValue("special")))
                    .ForMember(x => x.Label, x => x.MapFrom(ctx => ctx.Get<AmbiguousOrder>().Code));
            }
        }

        public sealed class TenSourceProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMultiMap<TenSourceDto>()
                    .From<Source1>(_ => { })
                    .From<Source2>(_ => { })
                    .From<Source3>(_ => { })
                    .From<Source4>(_ => { })
                    .From<Source5>(_ => { })
                    .From<Source6>(_ => { })
                    .From<Source7>(_ => { })
                    .From<Source8>(_ => { })
                    .From<Source9>(_ => { })
                    .From<Source10>(_ => { })
                    .ForMember(x => x.Sum, x => x.MapFrom(ctx =>
                        ctx.Get<Source1>().Value
                        + ctx.Get<Source2>().Value
                        + ctx.Get<Source3>().Value
                        + ctx.Get<Source4>().Value
                        + ctx.Get<Source5>().Value
                        + ctx.Get<Source6>().Value
                        + ctx.Get<Source7>().Value
                        + ctx.Get<Source8>().Value
                        + ctx.Get<Source9>().Value
                        + ctx.Get<Source10>().Value));
            }
        }

        public sealed class TenSourceDto
        {
            public int Sum { get; set; }
        }

        public sealed class Source1 { public int Value { get; set; } }

        public sealed class Source2 { public int Value { get; set; } }

        public sealed class Source3 { public int Value { get; set; } }

        public sealed class Source4 { public int Value { get; set; } }

        public sealed class Source5 { public int Value { get; set; } }

        public sealed class Source6 { public int Value { get; set; } }

        public sealed class Source7 { public int Value { get; set; } }

        public sealed class Source8 { public int Value { get; set; } }

        public sealed class Source9 { public int Value { get; set; } }

        public sealed class Source10 { public int Value { get; set; } }
    }
}
