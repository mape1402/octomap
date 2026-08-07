using OctoMap.Testing;

namespace OctoMap.Testing.Tests
{
    public sealed class MappingAssertionTests
    {
        [Fact]
        public void Matching_Throws_When_Destination_Value_Does_Not_Match_Source()
        {
            var source = new Customer { Id = 1, Name = "Ada" };
            var destination = new CustomerResponse { Id = 1, Name = "Grace" };

            var exception = Assert.Throws<OctoMapTestingAssertionException>(() =>
                destination.ShouldMapFrom(source).Matching(x => x.Name));

            Assert.Contains("Name", exception.Message);
            Assert.Contains("Ada", exception.Message);
            Assert.Contains("Grace", exception.Message);
        }

        [Fact]
        public void Matching_Uses_Explicit_Source_And_Destination_Members()
        {
            var source = new Customer { Name = "Ada" };
            var destination = new CustomerSummary { DisplayName = "Ada" };

            destination.ShouldMapFrom(source)
                .Matching(x => x.Name, x => x.DisplayName);
        }
    }
}
