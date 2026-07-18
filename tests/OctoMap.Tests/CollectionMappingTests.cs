using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class CollectionMappingTests
    {
        [Fact]
        public void Map_Uses_Configured_Item_Map_For_List_Member()
        {
            var provider = CreateProvider<ConfiguredCollectionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, OrderDto>(new Order
            {
                Items = new List<OrderItem>
                {
                    new() { Sku = "A-1", Quantity = 2 },
                    new() { Sku = "B-2", Quantity = 5 }
                }
            });

            Assert.NotNull(destination.Items);
            Assert.Equal(2, destination.Items.Count);
            Assert.Equal("A-1 x 2", destination.Items[0].Label);
            Assert.Equal("B-2 x 5", destination.Items[1].Label);
        }

        [Fact]
        public void Map_Creates_Implicit_Item_Map_For_Array_Member()
        {
            var provider = CreateProvider<ImplicitCollectionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<ArrayOrder, ArrayOrderDto>(new ArrayOrder
            {
                Items = new[]
                {
                    new ImplicitOrderItem { Sku = "A-1", Quantity = 2 },
                    new ImplicitOrderItem { Sku = "B-2", Quantity = 5 }
                }
            });

            Assert.NotNull(destination.Items);
            Assert.Equal(2, destination.Items.Length);
            Assert.Equal("A-1", destination.Items[0].Sku);
            Assert.Equal(5, destination.Items[1].Quantity);
        }

        [Fact]
        public void Map_Assigns_Default_When_Source_Collection_Is_Null()
        {
            var provider = CreateProvider<ConfiguredCollectionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, OrderDto>(new Order
            {
                Items = null
            });

            Assert.Null(destination.Items);
        }

        [Fact]
        public void Map_Copies_Assignable_List_Items()
        {
            var provider = CreateProvider<AssignableCollectionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<TagSource, TagDestination>(new TagSource
            {
                Tags = new List<string> { "new", "paid" }
            });

            Assert.NotNull(destination.Tags);
            Assert.Equal(new[] { "new", "paid" }, destination.Tags);
            Assert.NotSame(destination.Tags, mapper.Map<TagSource, TagDestination>(new TagSource { Tags = destination.Tags }).Tags);
        }

        [Fact]
        public void Map_Enumerates_Enumerable_Source_And_Assigns_List_To_Interface_Destination()
        {
            var provider = CreateProvider<InterfaceCollectionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<InterfaceOrder, InterfaceOrderDto>(new InterfaceOrder
            {
                Items = new[]
                {
                    new ImplicitOrderItem { Sku = "A-1", Quantity = 2 },
                    new ImplicitOrderItem { Sku = "B-2", Quantity = 5 }
                }
            });

            Assert.NotNull(destination.Items);
            Assert.Equal(2, destination.Items.Count);
            Assert.Equal("A-1", destination.Items[0].Sku);
            Assert.Equal(5, destination.Items[1].Quantity);
        }

        [Fact]
        public void Map_Assigns_Empty_Collection_When_Source_Is_Null_And_Null_Collections_Are_Disabled()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(
                options => options.AllowNullCollections = false,
                typeof(NullCollectionOptionProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<NullCollectionOptionOrder, NullCollectionOptionOrderDto>(new NullCollectionOptionOrder
            {
                Items = null
            });

            Assert.NotNull(destination.Items);
            Assert.Empty(destination.Items);
        }

        [Fact]
        public void Map_Uses_Member_Rule_To_Assign_Empty_Collection_When_Source_Is_Null()
        {
            var provider = CreateProvider<EmptyCollectionMemberProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<EmptyCollectionMemberOrder, EmptyCollectionMemberOrderDto>(new EmptyCollectionMemberOrder
            {
                Items = null
            });

            Assert.NotNull(destination.Items);
            Assert.Empty(destination.Items);
        }

        [Fact]
        public void Map_Uses_Member_Rule_To_Preserve_Null_Collection_When_Global_Null_Collections_Are_Disabled()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(
                options => options.AllowNullCollections = false,
                typeof(AllowNullCollectionMemberProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<AllowNullCollectionMemberOrder, AllowNullCollectionMemberOrderDto>(new AllowNullCollectionMemberOrder
            {
                Items = null
            });

            Assert.Null(destination.Items);
        }

        [Fact]
        public void Map_Throws_When_Item_Map_Is_Not_Configured_And_Implicit_Maps_Are_Disabled()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(
                options => options.EnableRuntimeImplicitMaps = false,
                typeof(ImplicitCollectionProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var exception = Assert.Throws<InvalidOperationException>(() => mapper.Map<ArrayOrder, ArrayOrderDto>(new ArrayOrder
            {
                Items = new[] { new ImplicitOrderItem() }
            }));

            Assert.Contains("runtime implicit maps are disabled", exception.Message);
        }

        [Fact]
        public void Map_Uses_Global_Type_Conversion_For_Collection_Items()
        {
            var provider = CreateProvider<CollectionItemConversionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<CollectionItemConversionSource, CollectionItemConversionDestination>(new CollectionItemConversionSource
            {
                Codes = new List<string> { "a-1", "b-2" }
            });

            Assert.Equal(new[] { "A-1", "B-2" }, destination.Codes.Select(x => x.Value));
        }

        [Fact]
        public void Map_Resolves_Global_Service_Converter_For_Collection_Items()
        {
            var provider = CreateProvider<CollectionServiceConverterProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<CollectionAmountSource, CollectionAmountDestination>(new CollectionAmountSource
            {
                Amounts = new List<decimal> { 12.3m, 45.67m }
            });

            Assert.Equal(new[] { "$12.30", "$45.67" }, destination.Amounts.Select(x => x.Value));
        }

        [Fact]
        public void Map_Assigns_HashSet_Destination()
        {
            var provider = CreateProvider<SetCollectionProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<SetCollectionSource, SetCollectionDestination>(new SetCollectionSource
            {
                Tags = new List<string> { "paid", "paid", "new" }
            });

            Assert.NotNull(destination.Tags);
            Assert.Equal(new[] { "new", "paid" }, destination.Tags.OrderBy(x => x));
        }

        [Fact]
        public void Map_Preserves_Existing_Collection_When_Source_Is_Null_And_Null_Source_Values_Are_Ignored()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(
                options => options.IgnoreNullSourceValues = true,
                typeof(NullCollectionOptionProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();
            var existingItems = new List<OrderItemDto> { new() { Label = "existing" } };
            var destination = new NullCollectionOptionOrderDto
            {
                Items = existingItems
            };

            var result = mapper.Map<NullCollectionOptionOrder, NullCollectionOptionOrderDto>(
                new NullCollectionOptionOrder { Items = null },
                destination);

            Assert.Same(destination, result);
            Assert.Same(existingItems, result.Items);
            Assert.Equal("existing", result.Items[0].Label);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddTransient<CollectionAmountTextConverter>();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class ConfiguredCollectionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<Order, OrderDto>();
                builder.CreateMap<OrderItem, OrderItemDto>()
                    .ForMember(x => x.Label, x => x.MapFrom(s => s.Sku + " x " + s.Quantity));
            }
        }

        public sealed class ImplicitCollectionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<ArrayOrder, ArrayOrderDto>();
            }
        }

        public sealed class AssignableCollectionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<TagSource, TagDestination>();
            }
        }

        public sealed class InterfaceCollectionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<InterfaceOrder, InterfaceOrderDto>();
            }
        }

        public sealed class EmptyCollectionMemberProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<EmptyCollectionMemberOrder, EmptyCollectionMemberOrderDto>()
                    .ForMember(x => x.Items, x => x.UseEmptyCollectionWhenNull());
                builder.CreateMap<OrderItem, OrderItemDto>()
                    .ForMember(x => x.Label, x => x.MapFrom(s => s.Sku + " x " + s.Quantity));
            }
        }

        public sealed class AllowNullCollectionMemberProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<AllowNullCollectionMemberOrder, AllowNullCollectionMemberOrderDto>()
                    .ForMember(x => x.Items, x => x.AllowNullCollection(true));
                builder.CreateMap<OrderItem, OrderItemDto>()
                    .ForMember(x => x.Label, x => x.MapFrom(s => s.Sku + " x " + s.Quantity));
            }
        }

        public sealed class NullCollectionOptionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<NullCollectionOptionOrder, NullCollectionOptionOrderDto>();
                builder.CreateMap<OrderItem, OrderItemDto>()
                    .ForMember(x => x.Label, x => x.MapFrom(s => s.Sku + " x " + s.Quantity));
            }
        }

        public sealed class CollectionItemConversionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateConverter<string, CollectionSkuCode>(x => new CollectionSkuCode(x.ToUpperInvariant()));
                builder.CreateMap<CollectionItemConversionSource, CollectionItemConversionDestination>();
            }
        }

        public sealed class CollectionServiceConverterProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateConverter<CollectionAmountTextConverter, decimal, CollectionAmountText>();
                builder.CreateMap<CollectionAmountSource, CollectionAmountDestination>();
            }
        }

        public sealed class SetCollectionProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<SetCollectionSource, SetCollectionDestination>();
            }
        }

        public sealed class Order
        {
            public List<OrderItem> Items { get; set; }
        }

        public sealed class OrderDto
        {
            public List<OrderItemDto> Items { get; set; }
        }

        public sealed class EmptyCollectionMemberOrder
        {
            public List<OrderItem> Items { get; set; }
        }

        public sealed class EmptyCollectionMemberOrderDto
        {
            public List<OrderItemDto> Items { get; set; }
        }

        public sealed class AllowNullCollectionMemberOrder
        {
            public List<OrderItem> Items { get; set; }
        }

        public sealed class AllowNullCollectionMemberOrderDto
        {
            public List<OrderItemDto> Items { get; set; }
        }

        public sealed class NullCollectionOptionOrder
        {
            public List<OrderItem> Items { get; set; }
        }

        public sealed class NullCollectionOptionOrderDto
        {
            public List<OrderItemDto> Items { get; set; }
        }

        public sealed class OrderItem
        {
            public string Sku { get; set; }

            public int Quantity { get; set; }
        }

        public sealed class OrderItemDto
        {
            public string Label { get; set; }
        }

        public sealed class ArrayOrder
        {
            public ImplicitOrderItem[] Items { get; set; }
        }

        public sealed class ArrayOrderDto
        {
            public ImplicitOrderItemDto[] Items { get; set; }
        }

        public sealed class ImplicitOrderItem
        {
            public string Sku { get; set; }

            public int Quantity { get; set; }
        }

        public sealed class ImplicitOrderItemDto
        {
            public string Sku { get; set; }

            public int Quantity { get; set; }
        }

        public sealed class TagSource
        {
            public List<string> Tags { get; set; }
        }

        public sealed class TagDestination
        {
            public List<string> Tags { get; set; }
        }

        public sealed class InterfaceOrder
        {
            public IEnumerable<ImplicitOrderItem> Items { get; set; }
        }

        public sealed class InterfaceOrderDto
        {
            public IReadOnlyList<ImplicitOrderItemDto> Items { get; set; }
        }

        public sealed class CollectionItemConversionSource
        {
            public List<string> Codes { get; set; }
        }

        public sealed class CollectionItemConversionDestination
        {
            public List<CollectionSkuCode> Codes { get; set; }
        }

        public sealed class CollectionSkuCode
        {
            public CollectionSkuCode(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        public sealed class CollectionAmountSource
        {
            public List<decimal> Amounts { get; set; }
        }

        public sealed class CollectionAmountDestination
        {
            public List<CollectionAmountText> Amounts { get; set; }
        }

        public sealed record CollectionAmountText(string Value);

        public sealed class CollectionAmountTextConverter : IValueConverter<decimal, CollectionAmountText>
        {
            public CollectionAmountText Convert(decimal source, IMapContext context)
                => new(source.ToString("$0.00"));
        }

        public sealed class SetCollectionSource
        {
            public List<string> Tags { get; set; }
        }

        public sealed class SetCollectionDestination
        {
            public HashSet<string> Tags { get; set; }
        }
    }
}
