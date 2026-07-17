using Microsoft.Extensions.DependencyInjection;
using OctoMap;
using OctoMap.Samples.Basic;

var services = new ServiceCollection();

services.AddSingleton<SampleRunner>();
services.AddSingleton<ICurrencyFormatter, CurrencyFormatter>();
services.AddSingleton<IOrderLabelFormatter, OrderLabelFormatter>();
services.AddSingleton<IOrderStatusCatalog, OrderStatusCatalog>();
services.AddTransient<OrderStatusLabelResolver>();
services.AddTransient<OrderTotalTextConverter>();
services.AddOctoMap(
    options => options.EnableRuntimeImplicitMaps = true,
    typeof(SalesProfile).Assembly);

var provider = services.BuildServiceProvider();
provider.GetRequiredService<SampleRunner>().Run();
