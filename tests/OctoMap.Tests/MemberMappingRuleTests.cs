using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class MemberMappingRuleTests
    {
        [Fact]
        public void MapFrom_Uses_Configured_Source_Expression()
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

        [Fact]
        public void MapFrom_Uses_Arithmetic_Logical_And_Coalesce_Expressions()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(ExpressionRuleProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ExpressionRuleSource, ExpressionRuleDestination>(new ExpressionRuleSource
            {
                UnitPrice = 12.50m,
                Quantity = 4,
                Discount = 5m,
                IsActive = true,
                IsDeleted = false,
                DisplayName = null,
                FallbackName = "Fallback"
            });

            Assert.Equal(45m, destination.Total);
            Assert.Equal(0, destination.QuantityRemainder);
            Assert.True(destination.CanShip);
            Assert.Equal("Fallback", destination.Name);
        }

        [Fact]
        public void MapFrom_Uses_Instance_And_Static_Method_Calls()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(MethodCallRuleProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<MethodCallSource, MethodCallDestination>(new MethodCallSource
            {
                Name = "  ada lovelace  ",
                Prefix = "ORD",
                Number = 42,
                Amount = 12.345m
            });

            Assert.Equal("ADA LOVELACE", destination.NormalizedName);
            Assert.Equal("ORD-42", destination.Code);
            Assert.Equal(12.34m, destination.RoundedAmount);
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

        public sealed class ExpressionRuleProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ExpressionRuleSource, ExpressionRuleDestination>()
                    .ForMember(x => x.Total, x => x.MapFrom(s => (s.UnitPrice * s.Quantity) - s.Discount))
                    .ForMember(x => x.QuantityRemainder, x => x.MapFrom(s => s.Quantity % 2))
                    .ForMember(x => x.CanShip, x => x.MapFrom(s => s.IsActive && s.Quantity > 0 && !s.IsDeleted))
                    .ForMember(x => x.Name, x => x.MapFrom(s => s.DisplayName ?? s.FallbackName ?? "Unknown"));
            }
        }

        public sealed class MethodCallRuleProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<MethodCallSource, MethodCallDestination>()
                    .ForMember(x => x.NormalizedName, x => x.MapFrom(s => s.Name.Trim().ToUpperInvariant()))
                    .ForMember(x => x.Code, x => x.MapFrom(s => BuildCode(s.Prefix, s.Number)))
                    .ForMember(x => x.RoundedAmount, x => x.MapFrom(s => decimal.Round(s.Amount, 2)));
            }
        }

        public static string BuildCode(string prefix, int number)
            => string.Concat(prefix, "-", number);

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

        public sealed class ExpressionRuleSource
        {
            public decimal UnitPrice { get; set; }

            public int Quantity { get; set; }

            public decimal Discount { get; set; }

            public bool IsActive { get; set; }

            public bool IsDeleted { get; set; }

            public string DisplayName { get; set; }

            public string FallbackName { get; set; }
        }

        public sealed class ExpressionRuleDestination
        {
            public decimal Total { get; set; }

            public int QuantityRemainder { get; set; }

            public bool CanShip { get; set; }

            public string Name { get; set; }
        }

        public sealed class MethodCallSource
        {
            public string Name { get; set; }

            public string Prefix { get; set; }

            public int Number { get; set; }

            public decimal Amount { get; set; }
        }

        public sealed class MethodCallDestination
        {
            public string NormalizedName { get; set; }

            public string Code { get; set; }

            public decimal RoundedAmount { get; set; }
        }
    }
}
