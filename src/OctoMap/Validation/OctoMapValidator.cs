namespace OctoMap.Validation
{
    using System.Linq.Expressions;
    using System.Reflection;
    using OctoMap.Configuration;

    /// <summary>
    /// Validates OctoMap configuration maps.
    /// </summary>
    internal sealed class OctoMapValidator : IOctoMapValidator
    {
        /// <inheritdoc/>
        public OctoMapValidationReport Validate(IReadOnlyCollection<ITypeMap> maps)
        {
            if (maps == null)
            {
                throw new ArgumentNullException(nameof(maps));
            }

            var issues = new List<OctoMapValidationIssue>();
            foreach (var map in maps)
            {
                ValidateMap(map, issues);
            }

            return new OctoMapValidationReport(issues);
        }

        private static void ValidateMap(ITypeMap map, List<OctoMapValidationIssue> issues)
        {
            ValidateDestinationCreation(map, issues);

            if (map is not TypeMap typeMap)
            {
                return;
            }

            foreach (var memberMap in typeMap.MemberMaps.Values)
            {
                ValidateMemberMap(map, memberMap, issues);
            }
        }

        private static void ValidateDestinationCreation(ITypeMap map, List<OctoMapValidationIssue> issues)
        {
            if (map.DestinationType.IsAbstract || map.DestinationType.IsInterface)
            {
                issues.Add(CreateIssue(map, null, $"Destination type '{map.DestinationType.FullName}' cannot be created."));
                return;
            }

            if (map.DestinationType.IsValueType)
            {
                return;
            }

            if (map.DestinationType.GetConstructor(Type.EmptyTypes) == null)
            {
                issues.Add(CreateIssue(map, null, $"Destination type '{map.DestinationType.FullName}' must have a public parameterless constructor."));
            }
        }

        private static void ValidateMemberMap(ITypeMap map, MemberMap memberMap, List<OctoMapValidationIssue> issues)
        {
            if (memberMap.IsIgnored)
            {
                return;
            }

            if (!CanWrite(memberMap.DestinationProperty))
            {
                issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Destination member '{memberMap.DestinationProperty.Name}' must have a public setter."));
            }

            if (memberMap.SourceExpression != null)
            {
                ValidateExpression(map, memberMap, memberMap.SourceExpression.Body, memberMap.SourceExpression.Parameters[0], issues);
                if (!memberMap.DestinationProperty.PropertyType.IsAssignableFrom(memberMap.SourceExpression.Body.Type))
                {
                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"MapFrom expression result type '{memberMap.SourceExpression.Body.Type.FullName}' cannot be assigned to destination member '{memberMap.DestinationProperty.Name}' of type '{memberMap.DestinationProperty.PropertyType.FullName}'."));
                }
            }

            if (memberMap.HasConstantValue)
            {
                ValidateValue(map, memberMap.DestinationProperty.Name, memberMap.ConstantValue, memberMap.DestinationProperty.PropertyType, "constant", issues);
            }

            if (memberMap.HasNullSubstitute)
            {
                if (memberMap.DestinationProperty.PropertyType.IsValueType)
                {
                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"NullSubstitute is only supported for reference-type destination members in this phase."));
                }

                ValidateValue(map, memberMap.DestinationProperty.Name, memberMap.NullSubstitute, memberMap.DestinationProperty.PropertyType, "null substitute", issues);
            }
        }

        private static void ValidateExpression(ITypeMap map, MemberMap memberMap, Expression expression, ParameterExpression sourceParameter, List<OctoMapValidationIssue> issues)
        {
            switch (expression)
            {
                case ParameterExpression parameter when ReferenceEquals(parameter, sourceParameter):
                    return;
                case MemberExpression member:
                    if (member.Expression != null)
                    {
                        ValidateExpression(map, memberMap, member.Expression, sourceParameter, issues);
                    }

                    if (member.Member is PropertyInfo or FieldInfo)
                    {
                        return;
                    }

                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Member '{member.Member.Name}' is not supported in MapFrom expressions."));
                    return;
                case ConstantExpression constant:
                    ValidateValue(map, memberMap.DestinationProperty.Name, constant.Value, constant.Type, "constant expression", issues);
                    return;
                case BinaryExpression binary:
                    ValidateExpression(map, memberMap, binary.Left, sourceParameter, issues);
                    ValidateExpression(map, memberMap, binary.Right, sourceParameter, issues);
                    if (binary.NodeType != ExpressionType.Add)
                    {
                        issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Binary expression '{binary.NodeType}' is not supported in MapFrom expressions."));
                    }

                    return;
                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked:
                    ValidateExpression(map, memberMap, unary.Operand, sourceParameter, issues);
                    return;
                default:
                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Expression node '{expression.NodeType}' is not supported in MapFrom expressions."));
                    return;
            }
        }

        private static void ValidateValue(
            ITypeMap map,
            string memberName,
            object value,
            Type targetType,
            string valueKind,
            List<OctoMapValidationIssue> issues)
        {
            if (value == null)
            {
                if (targetType.IsValueType)
                {
                    issues.Add(CreateIssue(map, memberName, $"The {valueKind} for member '{memberName}' cannot be null because the member type is '{targetType.FullName}'."));
                }

                return;
            }

            if (!targetType.IsAssignableFrom(value.GetType()))
            {
                issues.Add(CreateIssue(map, memberName, $"The {valueKind} type '{value.GetType().FullName}' cannot be assigned to member '{memberName}' of type '{targetType.FullName}'."));
                return;
            }

            if (value is not string && value is not int && value is not bool && value is not decimal)
            {
                issues.Add(CreateIssue(map, memberName, $"The {valueKind} type '{value.GetType().FullName}' is not supported in this phase."));
            }
        }

        private static bool CanWrite(PropertyInfo property)
            => property.CanWrite && property.SetMethod != null && property.SetMethod.IsPublic;

        private static OctoMapValidationIssue CreateIssue(ITypeMap map, string memberName, string message)
            => new(map.SourceType, map.DestinationType, memberName, message);
    }
}
