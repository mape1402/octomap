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
                lines.Add($"{GetDestinationPath(plan, assignment)} <- {GetSourceDescription(plan, assignment)}");
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
