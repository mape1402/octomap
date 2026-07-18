using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class NullSourceValueMappingTests
    {
        [Fact]
        public void Map_Preserves_Existing_Destination_Value_When_Global_Ignore_Null_Source_Values_Is_Enabled()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(
                options => options.IgnoreNullSourceValues = true,
                typeof(NullSourceValueProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var destination = new GlobalPatchOrderDto
            {
                Description = "keep"
            };

            var result = mapper.Map(new GlobalPatchOrder
            {
                Description = null
            }, destination);

            Assert.Same(destination, result);
            Assert.Equal("keep", result.Description);
        }

        [Fact]
        public void Map_Assigns_Non_Null_Source_Value_When_Global_Ignore_Null_Source_Values_Is_Enabled()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(
                options => options.IgnoreNullSourceValues = true,
                typeof(NullSourceValueProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var destination = new GlobalPatchOrderDto
            {
                Description = "old"
            };

            var result = mapper.Map(new GlobalPatchOrder
            {
                Description = "new"
            }, destination);

            Assert.Equal("new", result.Description);
        }

        [Fact]
        public void Map_Uses_Member_Override_To_Ignore_Null_Source_Value()
        {
            var provider = CreateProvider<MemberIgnoreNullSourceValueProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var destination = new MemberIgnorePatchOrderDto
            {
                Description = "keep"
            };

            var result = mapper.Map(new MemberIgnorePatchOrder
            {
                Description = null
            }, destination);

            Assert.Equal("keep", result.Description);
        }

        [Fact]
        public void Map_Uses_Member_Override_To_Assign_Null_When_Global_Ignore_Null_Source_Values_Is_Enabled()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(
                options => options.IgnoreNullSourceValues = true,
                typeof(MemberAssignNullSourceValueProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var destination = new MemberAssignPatchOrderDto
            {
                Description = "clear"
            };

            var result = mapper.Map(new MemberAssignPatchOrder
            {
                Description = null
            }, destination);

            Assert.Null(result.Description);
        }

        [Fact]
        public void Map_Assigns_Null_Substitute_When_Ignore_Null_Source_Values_Is_Enabled()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(
                options => options.IgnoreNullSourceValues = true,
                typeof(NullSubstituteWithIgnoreNullSourceValueProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var destination = new SubstitutePatchOrderDto
            {
                Description = "old"
            };

            var result = mapper.Map(new SubstitutePatchOrder
            {
                Description = null
            }, destination);

            Assert.Equal("No description", result.Description);
        }

        [Fact]
        public void DescribeMap_Includes_Ignore_Null_Source_Value_Detail()
        {
            var provider = CreateProvider<MemberIgnoreNullSourceValueProfile>();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var description = configuration.DescribeMap<MemberIgnorePatchOrder, MemberIgnorePatchOrderDto>();

            Assert.Contains("ignore-null", description);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class NullSourceValueProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<GlobalPatchOrder, GlobalPatchOrderDto>();
            }
        }

        public sealed class MemberIgnoreNullSourceValueProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<MemberIgnorePatchOrder, MemberIgnorePatchOrderDto>()
                    .ForMember(x => x.Description, x => x.IgnoreNullSourceValue());
            }
        }

        public sealed class MemberAssignNullSourceValueProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<MemberAssignPatchOrder, MemberAssignPatchOrderDto>()
                    .ForMember(x => x.Description, x => x.IgnoreNullSourceValue(false));
            }
        }

        public sealed class NullSubstituteWithIgnoreNullSourceValueProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<SubstitutePatchOrder, SubstitutePatchOrderDto>()
                    .ForMember(x => x.Description, x => x.NullSubstitute("No description"));
            }
        }

        public sealed class GlobalPatchOrder
        {
            public string Description { get; set; }
        }

        public sealed class GlobalPatchOrderDto
        {
            public string Description { get; set; }
        }

        public sealed class MemberIgnorePatchOrder
        {
            public string Description { get; set; }
        }

        public sealed class MemberIgnorePatchOrderDto
        {
            public string Description { get; set; }
        }

        public sealed class MemberAssignPatchOrder
        {
            public string Description { get; set; }
        }

        public sealed class MemberAssignPatchOrderDto
        {
            public string Description { get; set; }
        }

        public sealed class SubstitutePatchOrder
        {
            public string Description { get; set; }
        }

        public sealed class SubstitutePatchOrderDto
        {
            public string Description { get; set; }
        }
    }
}
