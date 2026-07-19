using Microsoft.Extensions.DependencyInjection;
using OctoMap.Naming;

namespace OctoMap.Tests
{
    public class ConfigurationOrganizationTests
    {
        [Fact]
        public void Map_Uses_Options_Snapshot_From_Map_Declaration()
        {
            var provider = CreateProvider<SnapshotProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var legacy = mapper.Map<SnapshotLegacySource, SnapshotLegacyDestination>(new SnapshotLegacySource
            {
                customer_name = "Ada"
            });
            var exact = mapper.Map<SnapshotExactSource, SnapshotExactDestination>(new SnapshotExactSource
            {
                Name = "Grace"
            });

            Assert.Equal("Ada", legacy.CustomerName);
            Assert.Equal("Grace", exact.Name);
        }

        [Fact]
        public void AddOctoMap_Throws_When_Duplicate_Map_Policy_Is_Throw()
        {
            var services = new ServiceCollection();

            var exception = Assert.Throws<InvalidOperationException>(() => services.AddOctoMap(registration =>
            {
                registration.Options.DuplicateMapPolicy = DuplicateMapPolicy.Throw;
                registration.AddProfile<DuplicateMapFirstProfile>();
                registration.AddProfile<DuplicateMapSecondProfile>();
            }));

            Assert.Contains("Duplicate map", exception.Message);
            Assert.Contains(nameof(DuplicateMapSource), exception.Message);
            Assert.Contains(nameof(DuplicateMapDestination), exception.Message);
        }

        [Fact]
        public void AddOctoMap_Replaces_Previous_Map_When_Duplicate_Map_Policy_Is_Replace()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(registration =>
            {
                registration.Options.DuplicateMapPolicy = DuplicateMapPolicy.Replace;
                registration.AddProfile<ReplaceMapFirstProfile>();
                registration.AddProfile<ReplaceMapSecondProfile>();
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ReplaceMapSource, ReplaceMapDestination>(new ReplaceMapSource
            {
                Name = "Replaced"
            });

            Assert.Equal("Replaced", destination.Name);
        }

        [Fact]
        public void AddProfilesFromAssembly_Uses_Profile_Filter()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(registration =>
            {
                registration.Options.EnableRuntimeImplicitMaps = false;
                registration
                    .AddProfilesFromAssembly(typeof(ConfigurationOrganizationTests).Assembly)
                    .WhereProfile(type => type == typeof(FilteredIncludedProfile));
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var included = mapper.Map<FilteredIncludedSource, FilteredIncludedDestination>(new FilteredIncludedSource
            {
                Name = "Katherine"
            });

            Assert.Equal("Katherine", included.Name);
            Assert.Throws<InvalidOperationException>(() => mapper.Map<FilteredExcludedSource, FilteredExcludedDestination>(new FilteredExcludedSource()));
        }

        [Fact]
        public void AddMaps_Uses_Map_Type_Filter_For_Interface_And_Attribute_Maps()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(registration =>
            {
                registration.Options.EnableRuntimeImplicitMaps = false;
                registration
                    .AddMaps(typeof(ConfigurationOrganizationTests).Assembly)
                    .WhereProfile(type => false)
                    .WhereMapType(type =>
                        type == typeof(AllowedInterfaceDestination)
                        || type == typeof(AllowedAttributeDestination));
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var interfaceDestination = mapper.Map<AllowedInterfaceDestination>(new AllowedInterfaceSource
            {
                Name = "Linus"
            });
            var attributeDestination = mapper.Map<AllowedAttributeDestination>(new AllowedAttributeSource
            {
                Name = "Margaret"
            });

            Assert.Equal("Linus", interfaceDestination.Name);
            Assert.Equal("Margaret", attributeDestination.Name);
            Assert.Throws<InvalidOperationException>(() => mapper.Map<BlockedAttributeSource, BlockedAttributeDestination>(new BlockedAttributeSource()));
        }

        [Fact]
        public void Configuration_Diagnostics_Describe_Loaded_Profiles_And_Map_Declarations()
        {
            var provider = CreateProvider<DiagnosticsOrganizationProfile>();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var profiles = configuration.GetProfiles();
            var description = configuration.DescribeConfiguration();

            Assert.Contains(typeof(DiagnosticsOrganizationProfile).FullName, profiles);
            Assert.Contains(typeof(DiagnosticsSource).FullName, description);
            Assert.Contains(typeof(DiagnosticsDestination).FullName, description);
            Assert.Contains($"Profile {typeof(DiagnosticsOrganizationProfile).FullName}", description);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(registration => registration.AddProfile<TProfile>());
            return services.BuildServiceProvider();
        }

        public sealed class SnapshotProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.UseSourceNamingConvention(SnakeCaseNamingConvention.Instance);
                builder.UseDestinationNamingConvention(PascalCaseNamingConvention.Instance);
                builder.CreateMap<SnapshotLegacySource, SnapshotLegacyDestination>();

                builder.UseSourceNamingConvention(ExactNamingConvention.Instance);
                builder.UseDestinationNamingConvention(ExactNamingConvention.Instance);
                builder.CreateMap<SnapshotExactSource, SnapshotExactDestination>();
            }
        }

        public sealed class DuplicateMapFirstProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
                => builder.CreateMap<DuplicateMapSource, DuplicateMapDestination>();
        }

        public sealed class DuplicateMapSecondProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
                => builder.CreateMap<DuplicateMapSource, DuplicateMapDestination>();
        }

        public sealed class ReplaceMapFirstProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
                => builder.CreateMap<ReplaceMapSource, ReplaceMapDestination>()
                    .ForMember(x => x.Name, x => x.Ignore());
        }

        public sealed class ReplaceMapSecondProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
                => builder.CreateMap<ReplaceMapSource, ReplaceMapDestination>();
        }

        public sealed class FilteredIncludedProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
                => builder.CreateMap<FilteredIncludedSource, FilteredIncludedDestination>();
        }

        public sealed class FilteredExcludedProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
                => builder.CreateMap<FilteredExcludedSource, FilteredExcludedDestination>();
        }

        public sealed class DiagnosticsOrganizationProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
                => builder.CreateMap<DiagnosticsSource, DiagnosticsDestination>();
        }

        public sealed class SnapshotLegacySource
        {
            public string customer_name { get; set; }
        }

        public sealed class SnapshotLegacyDestination
        {
            public string CustomerName { get; set; }
        }

        public sealed class SnapshotExactSource
        {
            public string Name { get; set; }
        }

        public sealed class SnapshotExactDestination
        {
            public string Name { get; set; }
        }

        public sealed class DuplicateMapSource
        {
            public string Name { get; set; }
        }

        public sealed class DuplicateMapDestination
        {
            public string Name { get; set; }
        }

        public sealed class ReplaceMapSource
        {
            public string Name { get; set; }
        }

        public sealed class ReplaceMapDestination
        {
            public string Name { get; set; }
        }

        public sealed class FilteredIncludedSource
        {
            public string Name { get; set; }
        }

        public sealed class FilteredIncludedDestination
        {
            public string Name { get; set; }
        }

        public sealed class FilteredExcludedSource
        {
            public string Name { get; set; }
        }

        public sealed class FilteredExcludedDestination
        {
            public string Name { get; set; }
        }

        public sealed class AllowedInterfaceSource
        {
            public string Name { get; set; }
        }

        public sealed class AllowedInterfaceDestination : IMapFrom<AllowedInterfaceSource>
        {
            public string Name { get; set; }
        }

        public sealed class AllowedAttributeSource
        {
            public string Name { get; set; }
        }

        [MapFrom(typeof(AllowedAttributeSource))]
        public sealed class AllowedAttributeDestination
        {
            public string Name { get; set; }
        }

        public sealed class BlockedAttributeSource
        {
            public string Name { get; set; }
        }

        [MapFrom(typeof(BlockedAttributeSource))]
        public sealed class BlockedAttributeDestination
        {
            public string Name { get; set; }
        }

        public sealed class DiagnosticsSource
        {
            public string Name { get; set; }
        }

        public sealed class DiagnosticsDestination
        {
            public string Name { get; set; }
        }
    }
}
