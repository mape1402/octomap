using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class Phase2InterfaceMapTests
    {
        [Fact]
        public void IMapFrom_Registers_Map_By_Convention()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(CustomerFromDto).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<CustomerFromDto>(new CustomerFrom { Name = "Ada" });

            Assert.Equal("Ada", destination.Name);
        }

        [Fact]
        public void IMapTo_Registers_Map_By_Convention()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(typeof(CustomerTo).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<CustomerTo, CustomerToDto>(new CustomerTo { Name = "Grace" });

            Assert.Equal("Grace", destination.Name);
        }

        public sealed class CustomerFrom
        {
            public string Name { get; set; }
        }

        public sealed class CustomerFromDto : IMapFrom<CustomerFrom>
        {
            public string Name { get; set; }
        }

        public sealed class CustomerTo : IMapTo<CustomerToDto>
        {
            public string Name { get; set; }
        }

        public sealed class CustomerToDto
        {
            public string Name { get; set; }
        }
    }
}
