using Microsoft.Extensions.DependencyInjection;

namespace OctoMap.Tests
{
    public class ValueConverterMappingTests
    {
        [Fact]
        public void ConvertUsing_Resolves_Value_Converter_From_ServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton<CurrencyFormatter>();
            services.AddTransient<TotalTextConverter>();
            services.AddOctoMap(typeof(ConverterProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            var destination = mapper.Map<Order, OrderDto>(new Order { Total = 18.75m });

            Assert.Equal("18.75 USD", destination.TotalText);
        }

        [Fact]
        public void ConvertUsing_Resolves_Value_Converter_On_Each_Map_Call()
        {
            ConverterConstructionCount = 0;
            var services = new ServiceCollection();
            services.AddSingleton<CurrencyFormatter>();
            services.AddTransient<TotalTextConverter>();
            services.AddOctoMap(typeof(ConverterProfile).Assembly);

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IOctoMapper>();

            mapper.Map<Order, OrderDto>(new Order { Total = 10m });
            mapper.Map<Order, OrderDto>(new Order { Total = 20m });

            Assert.Equal(2, ConverterConstructionCount);
        }

        public static int ConverterConstructionCount { get; private set; }

        public sealed class ConverterProfile : OctoMapProfile
        {
            public override void Configure(IOctoMapConfigurationBuilder builder)
            {
                builder.CreateMap<Order, OrderDto>()
                    .ForMember(x => x.TotalText, x => x.ConvertUsing<TotalTextConverter>(s => s.Total));
            }
        }

        public sealed class TotalTextConverter : IValueConverter<decimal, string>
        {
            private readonly CurrencyFormatter _formatter;

            public TotalTextConverter(CurrencyFormatter formatter)
            {
                ConverterConstructionCount++;
                _formatter = formatter;
            }

            public string Convert(decimal sourceMember, IMapContext context)
                => _formatter.Format(sourceMember);
        }

        public sealed class CurrencyFormatter
        {
            public string Format(decimal value)
                => $"{value:0.00} USD";
        }

        public sealed class Order
        {
            public decimal Total { get; set; }
        }

        public sealed class OrderDto
        {
            public string TotalText { get; set; }
        }
    }
}
