using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class ConditionalMappingTests
    {
        [Fact]
        public void Map_Skips_Member_When_PreCondition_Is_False()
        {
            var provider = CreateProvider<ConditionalProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ConditionalOrder, ConditionalOrderDto>(new ConditionalOrder
            {
                Status = "Cancelled",
                Description = "Should not map"
            });

            Assert.Null(destination.Description);
        }

        [Fact]
        public void Map_Skips_Member_When_Value_Condition_Is_False()
        {
            var provider = CreateProvider<ConditionalProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ConditionalOrder, ConditionalOrderDto>(new ConditionalOrder
            {
                Status = "Created",
                Code = "skip"
            });

            Assert.Null(destination.Code);
        }

        [Fact]
        public void Map_Assigns_Member_When_Conditions_Are_True()
        {
            var provider = CreateProvider<ConditionalProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ConditionalOrder, ConditionalOrderDto>(new ConditionalOrder
            {
                Status = "Created",
                Description = "Mapped",
                Code = "A-100"
            });

            Assert.Equal("Mapped", destination.Description);
            Assert.Equal("A-100", destination.Code);
        }

        [Fact]
        public void Map_Does_Not_Invoke_Resolver_When_PreCondition_Is_False()
        {
            var counter = new ConditionalResolverCounter();
            var services = new ServiceCollection();
            services.AddSingleton(counter);
            services.AddTransient<ConditionalLabelResolver>();
            services.AddOctoMap(typeof(ResolverConditionalProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ConditionalOrder, ConditionalOrderDto>(new ConditionalOrder
            {
                Status = "Cancelled"
            });

            Assert.Null(destination.Label);
            Assert.Equal(0, counter.Count);
        }

        [Fact]
        public void Map_Applies_Condition_To_Destination_Path()
        {
            var provider = CreateProvider<ConditionalPathProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var skipped = mapper.Map<ConditionalOrder, ConditionalOrderEntity>(new ConditionalOrder
            {
                CustomerName = "x"
            });
            var assigned = mapper.Map<ConditionalOrder, ConditionalOrderEntity>(new ConditionalOrder
            {
                CustomerName = "Ada"
            });

            Assert.Null(skipped.Customer);
            Assert.NotNull(assigned.Customer);
            Assert.Equal("Ada", assigned.Customer.Name);
        }

        [Fact]
        public void ProjectTo_Throws_When_Map_Uses_Condition()
        {
            var provider = CreateProvider<ConditionalProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var exception = Assert.Throws<NotSupportedException>(() =>
                Array.Empty<ConditionalOrder>().AsQueryable().ProjectTo<ConditionalOrderDto>(mapper.ProjectionBuilder).ToArray());

            Assert.Contains("conditional mapping", exception.Message);
        }

        [Fact]
        public void DescribeMap_Includes_Conditional_Mapping_Details()
        {
            var provider = CreateProvider<ConditionalProfile>();
            var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

            var description = configuration.DescribeMap<ConditionalOrder, ConditionalOrderDto>();

            Assert.Contains("pre:", description);
            Assert.Contains("condition:", description);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ConditionalResolverCounter>();
            services.AddTransient<ConditionalLabelResolver>();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class ConditionalProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ConditionalOrder, ConditionalOrderDto>()
                    .ForMember(x => x.Description, x =>
                    {
                        x.MapFrom(s => s.Description);
                        x.PreCondition(s => s.Status != "Cancelled");
                    })
                    .ForMember(x => x.Code, x =>
                    {
                        x.MapFrom(s => s.Code);
                        x.Condition((source, value) => value != "skip");
                    });
            }
        }

        public sealed class ResolverConditionalProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ConditionalOrder, ConditionalOrderDto>()
                    .ForMember(x => x.Label, x =>
                    {
                        x.ResolveUsing<ConditionalLabelResolver>();
                        x.PreCondition(s => s.Status != "Cancelled");
                    });
            }
        }

        public sealed class ConditionalPathProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ConditionalOrder, ConditionalOrderEntity>()
                    .ForPath(x => x.Customer.Name, x =>
                    {
                        x.MapFrom(s => s.CustomerName);
                        x.Condition((source, value) => value.Length > 1);
                    });
            }
        }

        public sealed class ConditionalOrder
        {
            public string Status { get; set; }

            public string Description { get; set; }

            public string Code { get; set; }

            public string CustomerName { get; set; }
        }

        public sealed class ConditionalOrderDto
        {
            public string Description { get; set; }

            public string Code { get; set; }

            public string Label { get; set; }
        }

        public sealed class ConditionalOrderEntity
        {
            public ConditionalCustomer Customer { get; set; }
        }

        public sealed class ConditionalCustomer
        {
            public string Name { get; set; }
        }

        public sealed class ConditionalResolverCounter
        {
            public int Count { get; set; }
        }

        public sealed class ConditionalLabelResolver : IValueResolver<ConditionalOrder, ConditionalOrderDto, string>
        {
            private readonly ConditionalResolverCounter _counter;

            public ConditionalLabelResolver(ConditionalResolverCounter counter)
            {
                _counter = counter;
            }

            public string Resolve(ConditionalOrder source, ConditionalOrderDto destination, IMapContext context)
            {
                _counter.Count++;
                return source.Status;
            }
        }
    }
}
