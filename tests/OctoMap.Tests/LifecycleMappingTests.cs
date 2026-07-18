using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class LifecycleMappingTests
    {
        [Fact]
        public void Map_Executes_Inline_Lifecycle_Actions_Around_Member_Assignment()
        {
            InlineLifecycleProfile.Events.Clear();
            var provider = CreateProvider<InlineLifecycleProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<InlineLifecycleSource, InlineLifecycleDestination>(new InlineLifecycleSource
            {
                Name = "Ada"
            });

            Assert.Equal("Ada", destination.Name);
            Assert.Equal(new[] { "before:", "after:Ada" }, InlineLifecycleProfile.Events);
        }

        [Fact]
        public void Map_Resolves_Lifecycle_Action_From_ServiceProvider()
        {
            var provider = CreateProvider<ServiceLifecycleProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ServiceLifecycleSource, ServiceLifecycleDestination>(new ServiceLifecycleSource
            {
                Name = "Grace"
            });

            Assert.Equal("Grace after", destination.Name);
        }

        [Fact]
        public void Map_To_Existing_Destination_Executes_Lifecycle_Actions()
        {
            InlineLifecycleProfile.Events.Clear();
            var provider = CreateProvider<InlineLifecycleProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var destination = new InlineLifecycleDestination
            {
                Name = "existing"
            };

            var result = mapper.Map(new InlineLifecycleSource { Name = "Katherine" }, destination);

            Assert.Same(destination, result);
            Assert.Equal("Katherine", result.Name);
            Assert.Equal(new[] { "before:existing", "after:Katherine" }, InlineLifecycleProfile.Events);
        }

        [Fact]
        public void ProjectTo_Throws_When_Map_Uses_Lifecycle_Actions()
        {
            var provider = CreateProvider<InlineLifecycleProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var exception = Assert.Throws<NotSupportedException>(() => Array.Empty<InlineLifecycleSource>().AsQueryable().ProjectTo<InlineLifecycleDestination>(mapper.ProjectionBuilder).ToArray());

            Assert.Contains("runtime-only", exception.Message);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddTransient<ServiceAfterMapAction>();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class InlineLifecycleProfile : OctoMapProfile
        {
            public static readonly List<string> Events = new();

            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<InlineLifecycleSource, InlineLifecycleDestination>()
                    .BeforeMap((source, destination, context) => Events.Add("before:" + destination.Name))
                    .AfterMap((source, destination, context) => Events.Add("after:" + destination.Name));
            }
        }

        public sealed class ServiceLifecycleProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ServiceLifecycleSource, ServiceLifecycleDestination>()
                    .AfterMap<ServiceAfterMapAction>();
            }
        }

        public sealed class InlineLifecycleSource
        {
            public string Name { get; set; }
        }

        public sealed class InlineLifecycleDestination
        {
            public string Name { get; set; }
        }

        public sealed class ServiceLifecycleSource
        {
            public string Name { get; set; }
        }

        public sealed class ServiceLifecycleDestination
        {
            public string Name { get; set; }
        }

        public sealed class ServiceAfterMapAction : IMappingAction<ServiceLifecycleSource, ServiceLifecycleDestination>
        {
            public void Process(ServiceLifecycleSource source, ServiceLifecycleDestination destination, IMapContext context)
                => destination.Name += " after";
        }
    }
}
