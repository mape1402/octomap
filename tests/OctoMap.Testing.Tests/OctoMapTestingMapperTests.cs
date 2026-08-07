using Microsoft.Extensions.DependencyInjection;
using OctoMap.Testing;

namespace OctoMap.Testing.Tests
{
    public sealed class OctoMapTestingMapperTests
    {
        [Fact]
        public async Task MapAsync_Maps_Source_To_Destination()
        {
            var mapper = CreateMapper();
            var customer = new Customer
            {
                Id = 7,
                Name = "Ada",
                Address = new Address { City = "London" },
                Tags = new[] { "vip", "active" }
            };

            var response = await mapper.MapAsync<CustomerResponse>(customer);

            response.ShouldMapFrom(customer)
                .Matching(x => x.Id)
                .Matching(x => x.Name)
                .Matching(x => x.Address.City)
                .Matching(x => x.Tags);
        }

        [Fact]
        public async Task MapAsync_Updates_Existing_Destination()
        {
            var mapper = CreateMapper();
            var customer = new Customer { Id = 11, Name = "Grace", Address = new Address { City = "Arlington" } };
            var existing = new CustomerResponse { Name = "Before" };

            var result = await mapper.MapAsync(customer, existing);

            Assert.Same(existing, result);
            Assert.Equal(11, existing.Id);
            Assert.Equal("Grace", existing.Name);
            Assert.Equal("Arlington", existing.Address.City);
        }

        [Fact]
        public void ShouldProjectTo_Asserts_Queryable_Projection()
        {
            var mapper = CreateMapper();
            var customers = new[]
            {
                new Customer { Id = 19, Name = "Katherine", Address = new Address { City = "White Sulphur Springs" } }
            }.AsQueryable();

            var projected = customers.ShouldProjectTo<Customer, CustomerProjection>(mapper).Single();

            Assert.Equal(19, projected.Id);
            Assert.Equal("Katherine", projected.Name);
        }

        private static IOctoMapTestingMapper CreateMapper()
        {
            var services = new ServiceCollection();
            services.AddOctoMapTesting(typeof(CustomerTestingProfile).Assembly);
            return services.BuildServiceProvider().GetRequiredService<IOctoMapTestingMapper>();
        }
    }
}
