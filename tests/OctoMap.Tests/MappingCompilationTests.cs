using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class MappingCompilationTests
    {
        [Fact]
        public void CompileMap_Compiles_Specific_Configured_Map()
        {
            var provider = CreateProvider<CompilationProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            mapper.CompileMap<CompilationSource, CompilationDestination>();

            var destination = mapper.Map<CompilationSource, CompilationDestination>(new CompilationSource
            {
                Name = "Ada"
            });

            Assert.Equal("Ada", destination.Name);
        }

        [Fact]
        public void CompileMappings_Compiles_All_Configured_Maps()
        {
            var provider = CreateProvider<CompilationProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            mapper.CompileMappings();

            Assert.Equal("Grace", mapper.Map<CompilationSecondSource, CompilationSecondDestination>(new CompilationSecondSource
            {
                Name = "Grace"
            }).Name);
        }

        [Fact]
        public void CompileMappings_Throws_For_Invalid_Configured_Map()
        {
            var provider = CreateProvider<InvalidCompilationProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            Assert.Throws<OctoMapValidationException>(() => mapper.CompileMappings());
        }

        [Fact]
        public void CompileMap_Compiles_Multi_Source_Map()
        {
            var provider = CreateProvider<CompilationProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            mapper.CompileMap<CompilationSummaryDestination>(typeof(CompilationSource), typeof(CompilationSecondSource));

            var summary = mapper.Map<CompilationSummaryDestination>(SourceSet.Of(
                new CompilationSource { Name = "Ada" },
                new CompilationSecondSource { Name = "Lovelace" }));

            Assert.Equal("Ada Lovelace", summary.Label);
        }

        [Fact]
        public void GetConfiguredMaps_Returns_Configured_Single_And_Multi_Source_Maps()
        {
            var provider = CreateProvider<CompilationProfile>();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var maps = configuration.GetConfiguredMaps();

            Assert.Contains(maps, x => x.SourceTypes.SequenceEqual(new[] { typeof(CompilationSource) })
                && x.DestinationType == typeof(CompilationDestination));
            Assert.Contains(maps, x => x.SourceTypes.SequenceEqual(new[] { typeof(CompilationSource), typeof(CompilationSecondSource) })
                && x.DestinationType == typeof(CompilationSummaryDestination));
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(registration => registration.AddProfile<TProfile>());
            return services.BuildServiceProvider();
        }

        public sealed class CompilationProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<CompilationSource, CompilationDestination>();
                builder.CreateMap<CompilationSecondSource, CompilationSecondDestination>();
                builder.CreateMultiMap<CompilationSummaryDestination>()
                    .From<CompilationSource>(map => map.ForMember(x => x.FirstName, x => x.MapFrom(s => s.Name)))
                    .From<CompilationSecondSource>(map => map.ForMember(x => x.LastName, x => x.MapFrom(s => s.Name)))
                    .ForMember(x => x.Label, x => x.MapFrom(ctx => ctx.Get<CompilationSource>().Name + " " + ctx.Get<CompilationSecondSource>().Name));
            }
        }

        public sealed class InvalidCompilationProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
                => builder.CreateMap<InvalidCompilationSource, InvalidCompilationDestination>();
        }

        public sealed class CompilationSource
        {
            public string Name { get; set; }
        }

        public sealed class CompilationDestination
        {
            public string Name { get; set; }
        }

        public sealed class CompilationSecondSource
        {
            public string Name { get; set; }
        }

        public sealed class CompilationSecondDestination
        {
            public string Name { get; set; }
        }

        public sealed class CompilationSummaryDestination
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string Label { get; set; }
        }

        public sealed class InvalidCompilationSource
        {
            public string Name { get; set; }
        }

        public sealed class InvalidCompilationDestination
        {
            public InvalidCompilationDestination(string missing)
            {
                Missing = missing;
            }

            public string Missing { get; }
        }
    }
}
