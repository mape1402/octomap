using Microsoft.Extensions.DependencyInjection;
using OctoMap;
using OctoMap.Samples.Basic;

var services = new ServiceCollection();

services.AddSingleton<SampleRunner>();
services.AddOctoMap(
    options => options.EnableRuntimeImplicitMaps = true,
    typeof(SalesProfile).Assembly);

var provider = services.BuildServiceProvider();
provider.GetRequiredService<SampleRunner>().Run();
