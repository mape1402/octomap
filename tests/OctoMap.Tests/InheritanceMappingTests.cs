using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class InheritanceMappingTests
    {
        [Fact]
        public void IncludeBase_Reuses_Explicit_Base_Member_Configuration()
        {
            var provider = CreateProvider<IncludeBaseProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<OnlineOrderSource, OnlineOrderDto>(new OnlineOrderSource
            {
                StatusCode = "ready",
                Channel = "web"
            });

            Assert.Equal("READY", destination.StatusLabel);
            Assert.Equal("web", destination.Channel);
        }

        [Fact]
        public void Map_Uses_Base_Map_For_Derived_Runtime_Source()
        {
            var provider = CreateProvider<BaseDispatchProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<BaseDispatchOrderDto>((object)new DerivedDispatchOrderSource
            {
                StatusCode = "hold"
            });

            Assert.Equal("HOLD", destination.StatusLabel);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class IncludeBaseProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<BaseOrderSource, BaseOrderDto>()
                    .ForMember(x => x.StatusLabel, x => x.MapFrom(s => s.StatusCode.ToUpperInvariant()));

                builder.CreateMap<OnlineOrderSource, OnlineOrderDto>()
                    .IncludeBase<BaseOrderSource, BaseOrderDto>();
            }
        }

        public sealed class BaseDispatchProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<BaseDispatchOrderSource, BaseDispatchOrderDto>()
                    .ForMember(x => x.StatusLabel, x => x.MapFrom(s => s.StatusCode.ToUpperInvariant()));
            }
        }

        public class BaseOrderSource
        {
            public string StatusCode { get; set; }
        }

        public sealed class OnlineOrderSource : BaseOrderSource
        {
            public string Channel { get; set; }
        }

        public class BaseOrderDto
        {
            public string StatusLabel { get; set; }
        }

        public sealed class OnlineOrderDto : BaseOrderDto
        {
            public string Channel { get; set; }
        }

        public class BaseDispatchOrderSource
        {
            public string StatusCode { get; set; }
        }

        public sealed class DerivedDispatchOrderSource : BaseDispatchOrderSource
        {
            public string Channel { get; set; }
        }

        public sealed class BaseDispatchOrderDto
        {
            public string StatusLabel { get; set; }
        }
    }
}
