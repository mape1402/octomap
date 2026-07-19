namespace OctoMap
{
    /// <summary>
    /// Defines a compiled two-source mapping operation.
    /// </summary>
    public interface IOctoMapping<TSource1, TSource2, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination Map(TSource1 source0, TSource2 source1, IMapContext context);
    }

    /// <summary>
    /// Defines a compiled context-free two-source mapping operation.
    /// </summary>
    public interface IOctoContextFreeMapping<TSource1, TSource2, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination MapContextFree(TSource1 source0, TSource2 source1);
    }

    /// <summary>
    /// Defines a compiled three-source mapping operation.
    /// </summary>
    public interface IOctoMapping<TSource1, TSource2, TSource3, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination Map(TSource1 source0, TSource2 source1, TSource3 source2, IMapContext context);
    }

    /// <summary>
    /// Defines a compiled context-free three-source mapping operation.
    /// </summary>
    public interface IOctoContextFreeMapping<TSource1, TSource2, TSource3, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination MapContextFree(TSource1 source0, TSource2 source1, TSource3 source2);
    }

    /// <summary>
    /// Defines a compiled four-source mapping operation.
    /// </summary>
    public interface IOctoMapping<TSource1, TSource2, TSource3, TSource4, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination Map(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, IMapContext context);
    }

    /// <summary>
    /// Defines a compiled context-free four-source mapping operation.
    /// </summary>
    public interface IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination MapContextFree(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3);
    }

    /// <summary>
    /// Defines a compiled five-source mapping operation.
    /// </summary>
    public interface IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination Map(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4, IMapContext context);
    }

    /// <summary>
    /// Defines a compiled context-free five-source mapping operation.
    /// </summary>
    public interface IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination MapContextFree(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4);
    }

    /// <summary>
    /// Defines a compiled six-source mapping operation.
    /// </summary>
    public interface IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination Map(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4, TSource6 source5, IMapContext context);
    }

    /// <summary>
    /// Defines a compiled context-free six-source mapping operation.
    /// </summary>
    public interface IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination MapContextFree(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4, TSource6 source5);
    }

    /// <summary>
    /// Defines a compiled seven-source mapping operation.
    /// </summary>
    public interface IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination Map(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4, TSource6 source5, TSource7 source6, IMapContext context);
    }

    /// <summary>
    /// Defines a compiled context-free seven-source mapping operation.
    /// </summary>
    public interface IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination MapContextFree(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4, TSource6 source5, TSource7 source6);
    }

    /// <summary>
    /// Defines a compiled eight-source mapping operation.
    /// </summary>
    public interface IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination Map(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4, TSource6 source5, TSource7 source6, TSource8 source7, IMapContext context);
    }

    /// <summary>
    /// Defines a compiled context-free eight-source mapping operation.
    /// </summary>
    public interface IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination MapContextFree(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4, TSource6 source5, TSource7 source6, TSource8 source7);
    }

    /// <summary>
    /// Defines a compiled nine-source mapping operation.
    /// </summary>
    public interface IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination Map(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4, TSource6 source5, TSource7 source6, TSource8 source7, TSource9 source8, IMapContext context);
    }

    /// <summary>
    /// Defines a compiled context-free nine-source mapping operation.
    /// </summary>
    public interface IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination MapContextFree(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4, TSource6 source5, TSource7 source6, TSource8 source7, TSource9 source8);
    }

    /// <summary>
    /// Defines a compiled ten-source mapping operation.
    /// </summary>
    public interface IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination Map(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4, TSource6 source5, TSource7 source6, TSource8 source7, TSource9 source8, TSource10 source9, IMapContext context);
    }

    /// <summary>
    /// Defines a compiled context-free ten-source mapping operation.
    /// </summary>
    public interface IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TDestination>
    {
        /// <summary>
        /// Maps the specified source instances to the destination type.
        /// </summary>
        TDestination MapContextFree(TSource1 source0, TSource2 source1, TSource3 source2, TSource4 source3, TSource5 source4, TSource6 source5, TSource7 source6, TSource8 source7, TSource9 source8, TSource10 source9);
    }
}
