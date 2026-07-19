namespace OctoMap.Validation
{
    using System.Linq.Expressions;
    using System.Reflection;
    using OctoMap.Configuration;
    using OctoMap.Planning;

    /// <summary>
    /// Validates OctoMap configuration maps.
    /// </summary>
    internal sealed class OctoMapValidator : IOctoMapValidator
    {
        private readonly ITypeConversionRegistry _typeConversions;
        private readonly OctoMapOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapValidator"/> class.
        /// </summary>
        /// <param name="typeConversions">The type conversion registry.</param>
        /// <param name="options">The OctoMap options.</param>
        public OctoMapValidator(ITypeConversionRegistry typeConversions, OctoMapOptions options = null)
        {
            _typeConversions = typeConversions ?? throw new ArgumentNullException(nameof(typeConversions));
            _options = options ?? new OctoMapOptions();
        }

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

        private void ValidateMap(ITypeMap map, List<OctoMapValidationIssue> issues)
        {
            if (map.SourceType.ContainsGenericParameters || map.DestinationType.ContainsGenericParameters)
            {
                return;
            }

            if (map is not TypeMap typeMap)
            {
                ValidateDestinationCreation(map, issues);

                if (map is MultiSourceTypeMap multiSourceTypeMap)
                {
                    ValidateMultiSourceMap(multiSourceTypeMap, issues);
                }

                return;
            }

            ValidateDestinationCreation(typeMap, issues);
            ValidateConstructionExpression(typeMap, issues);
            ValidateDestinationPathConflicts(typeMap, issues);

            foreach (var memberMap in typeMap.MemberMaps.Values)
            {
                ValidateMemberMap(map, memberMap, issues);
            }

            ValidateConventionMemberConversions(typeMap, issues);
        }

        private void ValidateDestinationCreation(ITypeMap map, List<OctoMapValidationIssue> issues)
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

            if (map is TypeMap typeMap && typeMap.ConstructionExpression != null)
            {
                return;
            }

            if (map.DestinationType.GetConstructor(Type.EmptyTypes) != null)
            {
                return;
            }

            if (map is TypeMap singleSourceTypeMap && CanResolveConventionConstructor(singleSourceTypeMap))
            {
                return;
            }

            if (map.DestinationType.GetConstructor(Type.EmptyTypes) == null)
            {
                issues.Add(CreateIssue(map, null, $"Destination type '{map.DestinationType.FullName}' must have a public parameterless constructor or constructor parameters that match readable source properties."));
            }
        }

        private static void ValidateConstructionExpression(TypeMap map, List<OctoMapValidationIssue> issues)
        {
            if (map.ConstructionExpression == null)
            {
                return;
            }

            ValidateExpression(map, null, map.ConstructionExpression.Body, map.ConstructionExpression.Parameters[0], issues);
            if (!map.DestinationType.IsAssignableFrom(map.ConstructionExpression.Body.Type))
            {
                issues.Add(CreateIssue(map, null, $"ConstructUsing expression result type '{map.ConstructionExpression.Body.Type.FullName}' cannot be assigned to destination type '{map.DestinationType.FullName}'."));
            }
        }

        private void ValidateMemberMap(ITypeMap map, MemberMap memberMap, List<OctoMapValidationIssue> issues)
        {
            if (memberMap.IsIgnored)
            {
                return;
            }

            ValidateDestinationPath(map, memberMap, issues);

            if (!CanWrite(memberMap.DestinationProperty))
            {
                issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Destination member '{GetMemberName(memberMap)}' must have a public setter."));
            }

            if (memberMap.SourceExpression != null)
            {
                ValidateExpression(map, memberMap, memberMap.SourceExpression.Body, memberMap.SourceExpression.Parameters[0], issues);
                if (!memberMap.DestinationProperty.PropertyType.IsAssignableFrom(memberMap.SourceExpression.Body.Type)
                    && !_typeConversions.TryFind(memberMap.SourceExpression.Body.Type, memberMap.DestinationProperty.PropertyType, out _))
                {
                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"No type converter is registered for MapFrom result type '{memberMap.SourceExpression.Body.Type.FullName}' to destination member '{memberMap.DestinationProperty.Name}' of type '{memberMap.DestinationProperty.PropertyType.FullName}'."));
                }
            }

            if (memberMap.PreConditionExpression != null)
            {
                ValidateConditionExpression(map, memberMap, memberMap.PreConditionExpression, true, issues);
            }

            if (memberMap.ConditionExpression != null)
            {
                ValidateConditionExpression(map, memberMap, memberMap.ConditionExpression, false, issues);
            }

            if (memberMap.ResolverType != null)
            {
                ValidateResolver(map, memberMap, issues);
            }

            if (memberMap.ConverterType != null)
            {
                ValidateConverter(map, memberMap, issues);
            }

            if (memberMap.HasConstantValue)
            {
                ValidateValue(map, GetMemberName(memberMap), memberMap.ConstantValue, memberMap.DestinationProperty.PropertyType, "constant", issues);
            }

            if (memberMap.HasNullSubstitute)
            {
                if (memberMap.DestinationProperty.PropertyType.IsValueType)
                {
                    issues.Add(CreateIssue(map, GetMemberName(memberMap), $"NullSubstitute is only supported for reference-type destination members."));
                }

                ValidateValue(map, GetMemberName(memberMap), memberMap.NullSubstitute, memberMap.DestinationProperty.PropertyType, "null substitute", issues);
            }
        }

        private static void ValidateExpression(
            ITypeMap map,
            MemberMap memberMap,
            Expression expression,
            ParameterExpression sourceParameter,
            List<OctoMapValidationIssue> issues,
            ParameterExpression valueParameter = null)
        {
            switch (expression)
            {
                case ParameterExpression parameter when ReferenceEquals(parameter, sourceParameter):
                    return;
                case ParameterExpression parameter when valueParameter != null && ReferenceEquals(parameter, valueParameter):
                    return;
                case MethodCallExpression call:
                    ValidateMethodCall(map, memberMap, call, sourceParameter, issues, valueParameter);
                    return;
                case MemberExpression member:
                    if (member.Expression != null)
                    {
                        ValidateExpression(map, memberMap, member.Expression, sourceParameter, issues, valueParameter);
                    }

                    if (member.Member is PropertyInfo or FieldInfo)
                    {
                        return;
                    }

                    issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Member '{member.Member.Name}' is not supported in MapFrom expressions."));
                    return;
                case ConstantExpression constant:
                    ValidateValue(map, GetMemberName(memberMap), constant.Value, constant.Type, "constant expression", issues);
                    return;
                case BinaryExpression binary:
                    ValidateExpression(map, memberMap, binary.Left, sourceParameter, issues, valueParameter);
                    ValidateExpression(map, memberMap, binary.Right, sourceParameter, issues, valueParameter);
                    if (!IsSupportedBinaryExpression(binary.NodeType))
                    {
                        issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Binary expression '{binary.NodeType}' is not supported in MapFrom expressions."));
                    }

                    return;
                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked:
                    ValidateExpression(map, memberMap, unary.Operand, sourceParameter, issues, valueParameter);
                    return;
                case UnaryExpression unary when unary.NodeType == ExpressionType.Not:
                    ValidateExpression(map, memberMap, unary.Operand, sourceParameter, issues, valueParameter);
                    return;
                case ConditionalExpression conditional:
                    ValidateExpression(map, memberMap, conditional.Test, sourceParameter, issues, valueParameter);
                    ValidateExpression(map, memberMap, conditional.IfTrue, sourceParameter, issues, valueParameter);
                    ValidateExpression(map, memberMap, conditional.IfFalse, sourceParameter, issues, valueParameter);
                    return;
                case NewExpression @new:
                    foreach (var argument in @new.Arguments)
                    {
                        ValidateExpression(map, memberMap, argument, sourceParameter, issues, valueParameter);
                    }

                    return;
                default:
                    issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Expression node '{expression.NodeType}' is not supported in MapFrom expressions."));
                    return;
            }
        }

        private static void ValidateConditionExpression(
            ITypeMap map,
            MemberMap memberMap,
            LambdaExpression expression,
            bool isPreCondition,
            List<OctoMapValidationIssue> issues)
        {
            var expectedParameterCount = isPreCondition ? 1 : expression.Parameters.Count;
            if (expectedParameterCount != expression.Parameters.Count || expression.Parameters.Count is < 1 or > 2)
            {
                issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Condition for member '{GetMemberName(memberMap)}' must declare one source parameter or source and value parameters."));
                return;
            }

            if (isPreCondition && expression.Parameters.Count != 1)
            {
                issues.Add(CreateIssue(map, GetMemberName(memberMap), $"PreCondition for member '{GetMemberName(memberMap)}' must declare one source parameter."));
                return;
            }

            if (expression.Parameters.Count == 2
                && !expression.Parameters[1].Type.IsAssignableFrom(memberMap.DestinationProperty.PropertyType)
                && !memberMap.DestinationProperty.PropertyType.IsAssignableFrom(expression.Parameters[1].Type))
            {
                issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Condition value parameter type '{expression.Parameters[1].Type.FullName}' is not compatible with destination member '{GetMemberName(memberMap)}' of type '{memberMap.DestinationProperty.PropertyType.FullName}'."));
                return;
            }

            var valueParameter = expression.Parameters.Count == 2 ? expression.Parameters[1] : null;
            ValidateExpression(map, memberMap, expression.Body, expression.Parameters[0], issues, valueParameter);
            if (expression.Body.Type != typeof(bool))
            {
                issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Condition for member '{GetMemberName(memberMap)}' must return Boolean."));
            }
        }

        private static string GetMemberName(MemberMap memberMap)
            => memberMap == null
                ? null
                : string.Join(".", memberMap.DestinationPath.Select(x => x.Name));

        private static void ValidateDestinationPath(ITypeMap map, MemberMap memberMap, List<OctoMapValidationIssue> issues)
        {
            for (var index = 0; index < memberMap.DestinationPath.Count; index++)
            {
                var property = memberMap.DestinationPath[index];
                if (!CanWrite(property))
                {
                    issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Destination path member '{property.Name}' must have a public setter."));
                }

                if (index == memberMap.DestinationPath.Count - 1)
                {
                    continue;
                }

                if (!property.CanRead || property.GetMethod == null || !property.GetMethod.IsPublic)
                {
                    issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Destination path member '{property.Name}' must have a public getter."));
                }

                if (property.PropertyType.IsValueType)
                {
                    issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Destination path member '{property.Name}' cannot be a value type."));
                    continue;
                }

                if (property.PropertyType.IsAbstract || property.PropertyType.IsInterface)
                {
                    issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Destination path member '{property.Name}' cannot be abstract or an interface."));
                    continue;
                }

                if (property.PropertyType.GetConstructor(Type.EmptyTypes) == null)
                {
                    issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Destination path member '{property.Name}' must have a public parameterless constructor."));
                }
            }
        }

        private static void ValidateDestinationPathConflicts(TypeMap map, List<OctoMapValidationIssue> issues)
        {
            var rootMemberMaps = map.MemberMaps.Values
                .Where(x => !x.UsesDestinationPath && !x.IsIgnored)
                .ToDictionary(x => x.DestinationProperty.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var pathMemberMap in map.MemberMaps.Values.Where(x => x.UsesDestinationPath && !x.IsIgnored))
            {
                var rootName = pathMemberMap.DestinationPath[0].Name;
                if (rootMemberMaps.ContainsKey(rootName))
                {
                    issues.Add(CreateIssue(
                        map,
                        GetMemberName(pathMemberMap),
                        $"Destination path '{GetMemberName(pathMemberMap)}' conflicts with configured destination member '{rootName}'."));
                }
            }
        }

        private void ValidateMultiSourceMap(MultiSourceTypeMap map, List<OctoMapValidationIssue> issues)
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

                    if (memberMap.SourceExpression == null && memberMap.ResolverType == null && memberMap.ConverterType == null && !memberMap.HasConstantValue)
                    {
                        issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Multi-source member '{GetMemberName(memberMap)}' must be mapped explicitly with MapFrom, ConvertUsing, ResolveUsing, or UseValue."));
                        continue;
                    }

                    ValidateMemberMap(sourceMap, memberMap, issues);
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
                if (!memberMap.DestinationProperty.PropertyType.IsAssignableFrom(memberMap.SourceExpression.Body.Type)
                    && !_typeConversions.TryFind(memberMap.SourceExpression.Body.Type, memberMap.DestinationProperty.PropertyType, out _))
                {
                    issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"No type converter is registered for multi-source MapFrom result type '{memberMap.SourceExpression.Body.Type.FullName}' to destination member '{memberMap.DestinationProperty.Name}' of type '{memberMap.DestinationProperty.PropertyType.FullName}'."));
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
                    if (!IsSupportedBinaryExpression(binary.NodeType))
                    {
                        issues.Add(CreateIssue(map, memberMap.DestinationProperty.Name, $"Binary expression '{binary.NodeType}' is not supported in multi-source MapFrom expressions."));
                    }

                    return;
                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked:
                    ValidateMultiSourceExpression(map, memberMap, unary.Operand, sourceParameter, issues);
                    return;
                case UnaryExpression unary when unary.NodeType == ExpressionType.Not:
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
            if (IsMultiSourceGetCall(expression, sourceParameter))
            {
                ValidateMultiSourceGetCall(map, memberMap, expression, issues);
                return;
            }

            if (expression.Object != null)
            {
                ValidateMultiSourceExpression(map, memberMap, expression.Object, sourceParameter, issues);
            }

            foreach (var argument in expression.Arguments)
            {
                ValidateMultiSourceExpression(map, memberMap, argument, sourceParameter, issues);
            }
        }

        private static void ValidateMethodCall(
            ITypeMap map,
            MemberMap memberMap,
            MethodCallExpression expression,
            ParameterExpression sourceParameter,
            List<OctoMapValidationIssue> issues,
            ParameterExpression valueParameter = null)
        {
            if (expression.Object != null)
            {
                ValidateExpression(map, memberMap, expression.Object, sourceParameter, issues, valueParameter);
            }

            foreach (var argument in expression.Arguments)
            {
                ValidateExpression(map, memberMap, argument, sourceParameter, issues, valueParameter);
            }
        }

        private static void ValidateMultiSourceGetCall(
            MultiSourceTypeMap map,
            MultiSourceMemberMap memberMap,
            MethodCallExpression expression,
            List<OctoMapValidationIssue> issues)
        {
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

        private static bool IsMultiSourceGetCall(MethodCallExpression expression, ParameterExpression sourceParameter)
            => ReferenceEquals(expression.Object, sourceParameter)
                && expression.Method.IsGenericMethod
                && expression.Method.GetGenericMethodDefinition() == typeof(IMultiSourceMapContext).GetMethod(nameof(IMultiSourceMapContext.Get));

        private bool CanResolveConventionConstructor(TypeMap map)
        {
            var sourceProperties = BuildSourcePropertyIndex(map.SourceType);

            return map.DestinationType
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                .OrderByDescending(x => x.GetCustomAttribute<MapConstructorAttribute>() != null)
                .ThenByDescending(x => x.GetParameters().Length)
                .Any(constructor =>
                {
                    var parameters = constructor.GetParameters();
                    return parameters.Length > 0
                        && parameters.All(parameter =>
                            sourceProperties.TryGetValue(GetDestinationMemberKey(parameter.Name), out var sourceProperty)
                            && (parameter.ParameterType.IsAssignableFrom(sourceProperty.PropertyType)
                                || _typeConversions.TryFind(sourceProperty.PropertyType, parameter.ParameterType, out _)));
                });
        }

        private static bool IsSupportedBinaryExpression(ExpressionType nodeType)
            => nodeType == ExpressionType.Add
                || nodeType == ExpressionType.Subtract
                || nodeType == ExpressionType.Multiply
                || nodeType == ExpressionType.Divide
                || nodeType == ExpressionType.Modulo
                || nodeType == ExpressionType.Equal
                || nodeType == ExpressionType.NotEqual
                || nodeType == ExpressionType.LessThan
                || nodeType == ExpressionType.LessThanOrEqual
                || nodeType == ExpressionType.GreaterThan
                || nodeType == ExpressionType.GreaterThanOrEqual
                || nodeType == ExpressionType.AndAlso
                || nodeType == ExpressionType.OrElse
                || nodeType == ExpressionType.Coalesce;

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

                    AddConfiguredMember(map, GetMemberName(memberMap), $"source '{sourceMap.SourceType.FullName}'", configuredMembers, issues);
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
                issues.Add(CreateIssue(map, memberName, $"The {valueKind} type '{value.GetType().FullName}' is not supported by the current OctoMap value emitter."));
            }
        }

        private static bool CanWrite(PropertyInfo property)
            => property.CanWrite && property.SetMethod != null && property.SetMethod.IsPublic;

        private IReadOnlyDictionary<string, PropertyInfo> BuildSourcePropertyIndex(Type sourceType)
        {
            var sourceProperties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in sourceType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.GetMethod != null))
            {
                var key = GetSourceMemberKey(property.Name);
                if (sourceProperties.ContainsKey(key))
                {
                    continue;
                }

                sourceProperties[key] = property;
            }

            return sourceProperties;
        }

        private string GetSourceMemberKey(string memberName)
            => _options.SourceNamingConvention.Normalize(ApplyAffixes(
                memberName,
                _options.SourceMemberPrefixes,
                _options.SourceMemberSuffixes));

        private string GetDestinationMemberKey(string memberName)
            => _options.DestinationNamingConvention.Normalize(ApplyAffixes(
                memberName,
                _options.DestinationMemberPrefixes,
                _options.DestinationMemberSuffixes));

        private static string ApplyAffixes(
            string memberName,
            IEnumerable<string> prefixes,
            IEnumerable<string> suffixes)
        {
            var name = memberName ?? string.Empty;
            foreach (var prefix in prefixes ?? Array.Empty<string>())
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name[prefix.Length..];
                    break;
                }
            }

            foreach (var suffix in suffixes ?? Array.Empty<string>())
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name[..^suffix.Length];
                    break;
                }
            }

            return name;
        }

        private void ValidateConventionMemberConversions(TypeMap map, List<OctoMapValidationIssue> issues)
        {
            var sourceProperties = BuildSourcePropertyIndex(map.SourceType);
            var configuredMembers = map.MemberMaps.Values
                .Where(x => !x.UsesDestinationPath)
                .ToDictionary(x => x.DestinationProperty.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var destinationProperty in map.DestinationType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(CanWrite))
            {
                if (configuredMembers.ContainsKey(destinationProperty.Name)
                    || !sourceProperties.TryGetValue(GetDestinationMemberKey(destinationProperty.Name), out var sourceProperty)
                    || destinationProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType)
                    || CanUseNestedMap(sourceProperty.PropertyType, destinationProperty.PropertyType)
                    || _typeConversions.TryFind(sourceProperty.PropertyType, destinationProperty.PropertyType, out _))
                {
                    continue;
                }

                if (AreSupportedCollections(sourceProperty.PropertyType, destinationProperty.PropertyType))
                {
                    ValidateCollectionMemberConversion(map, sourceProperty, destinationProperty, issues);
                    continue;
                }

                issues.Add(CreateIssue(
                    map,
                    destinationProperty.Name,
                    $"No type converter is registered for source member '{sourceProperty.Name}' of type '{sourceProperty.PropertyType.FullName}' to destination member '{destinationProperty.Name}' of type '{destinationProperty.PropertyType.FullName}'."));
            }
        }

        private static bool CanUseNestedMap(Type sourceType, Type destinationType)
        {
            if (sourceType == typeof(string) || destinationType == typeof(string))
            {
                return false;
            }

            if (sourceType.IsValueType || destinationType.IsValueType)
            {
                return false;
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(sourceType)
                || typeof(System.Collections.IEnumerable).IsAssignableFrom(destinationType))
            {
                return false;
            }

            return true;
        }

        private void ValidateCollectionMemberConversion(
            TypeMap map,
            PropertyInfo sourceProperty,
            PropertyInfo destinationProperty,
            List<OctoMapValidationIssue> issues)
        {
            if (!TryGetCollectionElementType(sourceProperty.PropertyType, out var sourceElementType)
                || !TryGetCollectionElementType(destinationProperty.PropertyType, out var destinationElementType)
                || destinationElementType.IsAssignableFrom(sourceElementType)
                || CanUseNestedMap(sourceElementType, destinationElementType)
                || _typeConversions.TryFind(sourceElementType, destinationElementType, out _))
            {
                return;
            }

            issues.Add(CreateIssue(
                map,
                destinationProperty.Name,
                $"No type converter is registered for collection element type '{sourceElementType.FullName}' to destination collection element type '{destinationElementType.FullName}' on member '{destinationProperty.Name}'."));
        }

        private static bool AreSupportedCollections(Type sourceType, Type destinationType)
            => TryGetCollectionElementType(sourceType, out _)
                && TryGetCollectionElementType(destinationType, out _);

        private static bool TryGetCollectionElementType(Type type, out Type elementType)
        {
            if (type != typeof(string) && type.IsArray && type.GetArrayRank() == 1)
            {
                elementType = type.GetElementType();
                return true;
            }

            if (type != typeof(string) && type.IsGenericType)
            {
                var genericDefinition = type.GetGenericTypeDefinition();
                if (genericDefinition == typeof(List<>)
                    || genericDefinition == typeof(HashSet<>)
                    || genericDefinition == typeof(IEnumerable<>)
                    || genericDefinition == typeof(ICollection<>)
                    || genericDefinition == typeof(IReadOnlyCollection<>)
                    || genericDefinition == typeof(IList<>)
                    || genericDefinition == typeof(IReadOnlyList<>)
                    || genericDefinition == typeof(ISet<>)
                    || genericDefinition == typeof(IReadOnlySet<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            elementType = null;
            return false;
        }

        private static void ValidateResolver(ITypeMap map, MemberMap memberMap, List<OctoMapValidationIssue> issues)
        {
            var resolverContract = typeof(IValueResolver<,,>).MakeGenericType(
                map.SourceType,
                map.DestinationType,
                memberMap.DestinationProperty.PropertyType);

            if (!resolverContract.IsAssignableFrom(memberMap.ResolverType))
            {
                issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Resolver type '{memberMap.ResolverType.FullName}' must implement '{resolverContract.FullName}'."));
            }
        }

        private static void ValidateConverter(ITypeMap map, MemberMap memberMap, List<OctoMapValidationIssue> issues)
        {
            if (memberMap.ConverterSourceExpression == null)
            {
                issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Converter source expression is required for member '{GetMemberName(memberMap)}'."));
                return;
            }

            ValidateExpression(map, memberMap, memberMap.ConverterSourceExpression.Body, memberMap.ConverterSourceExpression.Parameters[0], issues);

            var converterContract = typeof(IValueConverter<,>).MakeGenericType(
                memberMap.ConverterSourceExpression.Body.Type,
                memberMap.DestinationProperty.PropertyType);

            if (!converterContract.IsAssignableFrom(memberMap.ConverterType))
            {
                issues.Add(CreateIssue(map, GetMemberName(memberMap), $"Converter type '{memberMap.ConverterType.FullName}' must implement '{converterContract.FullName}'."));
            }
        }

        private static OctoMapValidationIssue CreateIssue(ITypeMap map, string memberName, string message)
            => new(map.SourceType, map.DestinationType, memberName, message);
    }
}
