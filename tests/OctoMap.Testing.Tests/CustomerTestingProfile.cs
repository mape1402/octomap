namespace OctoMap.Testing.Tests
{
    public sealed class CustomerTestingProfile : OctoMapProfile
    {
        public override void Configure(IOctoMapConfigurationBuilder builder)
        {
            builder.CreateMap<Customer, CustomerResponse>();
            builder.CreateMap<Customer, CustomerProjection>();
            builder.CreateMap<Address, AddressResponse>();
        }
    }
}
