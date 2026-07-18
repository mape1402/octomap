using OctoMap.Configuration;
using OctoMap.Planning;

namespace OctoMap.Diagnostics
{
    /// <summary>
    /// Describes convention mapping plans.
    /// </summary>
    internal sealed class ConventionMappingPlanDescriber : IMappingPlanDescriber
    {
        /// <inheritdoc/>
        public string Describe(MappingPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var lines = new List<string>();
            foreach (var assignment in plan.Assignments)
            {
                lines.Add($"{GetDestinationPath(plan, assignment)} <- {GetSourceDescription(plan, assignment)}{GetConditionDescription(plan, assignment)}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string GetDestinationPath(MappingPlan plan, MemberAssignmentPlan assignment)
            => $"{plan.DestinationType.Name}.{string.Join(".", assignment.DestinationPath.Select(x => x.Name))}";

        private static string GetSourceDescription(MappingPlan plan, MemberAssignmentPlan assignment)
        {
            if (assignment.ResolverType != null)
            {
                return assignment.ResolverType.Name;
            }

            if (assignment.ConverterType != null)
            {
                return $"{assignment.ConverterType.Name}({GetExpressionDescription(assignment.ConverterSourceExpression, GetSourcePrefix(plan, assignment))})";
            }

            if (assignment.TypeConversion != null)
            {
                var source = assignment.SourceExpression != null
                    ? GetExpressionDescription(assignment.SourceExpression, GetSourcePrefix(plan, assignment))
                    : $"{GetSourcePrefix(plan, assignment)}.{assignment.SourceProperty.Name}";
                return $"{GetConversionDescription(assignment.TypeConversion)}({source})";
            }

            if (assignment.HasConstantValue)
            {
                return assignment.ConstantValue == null ? "null" : assignment.ConstantValue.ToString();
            }

            if (assignment.SourceExpression != null)
            {
                return GetExpressionDescription(assignment.SourceExpression, GetSourcePrefix(plan, assignment));
            }

            if (assignment.UseCollectionMap)
            {
                return $"{GetSourcePrefix(plan, assignment)}.{assignment.SourceProperty.Name}[]";
            }

            if (assignment.UseNestedMap)
            {
                return $"{GetSourcePrefix(plan, assignment)}.{assignment.SourceProperty.Name}";
            }

            if (assignment.UseFlattenedMap)
            {
                return $"{GetSourcePrefix(plan, assignment)}.{string.Join(".", assignment.SourcePath.Select(x => x.Name))}";
            }

            return $"{GetSourcePrefix(plan, assignment)}.{assignment.SourceProperty.Name}";
        }

        private static string GetConversionDescription(TypeConversionMap conversion)
            => conversion.UsesServiceConverter
                ? conversion.ConverterType.Name
                : $"{conversion.SourceType.Name}->{conversion.DestinationType.Name}";

        private static string GetConditionDescription(MappingPlan plan, MemberAssignmentPlan assignment)
        {
            var conditions = new List<string>();
            if (assignment.PreConditionExpression != null)
            {
                conditions.Add($"pre: {GetExpressionDescription(assignment.PreConditionExpression, GetSourcePrefix(plan, assignment))}");
            }

            if (assignment.ConditionExpression != null)
            {
                conditions.Add($"condition: {GetExpressionDescription(assignment.ConditionExpression, GetSourcePrefix(plan, assignment))}");
            }

            if (assignment.IgnoreNullSourceValue)
            {
                conditions.Add("ignore-null");
            }

            return conditions.Count == 0
                ? string.Empty
                : $" [{string.Join("; ", conditions)}]";
        }

        private static string GetSourcePrefix(MappingPlan plan, MemberAssignmentPlan assignment)
        {
            if (assignment.SourceIndex < 0)
            {
                return "Context";
            }

            return plan.SourceTypes.Count == 1
                ? plan.SourceType.Name
                : plan.SourceTypes[assignment.SourceIndex].Name;
        }

        private static string GetExpressionDescription(System.Linq.Expressions.LambdaExpression expression, string sourcePrefix)
        {
            if (expression == null)
            {
                return "expression";
            }

            var parameterPrefix = $"{expression.Parameters[0].Name}.";
            return expression.Body.ToString().Replace(parameterPrefix, $"{sourcePrefix}.", StringComparison.Ordinal);
        }
    }
}
