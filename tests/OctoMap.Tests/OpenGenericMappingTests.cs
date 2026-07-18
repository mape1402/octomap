using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class OpenGenericMappingTests
    {
        [Fact]
        public void Map_Closes_Open_Generic_Map_At_Runtime()
        {
            var provider = CreateProvider<OpenGenericProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Box<OpenGenericCustomer>, BoxDto<OpenGenericCustomerDto>>(new Box<OpenGenericCustomer>
            {
                Value = new OpenGenericCustomer
                {
                    Name = "Ada"
                }
            });

            Assert.NotNull(destination.Value);
            Assert.Equal("Ada", destination.Value.Name);
        }

        [Fact]
        public void Map_Closes_Open_Generic_Collection_Map_At_Runtime()
        {
            var provider = CreateProvider<OpenGenericProfile>();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Page<OpenGenericCustomer>, PageDto<OpenGenericCustomerDto>>(new Page<OpenGenericCustomer>
            {
                Items = new List<OpenGenericCustomer>
                {
                    new() { Name = "Grace" }
                }
            });

            Assert.Single(destination.Items);
            Assert.Equal("Grace", destination.Items[0].Name);
        }

        private static ServiceProvider CreateProvider<TProfile>()
            where TProfile : OctoMapProfile, new()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(TProfile).Assembly);
            return services.BuildServiceProvider();
        }

        public sealed class OpenGenericProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap(typeof(Box<>), typeof(BoxDto<>));
                builder.CreateMap(typeof(Page<>), typeof(PageDto<>));
            }
        }

        public sealed class Box<T>
        {
            public T Value { get; set; }
        }

        public sealed class BoxDto<T>
        {
            public T Value { get; set; }
        }

        public sealed class Page<T>
        {
            public List<T> Items { get; set; }
        }

        public sealed class PageDto<T>
        {
            public List<T> Items { get; set; }
        }

        public sealed class OpenGenericCustomer
        {
            public string Name { get; set; }
        }

        public sealed class OpenGenericCustomerDto
        {
            public string Name { get; set; }
        }
    }
}
