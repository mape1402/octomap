using BenchmarkDotNet.Attributes;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OctoMap.Benchmarks
{
    /// <summary>
    /// Measures core runtime mapping scenarios.
    /// </summary>
    [MemoryDiagnoser]
    public class MappingScenarioBenchmarks
    {
        private IOctoMapper _mapper = null!;
        private IOctoMapper<FlatSource, FlatDestination> _typedFlatMapper = null!;
        private IOctoMapper<FlatSource, ResolverDestination> _typedResolverMapper = null!;
        private IOctoMapper<FlatSource, ConverterDestination> _typedConverterMapper = null!;
        private IOctoMapper<FlatSource, SimpleConverterDestination> _typedSimpleConverterMapper = null!;
        private IMapContext _mapContext = null!;
        private IServiceProvider _services = null!;
        private CurrencyTextConverter _directConverter = null!;
        private IncrementConverter _directSimpleConverter = null!;
        private AutoMapper.IMapper _autoMapper = null!;
        private MapperConfiguration _autoMapperConfiguration = null!;
        private TypeAdapterConfig _mapsterConfig = null!;
        private FlatSource _flatSource = null!;
        private NestedSource _nestedSource = null!;
        private CollectionSource _collectionSource = null!;
        private ConstructorSource _constructorSource = null!;
        private FlatteningSource _flatteningSource = null!;
        private IQueryable<FlatSource> _queryable = null!;

        /// <summary>
        /// Initializes benchmark dependencies and warms steady-state maps.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddSingleton<StatusCatalog>();
            services.AddTransient<StatusResolver>();
            services.AddTransient<CurrencyTextConverter>();
            services.AddTransient<IncrementConverter>();
            services.AddOctoMap(registration =>
            {
                registration.Options.EnableRuntimeImplicitMaps = false;
                registration.AddProfile<BenchmarkProfile>();
            });

            var provider = services.BuildServiceProvider();
            _services = provider;
            _mapper = provider.GetRequiredService<IOctoMapper>();
            _typedFlatMapper = provider.GetRequiredService<IOctoMapper<FlatSource, FlatDestination>>();
            _typedResolverMapper = provider.GetRequiredService<IOctoMapper<FlatSource, ResolverDestination>>();
            _typedConverterMapper = provider.GetRequiredService<IOctoMapper<FlatSource, ConverterDestination>>();
            _typedSimpleConverterMapper = provider.GetRequiredService<IOctoMapper<FlatSource, SimpleConverterDestination>>();
            _mapContext = provider.GetRequiredService<IMapContextFactory>().Create();
            _directConverter = new CurrencyTextConverter();
            _directSimpleConverter = new IncrementConverter();
            _mapper.CompileMappings();
            _autoMapperConfiguration = CreateAutoMapperConfiguration();
            _autoMapperConfiguration.CompileMappings();
            _autoMapper = _autoMapperConfiguration.CreateMapper();
            _mapsterConfig = CreateMapsterConfiguration();
            _mapsterConfig.Compile();

            _flatSource = new FlatSource { Id = 42, Name = "Ada", Total = 125.50m, StatusCode = "A" };
            _nestedSource = new NestedSource
            {
                Id = 7,
                Customer = new CustomerSource { FirstName = "Grace", LastName = "Hopper" }
            };
            _collectionSource = new CollectionSource
            {
                Items = Enumerable.Range(1, 10)
                    .Select(x => new ItemSource { Sku = $"SKU-{x}", Quantity = x })
                    .ToArray()
            };
            _constructorSource = new ConstructorSource { Id = 10, Name = "Katherine" };
            _flatteningSource = new FlatteningSource
            {
                Customer = new CustomerSource { FirstName = "Dorothy", LastName = "Vaughan" }
            };
            _queryable = Enumerable.Range(1, 25)
                .Select(x => new FlatSource { Id = x, Name = $"Name {x}", Total = x, StatusCode = "A" })
                .AsQueryable();
        }

        /// <summary>
        /// Measures hand-written mapping for the baseline flat scenario.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark(Baseline = true)]
        public FlatDestination Manual_Flat()
            => new()
            {
                Id = _flatSource.Id,
                Name = _flatSource.Name,
                Total = _flatSource.Total,
                Status = _flatSource.StatusCode
            };

        /// <summary>
        /// Measures a warmed OctoMap flat map.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatDestination OctoMap_Warm_Flat()
            => _mapper.Map<FlatSource, FlatDestination>(_flatSource);

        /// <summary>
        /// Measures a warmed typed OctoMap flat map.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatDestination OctoMap_Typed_Warm_Flat()
            => _typedFlatMapper.Map(_flatSource, _mapContext);

        /// <summary>
        /// Measures a warmed AutoMapper flat map.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatDestination AutoMapper_Warm_Flat()
            => _autoMapper.Map<FlatDestination>(_flatSource);

        /// <summary>
        /// Measures a warmed Mapster flat map.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatDestination Mapster_Warm_Flat()
            => _flatSource.Adapt<FlatDestination>(_mapsterConfig);

        /// <summary>
        /// Measures cold startup, configuration, compilation, and first map execution.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatDestination OctoMap_Cold_Compile_And_Map()
        {
            var services = new ServiceCollection();
            services.AddOctoMap(registration => registration.AddProfile<ColdBenchmarkProfile>());
            var mapper = services.BuildServiceProvider().GetRequiredService<IOctoMapper>();
            return mapper.Map<FlatSource, FlatDestination>(_flatSource);
        }

        /// <summary>
        /// Measures AutoMapper cold startup, configuration, compilation, and first map execution.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatDestination AutoMapper_Cold_Compile_And_Map()
        {
            var configuration = CreateAutoMapperConfiguration();
            configuration.CompileMappings();
            return configuration.CreateMapper().Map<FlatDestination>(_flatSource);
        }

        /// <summary>
        /// Measures Mapster cold startup, configuration, compilation, and first map execution.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatDestination Mapster_Cold_Compile_And_Map()
        {
            var configuration = CreateMapsterConfiguration();
            configuration.Compile();
            return _flatSource.Adapt<FlatDestination>(configuration);
        }

        /// <summary>
        /// Measures eager compilation of all configured maps.
        /// </summary>
        [Benchmark]
        public void OctoMap_Startup_Compile_All()
        {
            var services = new ServiceCollection();
            services.AddSingleton<StatusCatalog>();
            services.AddTransient<StatusResolver>();
            services.AddTransient<CurrencyTextConverter>();
            services.AddTransient<IncrementConverter>();
            services.AddOctoMap(registration => registration.AddProfile<BenchmarkProfile>());
            services.BuildServiceProvider().GetRequiredService<IOctoMapper>().CompileMappings();
        }

        /// <summary>
        /// Measures AutoMapper startup configuration and compilation.
        /// </summary>
        [Benchmark]
        public void AutoMapper_Startup_Compile_All()
            => CreateAutoMapperConfiguration().CompileMappings();

        /// <summary>
        /// Measures Mapster startup configuration and compilation.
        /// </summary>
        [Benchmark]
        public void Mapster_Startup_Compile_All()
            => CreateMapsterConfiguration().Compile();

        /// <summary>
        /// Measures nested object mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public NestedDestination OctoMap_Nested()
            => _mapper.Map<NestedSource, NestedDestination>(_nestedSource);

        /// <summary>
        /// Measures hand-written nested object mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public NestedDestination Manual_Nested()
            => new()
            {
                Id = _nestedSource.Id,
                Customer = new CustomerDestination
                {
                    FullName = _nestedSource.Customer.FirstName + " " + _nestedSource.Customer.LastName
                }
            };

        /// <summary>
        /// Measures AutoMapper nested object mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public NestedDestination AutoMapper_Nested()
            => _autoMapper.Map<NestedDestination>(_nestedSource);

        /// <summary>
        /// Measures Mapster nested object mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public NestedDestination Mapster_Nested()
            => _nestedSource.Adapt<NestedDestination>(_mapsterConfig);

        /// <summary>
        /// Measures collection member mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public CollectionDestination OctoMap_Collections()
            => _mapper.Map<CollectionSource, CollectionDestination>(_collectionSource);

        /// <summary>
        /// Measures hand-written collection member mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public CollectionDestination Manual_Collections()
            => new()
            {
                Items = _collectionSource.Items
                    .Select(x => new ItemDestination
                    {
                        Sku = x.Sku,
                        Quantity = x.Quantity
                    })
                    .ToArray()
            };

        /// <summary>
        /// Measures AutoMapper collection member mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public CollectionDestination AutoMapper_Collections()
            => _autoMapper.Map<CollectionDestination>(_collectionSource);

        /// <summary>
        /// Measures Mapster collection member mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public CollectionDestination Mapster_Collections()
            => _collectionSource.Adapt<CollectionDestination>(_mapsterConfig);

        /// <summary>
        /// Measures DI resolver execution.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public ResolverDestination OctoMap_Resolver()
            => _mapper.Map<FlatSource, ResolverDestination>(_flatSource);

        /// <summary>
        /// Measures typed DI resolver execution.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public ResolverDestination OctoMap_Typed_Resolver()
            => _typedResolverMapper.Map(_flatSource, _mapContext);

        /// <summary>
        /// Measures DI value converter execution.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public ConverterDestination OctoMap_Converter()
            => _mapper.Map<FlatSource, ConverterDestination>(_flatSource);

        /// <summary>
        /// Measures typed DI value converter execution.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public ConverterDestination OctoMap_Typed_Converter()
            => _typedConverterMapper.Map(_flatSource, _mapContext);

        /// <summary>
        /// Measures simple DI value converter execution.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public SimpleConverterDestination OctoMap_Simple_Converter()
            => _mapper.Map<FlatSource, SimpleConverterDestination>(_flatSource);

        /// <summary>
        /// Measures typed simple DI value converter execution.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public SimpleConverterDestination OctoMap_Typed_Simple_Converter()
            => _typedSimpleConverterMapper.Map(_flatSource, _mapContext);

        /// <summary>
        /// Measures direct converter invocation without mapper or service provider overhead.
        /// </summary>
        /// <returns>The converted destination member.</returns>
        [Benchmark]
        public string Manual_Converter_Direct()
            => _directConverter.Convert(_flatSource.Total, _mapContext);

        /// <summary>
        /// Measures converter resolution through the service provider and direct invocation.
        /// </summary>
        /// <returns>The converted destination member.</returns>
        [Benchmark]
        public string Manual_Converter_ServiceProvider()
            => _services.GetRequiredService<CurrencyTextConverter>().Convert(_flatSource.Total, _mapContext);

        /// <summary>
        /// Measures direct simple converter invocation without mapper or service provider overhead.
        /// </summary>
        /// <returns>The converted destination member.</returns>
        [Benchmark]
        public int Manual_Simple_Converter_Direct()
            => _directSimpleConverter.Convert(_flatSource.Id, _mapContext);

        /// <summary>
        /// Measures simple converter resolution through the service provider and direct invocation.
        /// </summary>
        /// <returns>The converted destination member.</returns>
        [Benchmark]
        public int Manual_Simple_Converter_ServiceProvider()
            => _services.GetRequiredService<IncrementConverter>().Convert(_flatSource.Id, _mapContext);

        /// <summary>
        /// Measures constructor mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public ConstructorDestination OctoMap_Constructor()
            => _mapper.Map<ConstructorSource, ConstructorDestination>(_constructorSource);

        /// <summary>
        /// Measures hand-written constructor mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public ConstructorDestination Manual_Constructor()
            => new(_constructorSource.Id, _constructorSource.Name);

        /// <summary>
        /// Measures AutoMapper constructor mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public ConstructorDestination AutoMapper_Constructor()
            => _autoMapper.Map<ConstructorDestination>(_constructorSource);

        /// <summary>
        /// Measures Mapster constructor mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public ConstructorDestination Mapster_Constructor()
            => _constructorSource.Adapt<ConstructorDestination>(_mapsterConfig);

        /// <summary>
        /// Measures flattening by convention.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatteningDestination OctoMap_Flattening()
            => _mapper.Map<FlatteningSource, FlatteningDestination>(_flatteningSource);

        /// <summary>
        /// Measures hand-written flattening.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatteningDestination Manual_Flattening()
            => new()
            {
                CustomerFirstName = _flatteningSource.Customer.FirstName
            };

        /// <summary>
        /// Measures AutoMapper flattening by convention.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatteningDestination AutoMapper_Flattening()
            => _autoMapper.Map<FlatteningDestination>(_flatteningSource);

        /// <summary>
        /// Measures Mapster flattening by convention.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatteningDestination Mapster_Flattening()
            => _flatteningSource.Adapt<FlatteningDestination>(_mapsterConfig);

        /// <summary>
        /// Measures projection expression usage over LINQ to Objects.
        /// </summary>
        /// <returns>The projected destination count.</returns>
        [Benchmark]
        public int OctoMap_Projection()
            => _queryable.ProjectTo<FlatDestination>(_mapper.ProjectionBuilder).Count();

        /// <summary>
        /// Measures hand-written projection expression usage over LINQ to Objects.
        /// </summary>
        /// <returns>The projected destination count.</returns>
        [Benchmark]
        public int Manual_Projection()
            => _queryable.Select(x => new FlatDestination
            {
                Id = x.Id,
                Name = x.Name,
                Total = x.Total,
                Status = x.StatusCode
            }).Count();

        /// <summary>
        /// Measures AutoMapper projection expression usage over LINQ to Objects.
        /// </summary>
        /// <returns>The projected destination count.</returns>
        [Benchmark]
        public int AutoMapper_Projection()
            => _queryable.ProjectTo<FlatDestination>(_autoMapperConfiguration).Count();

        /// <summary>
        /// Measures Mapster projection expression usage over LINQ to Objects.
        /// </summary>
        /// <returns>The projected destination count.</returns>
        [Benchmark]
        public int Mapster_Projection()
            => _queryable.ProjectToType<FlatDestination>(_mapsterConfig).Count();

        private static MapperConfiguration CreateAutoMapperConfiguration()
            => new MapperConfiguration(configuration =>
            {
                configuration.CreateMap<FlatSource, FlatDestination>()
                    .ForMember(x => x.Status, x => x.MapFrom(s => s.StatusCode));
                configuration.CreateMap<CustomerSource, CustomerDestination>()
                    .ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName));
                configuration.CreateMap<NestedSource, NestedDestination>();
                configuration.CreateMap<ItemSource, ItemDestination>();
                configuration.CreateMap<CollectionSource, CollectionDestination>();
                configuration.CreateMap<ConstructorSource, ConstructorDestination>()
                    .ConstructUsing(s => new ConstructorDestination(s.Id, s.Name));
                configuration.CreateMap<FlatteningSource, FlatteningDestination>();
            }, NullLoggerFactory.Instance);

        private static TypeAdapterConfig CreateMapsterConfiguration()
        {
            var configuration = new TypeAdapterConfig();
            configuration.NewConfig<FlatSource, FlatDestination>()
                .Map(x => x.Status, x => x.StatusCode);
            configuration.NewConfig<CustomerSource, CustomerDestination>()
                .Map(x => x.FullName, x => x.FirstName + " " + x.LastName);
            configuration.NewConfig<NestedSource, NestedDestination>();
            configuration.NewConfig<ItemSource, ItemDestination>();
            configuration.NewConfig<CollectionSource, CollectionDestination>();
            configuration.NewConfig<ConstructorSource, ConstructorDestination>()
                .MapToConstructor(true);
            configuration.NewConfig<FlatteningSource, FlatteningDestination>();
            return configuration;
        }
    }

    /// <summary>
    /// Defines benchmark maps.
    /// </summary>
    public sealed class BenchmarkProfile : OctoMapProfile
    {
        /// <inheritdoc/>
        public override void Configure(IOctoMapConfigurationBuilder builder)
        {
            builder.CreateMap<FlatSource, FlatDestination>()
                .ForMember(x => x.Status, x => x.MapFrom(s => s.StatusCode));
            builder.CreateMap<CustomerSource, CustomerDestination>()
                .ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName));
            builder.CreateMap<NestedSource, NestedDestination>();
            builder.CreateMap<ItemSource, ItemDestination>();
            builder.CreateMap<CollectionSource, CollectionDestination>();
            builder.CreateMap<FlatSource, ResolverDestination>()
                .ForMember(x => x.Status, x => x.ResolveUsing<StatusResolver>());
            builder.CreateMap<FlatSource, ConverterDestination>()
                .ForMember(x => x.TotalText, x => x.ConvertUsing<CurrencyTextConverter>(s => s.Total));
            builder.CreateMap<FlatSource, SimpleConverterDestination>()
                .ForMember(x => x.Value, x => x.ConvertUsing<IncrementConverter>(s => s.Id));
            builder.CreateMap<ConstructorSource, ConstructorDestination>();
            builder.CreateMap<FlatteningSource, FlatteningDestination>();
        }
    }

    /// <summary>
    /// Defines the minimal map used for cold compile benchmarks.
    /// </summary>
    public sealed class ColdBenchmarkProfile : OctoMapProfile
    {
        /// <inheritdoc/>
        public override void Configure(IOctoMapConfigurationBuilder builder)
            => builder.CreateMap<FlatSource, FlatDestination>()
                .ForMember(x => x.Status, x => x.MapFrom(s => s.StatusCode));
    }

    /// <summary>
    /// Provides status labels for resolver benchmarks.
    /// </summary>
    public sealed class StatusCatalog
    {
        /// <summary>
        /// Gets a status label.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <returns>The status label.</returns>
        public string GetStatus(string statusCode)
            => statusCode == "A" ? "Active" : "Unknown";
    }

    /// <summary>
    /// Resolves status labels through DI.
    /// </summary>
    public sealed class StatusResolver : IValueResolver<FlatSource, ResolverDestination, string>
    {
        private readonly StatusCatalog _catalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusResolver"/> class.
        /// </summary>
        /// <param name="catalog">The status catalog.</param>
        public StatusResolver(StatusCatalog catalog)
        {
            _catalog = catalog;
        }

        /// <inheritdoc/>
        public string Resolve(FlatSource source, ResolverDestination destination, IMapContext context)
            => _catalog.GetStatus(source.StatusCode);
    }

    /// <summary>
    /// Converts decimal totals into currency text.
    /// </summary>
    public sealed class CurrencyTextConverter : IValueConverter<decimal, string>
    {
        /// <inheritdoc/>
        public string Convert(decimal sourceMember, IMapContext context)
            => sourceMember.ToString("0.00");
    }

    /// <summary>
    /// Converts integer values with a minimal arithmetic operation.
    /// </summary>
    public sealed class IncrementConverter : IValueConverter<int, int>
    {
        /// <inheritdoc/>
        public int Convert(int sourceMember, IMapContext context)
            => sourceMember + 1;
    }

    /// <summary>
    /// Represents a flat source model.
    /// </summary>
    public sealed class FlatSource
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total.
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        public string StatusCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a flat destination model.
    /// </summary>
    public sealed class FlatDestination
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total.
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a customer source model.
    /// </summary>
    public sealed class CustomerSource
    {
        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a customer destination model.
    /// </summary>
    public sealed class CustomerDestination
    {
        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        public string FullName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a nested source model.
    /// </summary>
    public sealed class NestedSource
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the customer.
        /// </summary>
        public CustomerSource Customer { get; set; } = new();
    }

    /// <summary>
    /// Represents a nested destination model.
    /// </summary>
    public sealed class NestedDestination
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the customer.
        /// </summary>
        public CustomerDestination Customer { get; set; } = new();
    }

    /// <summary>
    /// Represents an item source model.
    /// </summary>
    public sealed class ItemSource
    {
        /// <summary>
        /// Gets or sets the SKU.
        /// </summary>
        public string Sku { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Represents an item destination model.
    /// </summary>
    public sealed class ItemDestination
    {
        /// <summary>
        /// Gets or sets the SKU.
        /// </summary>
        public string Sku { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Represents a collection source model.
    /// </summary>
    public sealed class CollectionSource
    {
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        public IReadOnlyList<ItemSource> Items { get; set; } = Array.Empty<ItemSource>();
    }

    /// <summary>
    /// Represents a collection destination model.
    /// </summary>
    public sealed class CollectionDestination
    {
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        public IReadOnlyList<ItemDestination> Items { get; set; } = Array.Empty<ItemDestination>();
    }

    /// <summary>
    /// Represents a resolver destination model.
    /// </summary>
    public sealed class ResolverDestination
    {
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a converter destination model.
    /// </summary>
    public sealed class ConverterDestination
    {
        /// <summary>
        /// Gets or sets the total text.
        /// </summary>
        public string TotalText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a destination model for simple converter benchmarks.
    /// </summary>
    public sealed class SimpleConverterDestination
    {
        /// <summary>
        /// Gets or sets the converted value.
        /// </summary>
        public int Value { get; set; }
    }

    /// <summary>
    /// Represents a constructor source model.
    /// </summary>
    public sealed class ConstructorSource
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a constructor destination model.
    /// </summary>
    public sealed class ConstructorDestination
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorDestination"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        public ConstructorDestination(int id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }
    }

    /// <summary>
    /// Represents a flattening source model.
    /// </summary>
    public sealed class FlatteningSource
    {
        /// <summary>
        /// Gets or sets the customer.
        /// </summary>
        public CustomerSource Customer { get; set; } = new();
    }

    /// <summary>
    /// Represents a flattening destination model.
    /// </summary>
    public sealed class FlatteningDestination
    {
        /// <summary>
        /// Gets or sets the customer first name.
        /// </summary>
        public string CustomerFirstName { get; set; } = string.Empty;
    }
}
