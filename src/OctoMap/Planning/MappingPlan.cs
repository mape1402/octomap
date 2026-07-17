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
        public MappingPlan(Type sourceType, Type destinationType, IReadOnlyList<MemberAssignmentPlan> assignments)
            : this(new[] { sourceType }, destinationType, assignments)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingPlan"/> class.
        /// </summary>
        /// <param name="sourceTypes">The source types.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="assignments">The member assignment plans.</param>
        public MappingPlan(IReadOnlyList<Type> sourceTypes, Type destinationType, IReadOnlyList<MemberAssignmentPlan> assignments)
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
            : this(destinationProperty, sourceProperty, sourceExpression, null, hasConstantValue, constantValue, hasNullSubstitute, nullSubstitute, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberAssignmentPlan"/> class.
        /// </summary>
        /// <param name="destinationProperty">The destination property.</param>
        /// <param name="sourceProperty">The source property.</param>
        /// <param name="sourceExpression">The source expression.</param>
        /// <param name="resolverType">The resolver type.</param>
        /// <param name="hasConstantValue">A value indicating whether this assignment uses a constant value.</param>
        /// <param name="constantValue">The constant value.</param>
        /// <param name="hasNullSubstitute">A value indicating whether this assignment has a null substitute.</param>
        /// <param name="nullSubstitute">The null substitute value.</param>
        /// <param name="sourceIndex">The source index.</param>
        public MemberAssignmentPlan(
            PropertyInfo destinationProperty,
            PropertyInfo sourceProperty,
            LambdaExpression sourceExpression,
            Type resolverType,
            bool hasConstantValue,
            object constantValue,
            bool hasNullSubstitute,
            object nullSubstitute,
            int sourceIndex)
        {
            DestinationProperty = destinationProperty ?? throw new ArgumentNullException(nameof(destinationProperty));
            SourceProperty = sourceProperty;
            SourceExpression = sourceExpression;
            ResolverType = resolverType;
            HasConstantValue = hasConstantValue;
            ConstantValue = constantValue;
            HasNullSubstitute = hasNullSubstitute;
            NullSubstitute = nullSubstitute;
            SourceIndex = sourceIndex;
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
        /// Gets the destination property.
        /// </summary>
        public PropertyInfo DestinationProperty { get; }
    }
}
