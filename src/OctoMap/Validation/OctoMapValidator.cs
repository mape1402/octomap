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
                if (map is MultiSourceTypeMap multiSourceTypeMap)
                {
                    ValidateMultiSourceMap(multiSourceTypeMap, issues);
                }

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
                    if (binary.NodeType != ExpressionType.Add && binary.NodeType != ExpressionType.Equal && binary.NodeType != ExpressionType.NotEqual)
                    {
                        issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Binary expression '{binary.NodeType}' is not supported in MapFrom expressions."));
                    }

                    return;
                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked:
                    ValidateExpression(map, memberMap, unary.Operand, sourceParameter, issues);
                    return;
                case ConditionalExpression conditional:
                    ValidateExpression(map, memberMap, conditional.Test, sourceParameter, issues);
                    ValidateExpression(map, memberMap, conditional.IfTrue, sourceParameter, issues);
                    ValidateExpression(map, memberMap, conditional.IfFalse, sourceParameter, issues);
                    return;
                default:
                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Expression node '{expression.NodeType}' is not supported in MapFrom expressions."));
                    return;
            }
        }

        private static void ValidateMultiSourceMap(MultiSourceTypeMap map, List<OctoMapValidationIssue> issues)
        {
            if (map.SourceMaps.Count == 0)
            {
                issues.Add(CreateIssue(map, null, $"Multi-source map for destination type '{map.DestinationType.FullName}' must declare at least one source."));
                return;
            }

            ValidateDuplicateMultiSourceDestinations(map, issues);

            foreach (var sourceMap in map.SourceMaps)
            {
                foreach (var memberMap in sourceMap.MemberMaps.Values)
                {
                    if (memberMap.IsIgnored)
                    {
                        continue;
                    }

                    if (memberMap.SourceExpression == null && !memberMap.HasConstantValue)
                    {
                        issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Multi-source member '{memberMap.DestinationProperty.Name}' must be mapped explicitly with MapFrom or UseValue."));
                        continue;
                    }

                    ValidateMemberMap(map, memberMap, issues);
                }
            }

            foreach (var memberMap in map.ContextMemberMaps.Values)
            {
                if (memberMap.SourceExpression == null)
                {
                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Multi-source context member '{memberMap.DestinationProperty.Name}' must be mapped explicitly with MapFrom."));
                    continue;
                }

                if (!CanWrite(memberMap.DestinationProperty))
                {
                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Destination member '{memberMap.DestinationProperty.Name}' must have a public setter."));
                }

                ValidateMultiSourceExpression(map, memberMap, memberMap.SourceExpression.Body, memberMap.SourceExpression.Parameters[0], issues);
                if (!memberMap.DestinationProperty.PropertyType.IsAssignableFrom(memberMap.SourceExpression.Body.Type))
                {
                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"MapFrom expression result type '{memberMap.SourceExpression.Body.Type.FullName}' cannot be assigned to destination member '{memberMap.DestinationProperty.Name}' of type '{memberMap.DestinationProperty.PropertyType.FullName}'."));
                }
            }
        }

        private static void ValidateMultiSourceExpression(
            MultiSourceTypeMap map,
            MultiSourceMemberMap memberMap,
            Expression expression,
            ParameterExpression sourceParameter,
            List<OctoMapValidationIssue> issues)
        {
            switch (expression)
            {
                case MethodCallExpression call:
                    ValidateMultiSourceMethodCall(map, memberMap, call, sourceParameter, issues);
                    return;
                case MemberExpression member:
                    if (member.Expression != null)
                    {
                        ValidateMultiSourceExpression(map, memberMap, member.Expression, sourceParameter, issues);
                    }

                    if (member.Member is PropertyInfo or FieldInfo)
                    {
                        return;
                    }

                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Member '{member.Member.Name}' is not supported in multi-source MapFrom expressions."));
                    return;
                case ConstantExpression constant:
                    ValidateValue(map, memberMap.DestinationProperty.Name, constant.Value, constant.Type, "constant expression", issues);
                    return;
                case BinaryExpression binary:
                    ValidateMultiSourceExpression(map, memberMap, binary.Left, sourceParameter, issues);
                    ValidateMultiSourceExpression(map, memberMap, binary.Right, sourceParameter, issues);
                    if (binary.NodeType != ExpressionType.Add && binary.NodeType != ExpressionType.Equal && binary.NodeType != ExpressionType.NotEqual)
                    {
                        issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Binary expression '{binary.NodeType}' is not supported in multi-source MapFrom expressions."));
                    }

                    return;
                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked:
                    ValidateMultiSourceExpression(map, memberMap, unary.Operand, sourceParameter, issues);
                    return;
                case ConditionalExpression conditional:
                    ValidateMultiSourceExpression(map, memberMap, conditional.Test, sourceParameter, issues);
                    ValidateMultiSourceExpression(map, memberMap, conditional.IfTrue, sourceParameter, issues);
                    ValidateMultiSourceExpression(map, memberMap, conditional.IfFalse, sourceParameter, issues);
                    return;
                default:
                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Expression node '{expression.NodeType}' is not supported in multi-source MapFrom expressions."));
                    return;
            }
        }

        private static void ValidateMultiSourceMethodCall(
            MultiSourceTypeMap map,
            MultiSourceMemberMap memberMap,
            MethodCallExpression expression,
            ParameterExpression sourceParameter,
            List<OctoMapValidationIssue> issues)
        {
            if (!ReferenceEquals(expression.Object, sourceParameter)
                || !expression.Method.IsGenericMethod
                || expression.Method.GetGenericMethodDefinition() != typeof(IMultiSourceMapContext).GetMethod(nameof(IMultiSourceMapContext.Get)))
            {
                issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Method call '{expression.Method.Name}' is not supported in multi-source MapFrom expressions."));
                return;
            }

            var requestedType = expression.Method.GetGenericArguments()[0];
            var matchingSources = map.SourceTypes.Where(requestedType.IsAssignableFrom).ToArray();
            if (matchingSources.Length == 0)
            {
                issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Multi-source map does not declare source type '{requestedType.FullName}'."));
                return;
            }

            if (matchingSources.Length > 1)
            {
                issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Multi-source map has more than one source assignable to '{requestedType.FullName}'."));
            }
        }

        private static void ValidateDuplicateMultiSourceDestinations(MultiSourceTypeMap map, List<OctoMapValidationIssue> issues)
        {
            var configuredMembers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sourceMap in map.SourceMaps)
            {
                foreach (var memberMap in sourceMap.MemberMaps.Values)
                {
                    if (memberMap.IsIgnored)
                    {
                        continue;
                    }

                    AddConfiguredMember(map, memberMap.DestinationProperty.Name, $"source '{sourceMap.SourceType.FullName}'", configuredMembers, issues);
                }
            }

            foreach (var memberMap in map.ContextMemberMaps.Values)
            {
                AddConfiguredMember(map, memberMap.DestinationProperty.Name, "multi-source context", configuredMembers, issues);
            }
        }

        private static void AddConfiguredMember(
            ITypeMap map,
            string memberName,
            string sourceDescription,
            Dictionary<string, string> configuredMembers,
            List<OctoMapValidationIssue> issues)
        {
            if (configuredMembers.TryGetValue(memberName, out var previousSource))
            {
                issues.Add(CreateIssue(map, memberName, $"Destination member '{memberName}' is configured more than once in multi-source map: {previousSource} and {sourceDescription}."));
                return;
            }

            configuredMembers[memberName] = sourceDescription;
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
