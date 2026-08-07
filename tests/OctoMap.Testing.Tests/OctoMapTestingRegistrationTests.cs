using Microsoft.Extensions.DependencyInjection;
using OctoMap.Testing;

namespace OctoMap.Testing.Tests
{
    public sealed class OctoMapTestingRegistrationTests
    {
        [Fact]
        public async Task AddOctoMapTesting_Registers_Testing_Mapper_With_Profile_Scanning()
        {
            var provider = CreateProvider(services => services.AddOctoMapTesting(typeof(CustomerTestingProfile).Assembly));
            var mapper = provider.GetRequiredService<IOctoMapTestingMapper>();

            await mapper.AssertConfigurationIsValidAsync();

            mapper
                .ShouldHaveProfile<CustomerTestingProfile>()
                .ShouldHaveMap<Customer, CustomerResponse>();
        }

        [Fact]
        public void AddOctoMapTestingAdapter_Registers_Adapter_Contract()
        {
            var provider = CreateProvider(services => services.AddOctoMapTestingAdapter(typeof(CustomerTestingProfile).Assembly));

            var adapter = provider.GetRequiredService<IOctoMapTestingAdapter>();
            var mapper = provider.GetRequiredService<IOctoMapTestingMapper>();

            Assert.Same(adapter, mapper);
        }

        [Fact]
        public void ShouldHaveMap_Throws_With_Configuration_Diagnostics_When_Map_Is_Missing()
        {
            var provider = CreateProvider(services => services.AddOctoMapTesting(typeof(CustomerTestingProfile).Assembly));
            var mapper = provider.GetRequiredService<IOctoMapTestingMapper>();

            var exception = Assert.Throws<OctoMapTestingAssertionException>(() => mapper.ShouldHaveMap<MissingSource, CustomerResponse>());

            Assert.Contains("MissingSource", exception.Message);
            Assert.Contains("Configuration:", exception.Message);
        }

        private static ServiceProvider CreateProvider(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();
            configure(services);
            return services.BuildServiceProvider();
        }
    }
}
