namespace OctoMap.Testing.Tests
{
    public sealed class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Address Address { get; set; }

        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    }

    public sealed class CustomerResponse
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public AddressResponse Address { get; set; }

        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    }

    public sealed class CustomerSummary
    {
        public string DisplayName { get; set; }
    }

    public sealed class CustomerProjection
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public sealed class Address
    {
        public string City { get; set; }
    }

    public sealed class AddressResponse
    {
        public string City { get; set; }
    }

    public sealed class MissingSource
    {
        public string Name { get; set; }
    }
}
