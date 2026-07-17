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
        public void Map_Normalizes_Enumerable_Source_And_Assigns_List_To_Interface_Destination()
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
                typeof(ConfiguredCollectionProfile).Assembly);
            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, OrderDto>(new Order
            {
                Items = null
            });

            Assert.NotNull(destination.Items);
            Assert.Empty(destination.Items);
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

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
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

        public sealed class Order
        {
            public List<OrderItem> Items { get; set; }
        }

        public sealed class OrderDto
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
    }
}
