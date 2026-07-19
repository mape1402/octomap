using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddOctoMap(registration =>
            {
                registration.Options.EnableRuntimeImplicitMaps = false;
                registration.AddProfile<BenchmarkProfile>();
            });

            _mapper = services.BuildServiceProvider().GetRequiredService<IOctoMapper>();
            _mapper.CompileMappings();

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
        /// Measures eager compilation of all configured maps.
        /// </summary>
        [Benchmark]
        public void OctoMap_Startup_Compile_All()
        {
            var services = new ServiceCollection();
            services.AddSingleton<StatusCatalog>();
            services.AddTransient<StatusResolver>();
            services.AddTransient<CurrencyTextConverter>();
            services.AddOctoMap(registration => registration.AddProfile<BenchmarkProfile>());
            services.BuildServiceProvider().GetRequiredService<IOctoMapper>().CompileMappings();
        }

        /// <summary>
        /// Measures nested object mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public NestedDestination OctoMap_Nested()
            => _mapper.Map<NestedSource, NestedDestination>(_nestedSource);

        /// <summary>
        /// Measures collection member mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public CollectionDestination OctoMap_Collections()
            => _mapper.Map<CollectionSource, CollectionDestination>(_collectionSource);

        /// <summary>
        /// Measures DI resolver execution.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public ResolverDestination OctoMap_Resolver()
            => _mapper.Map<FlatSource, ResolverDestination>(_flatSource);

        /// <summary>
        /// Measures DI value converter execution.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public ConverterDestination OctoMap_Converter()
            => _mapper.Map<FlatSource, ConverterDestination>(_flatSource);

        /// <summary>
        /// Measures constructor mapping.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public ConstructorDestination OctoMap_Constructor()
            => _mapper.Map<ConstructorSource, ConstructorDestination>(_constructorSource);

        /// <summary>
        /// Measures flattening by convention.
        /// </summary>
        /// <returns>The mapped destination.</returns>
        [Benchmark]
        public FlatteningDestination OctoMap_Flattening()
            => _mapper.Map<FlatteningSource, FlatteningDestination>(_flatteningSource);

        /// <summary>
        /// Measures projection expression usage over LINQ to Objects.
        /// </summary>
        /// <returns>The projected destination count.</returns>
        [Benchmark]
        public int OctoMap_Projection()
            => _queryable.ProjectTo<FlatDestination>(_mapper.ProjectionBuilder).Count();
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
