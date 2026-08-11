using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class ValueMappingRuleTests
    {
        [Fact]
        public void UseValue_Assigns_Configured_Constant_Value()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(ValueRuleProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, OrderDto>(new Order { Id = 9 });

            Assert.Equal(9, destination.Id);
            Assert.Equal("Created", destination.Status);
        }

        [Fact]
        public void NullSubstitute_Replaces_Null_Source_Value()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(ValueRuleProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, OrderDto>(new Order { Id = 10, Description = null });

            Assert.Equal("No description", destination.Description);
        }

        [Fact]
        public void UseValue_Assigns_DateTime_DateTimeOffset_Guid_And_Nullable_Constants()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(ComplexValueRuleProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ComplexValueSource, ComplexValueDestination>(new ComplexValueSource());

            Assert.Equal(ComplexValueRuleProfile.CreatedAt, destination.CreatedAt);
            Assert.Equal(ComplexValueRuleProfile.ObservedAt, destination.ObservedAt);
            Assert.Equal(ComplexValueRuleProfile.CorrelationId, destination.CorrelationId);
            Assert.Equal(21, destination.OptionalCount);
        }

        public sealed class ValueRuleProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<Order, OrderDto>()
                    .ForMember(x => x.Status, x => x.UseValue("Created"))
                    .ForMember(x => x.Description, x => x.NullSubstitute("No description"));
            }
        }

        public sealed class ComplexValueRuleProfile : OctoMapProfile
        {
            public static readonly DateTime CreatedAt = new(2026, 8, 11, 14, 30, 0, DateTimeKind.Utc);

            public static readonly DateTimeOffset ObservedAt = new(2026, 8, 11, 8, 30, 0, TimeSpan.FromHours(-6));

            public static readonly Guid CorrelationId = Guid.Parse("be4f1dd6-04f5-4a43-ae91-58d0cc19f93a");

            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ComplexValueSource, ComplexValueDestination>()
                    .ForMember(x => x.CreatedAt, x => x.UseValue(CreatedAt))
                    .ForMember(x => x.ObservedAt, x => x.UseValue(ObservedAt))
                    .ForMember(x => x.CorrelationId, x => x.UseValue(CorrelationId))
                    .ForMember(x => x.OptionalCount, x => x.UseValue((int?)21));
            }
        }

        public sealed class Order
        {
            public int Id { get; set; }

            public string Description { get; set; }
        }

        public sealed class OrderDto
        {
            public int Id { get; set; }

            public string Status { get; set; }

            public string Description { get; set; }
        }

        public sealed class ComplexValueSource
        {
        }

        public sealed class ComplexValueDestination
        {
            public DateTime CreatedAt { get; set; }

            public DateTimeOffset ObservedAt { get; set; }

            public Guid CorrelationId { get; set; }

            public int? OptionalCount { get; set; }
        }
    }
}
