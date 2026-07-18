using System.Reflection;
using System.Linq.Expressions;

namespace OctoMap.Planning
{
    /// <summary>
    /// Represents a convention-based executable mapping plan.
    /// </summary>
    public sealed class MappingPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappingPlan"/> class.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="assignments">The member assignment plans.</param>
        /// <param name="construction">The destination construction plan.</param>
        public MappingPlan(Type sourceType, Type destinationType, IReadOnlyList<MemberAssignmentPlan> assignments, DestinationConstructionPlan construction = null)
            : this(new[] { sourceType }, destinationType, assignments, construction)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingPlan"/> class.
        /// </summary>
        /// <param name="sourceTypes">The source types.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="assignments">The member assignment plans.</param>
        /// <param name="construction">The destination construction plan.</param>
        public MappingPlan(IReadOnlyList<Type> sourceTypes, Type destinationType, IReadOnlyList<MemberAssignmentPlan> assignments, DestinationConstructionPlan construction = null)
        {
            if (sourceTypes == null)
            {
                throw new ArgumentNullException(nameof(sourceTypes));
            }

            if (sourceTypes.Count == 0)
            {
                throw new ArgumentException("At least one source type is required.", nameof(sourceTypes));
            }

            SourceTypes = sourceTypes.ToArray();
            SourceType = SourceTypes[0];
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
            Assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
            Construction = construction;
        }

        /// <summary>
        /// Gets the source type.
        /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// Gets the source types.
        /// </summary>
        public IReadOnlyList<Type> SourceTypes { get; }

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        public Type DestinationType { get; }

        /// <summary>
        /// Gets the member assignment plans.
        /// </summary>
        public IReadOnlyList<MemberAssignmentPlan> Assignments { get; }

        /// <summary>
        /// Gets the destination construction plan.
        /// </summary>
        public DestinationConstructionPlan Construction { get; }

    }

    /// <summary>
    /// Represents one destination member assignment.
    /// </summary>
    public sealed class MemberAssignmentPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberAssignmentPlan"/> class.
        /// </summary>
        /// <param name="destinationProperty">The destination property.</param>
        /// <param name="sourceProperty">The source property.</param>
        /// <param name="sourceExpression">The source expression.</param>
        /// <param name="hasConstantValue">A value indicating whether this assignment uses a constant value.</param>
        /// <param name="constantValue">The constant value.</param>
        /// <param name="hasNullSubstitute">A value indicating whether this assignment has a null substitute.</param>
        /// <param name="nullSubstitute">The null substitute value.</param>
        public MemberAssignmentPlan(
            PropertyInfo destinationProperty,
            PropertyInfo sourceProperty,
            LambdaExpression sourceExpression,
            bool hasConstantValue,
            object constantValue,
            bool hasNullSubstitute,
            object nullSubstitute)
            : this(destinationProperty, sourceProperty, sourceExpression, null, null, null, false, CollectionShape.None, CollectionShape.None, null, null, true, hasConstantValue, constantValue, hasNullSubstitute, nullSubstitute, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberAssignmentPlan"/> class.
        /// </summary>
        /// <param name="destinationProperty">The destination property.</param>
        /// <param name="sourceProperty">The source property.</param>
        /// <param name="sourceExpression">The source expression.</param>
        /// <param name="resolverType">The resolver type.</param>
        /// <param name="converterType">The converter type.</param>
        /// <param name="converterSourceExpression">The converter source expression.</param>
        /// <param name="useNestedMap">A value indicating whether the assignment uses a nested map.</param>
        /// <param name="sourceCollectionShape">The source collection shape.</param>
        /// <param name="destinationCollectionShape">The destination collection shape.</param>
        /// <param name="sourceElementType">The source element type.</param>
        /// <param name="destinationElementType">The destination element type.</param>
        /// <param name="allowNullCollection">A value indicating whether null source collections are mapped as null destination collections.</param>
        /// <param name="hasConstantValue">A value indicating whether this assignment uses a constant value.</param>
        /// <param name="constantValue">The constant value.</param>
        /// <param name="hasNullSubstitute">A value indicating whether this assignment has a null substitute.</param>
        /// <param name="nullSubstitute">The null substitute value.</param>
        /// <param name="sourceIndex">The source index.</param>
        /// <param name="sourcePath">The source property path.</param>
        /// <param name="destinationPath">The destination property path.</param>
        public MemberAssignmentPlan(
            PropertyInfo destinationProperty,
            PropertyInfo sourceProperty,
            LambdaExpression sourceExpression,
            Type resolverType,
            Type converterType,
            LambdaExpression converterSourceExpression,
            bool useNestedMap,
            CollectionShape sourceCollectionShape,
            CollectionShape destinationCollectionShape,
            Type sourceElementType,
            Type destinationElementType,
            bool allowNullCollection,
            bool hasConstantValue,
            object constantValue,
            bool hasNullSubstitute,
            object nullSubstitute,
            int sourceIndex,
            IReadOnlyList<PropertyInfo> sourcePath = null,
            IReadOnlyList<PropertyInfo> destinationPath = null)
        {
            DestinationProperty = destinationProperty ?? throw new ArgumentNullException(nameof(destinationProperty));
            DestinationPath = destinationPath?.ToArray() ?? new[] { destinationProperty };
            SourceProperty = sourceProperty;
            SourceExpression = sourceExpression;
            ResolverType = resolverType;
            ConverterType = converterType;
            ConverterSourceExpression = converterSourceExpression;
            UseNestedMap = useNestedMap;
            SourceCollectionShape = sourceCollectionShape;
            DestinationCollectionShape = destinationCollectionShape;
            SourceElementType = sourceElementType;
            DestinationElementType = destinationElementType;
            AllowNullCollection = allowNullCollection;
            HasConstantValue = hasConstantValue;
            ConstantValue = constantValue;
            HasNullSubstitute = hasNullSubstitute;
            NullSubstitute = nullSubstitute;
            SourceIndex = sourceIndex;
            SourcePath = sourcePath ?? Array.Empty<PropertyInfo>();
        }

        /// <summary>
        /// Gets the source property.
        /// </summary>
        public PropertyInfo SourceProperty { get; }

        /// <summary>
        /// Gets the source expression.
        /// </summary>
        public LambdaExpression SourceExpression { get; }

        /// <summary>
        /// Gets the resolver type.
        /// </summary>
        public Type ResolverType { get; }

        /// <summary>
        /// Gets the converter type.
        /// </summary>
        public Type ConverterType { get; }

        /// <summary>
        /// Gets the converter source expression.
        /// </summary>
        public LambdaExpression ConverterSourceExpression { get; }

        /// <summary>
        /// Gets whether the assignment uses a nested map.
        /// </summary>
        public bool UseNestedMap { get; }

        /// <summary>
        /// Gets the source collection shape.
        /// </summary>
        public CollectionShape SourceCollectionShape { get; }

        /// <summary>
        /// Gets the destination collection shape.
        /// </summary>
        public CollectionShape DestinationCollectionShape { get; }

        /// <summary>
        /// Gets the source collection element type.
        /// </summary>
        public Type SourceElementType { get; }

        /// <summary>
        /// Gets the destination collection element type.
        /// </summary>
        public Type DestinationElementType { get; }

        /// <summary>
        /// Gets whether the assignment uses collection mapping.
        /// </summary>
        public bool UseCollectionMap => SourceCollectionShape != CollectionShape.None && DestinationCollectionShape != CollectionShape.None;

        /// <summary>
        /// Gets whether null source collections are mapped as null destination collections.
        /// </summary>
        public bool AllowNullCollection { get; }

        /// <summary>
        /// Gets whether this assignment uses a constant value.
        /// </summary>
        public bool HasConstantValue { get; }

        /// <summary>
        /// Gets the constant value.
        /// </summary>
        public object ConstantValue { get; }

        /// <summary>
        /// Gets whether this assignment has a null substitute.
        /// </summary>
        public bool HasNullSubstitute { get; }

        /// <summary>
        /// Gets the null substitute value.
        /// </summary>
        public object NullSubstitute { get; }

        /// <summary>
        /// Gets the source index.
        /// </summary>
        public int SourceIndex { get; }

        /// <summary>
        /// Gets the source property path used for flattened member mapping.
        /// </summary>
        public IReadOnlyList<PropertyInfo> SourcePath { get; }

        /// <summary>
        /// Gets whether the assignment uses flattened source member mapping.
        /// </summary>
        public bool UseFlattenedMap => SourcePath.Count > 0;

        /// <summary>
        /// Gets the destination property.
        /// </summary>
        public PropertyInfo DestinationProperty { get; }

        /// <summary>
        /// Gets the destination property path.
        /// </summary>
        public IReadOnlyList<PropertyInfo> DestinationPath { get; }

        /// <summary>
        /// Gets whether the assignment targets a nested destination property path.
        /// </summary>
        public bool UsesDestinationPath => DestinationPath.Count > 1;
    }
}
