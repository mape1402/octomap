using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class Phase2MemberRuleTests
    {
        [Fact]
        public void MapFrom_Maps_Destination_Member_From_Source_Expression()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(MemberRuleProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Person, PersonDto>(new Person
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                Age = 36
            });

            Assert.Equal("Ada Lovelace", destination.FullName);
            Assert.Equal(36, destination.Age);
        }

        [Fact]
        public void Ignore_Skips_Destination_Member()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(MemberRuleProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Person, PersonDto>(new Person
            {
                FirstName = "Grace",
                LastName = "Hopper",
                Age = 85,
                InternalCode = "SECRET"
            });

            Assert.Null(destination.InternalCode);
        }

        public sealed class MemberRuleProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<Person, PersonDto>()
                    .ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName))
                    .ForMember(x => x.InternalCode, x => x.Ignore());
            }
        }

        public sealed class Person
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public int Age { get; set; }

            public string InternalCode { get; set; }
        }

        public sealed class PersonDto
        {
            public string FullName { get; set; }

            public int Age { get; set; }

            public string InternalCode { get; set; }
        }
    }
}
