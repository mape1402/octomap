using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OctoMap.Analyzers
{
    /// <summary>
    /// Analyzes OctoMap projection usage against runtime-only map configuration.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RuntimeOnlyProjectionFeatureAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ISet<string> RuntimeOnlyMethodNames = new HashSet<string>
        {
            "ResolveUsing",
            "ConvertUsing",
            "BeforeMap",
            "AfterMap"
        };

        private static readonly DiagnosticDescriptor RuntimeOnlyProjectionFeatureRule = new(
            OctoMapDiagnosticIds.RuntimeOnlyProjectionFeature,
            "Projected map uses a runtime-only rule",
            "Map '{1}->{2}' uses OctoMap rule '{0}', which cannot be translated by ProjectTo",
            "OctoMap.Projection",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "DI resolvers, DI value converters, and lifecycle actions run during object mapping but cannot be translated by LINQ providers.",
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(RuntimeOnlyProjectionFeatureRule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(StartAnalysis);
        }

        private static void StartAnalysis(CompilationStartAnalysisContext context)
        {
            var runtimeOnlyUsages = new ConcurrentBag<RuntimeOnlyMapUsage>();
            var projectedMaps = new ConcurrentBag<ProjectedMapUsage>();

            context.RegisterSyntaxNodeAction(nodeContext =>
                AnalyzeInvocation(nodeContext, runtimeOnlyUsages, projectedMaps), SyntaxKind.InvocationExpression);

            context.RegisterCompilationEndAction(endContext =>
            {
                var projections = projectedMaps.ToArray();
                foreach (var usage in runtimeOnlyUsages)
                {
                    if (!IsProjected(usage, projections))
                    {
                        continue;
                    }

                    endContext.ReportDiagnostic(Diagnostic.Create(
                        RuntimeOnlyProjectionFeatureRule,
                        usage.Location,
                        usage.MethodName,
                        usage.SourceType.Name,
                        usage.DestinationType.Name));
                }
            });
        }

        private static void AnalyzeInvocation(
            SyntaxNodeAnalysisContext context,
            ConcurrentBag<RuntimeOnlyMapUsage> runtimeOnlyUsages,
            ConcurrentBag<ProjectedMapUsage> projectedMaps)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null)
            {
                return;
            }

            if (IsProjectToCall(symbol))
            {
                AddProjectedMap(context, invocation, symbol, projectedMaps);
                return;
            }

            var methodName = GetMethodName(invocation);
            if (methodName == null || !RuntimeOnlyMethodNames.Contains(methodName) || !IsOctoMapSymbol(symbol))
            {
                return;
            }

            var createMap = FindCreateMapInvocation(context, invocation);
            if (createMap == null)
            {
                return;
            }

            runtimeOnlyUsages.Add(new RuntimeOnlyMapUsage(
                createMap.SourceType,
                createMap.DestinationType,
                methodName,
                invocation.GetLocation()));
        }

        private static void AddProjectedMap(
            SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocation,
            IMethodSymbol symbol,
            ConcurrentBag<ProjectedMapUsage> projectedMaps)
        {
            if (symbol.TypeArguments.Length != 1)
            {
                return;
            }

            var sourceType = TryGetProjectToSourceType(context, invocation);
            projectedMaps.Add(new ProjectedMapUsage(sourceType, symbol.TypeArguments[0]));
        }

        private static ITypeSymbol TryGetProjectToSourceType(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
        {
            ExpressionSyntax sourceExpression = null;
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                sourceExpression = memberAccess.Expression;
            }
            else if (invocation.ArgumentList.Arguments.Count > 0)
            {
                sourceExpression = invocation.ArgumentList.Arguments[0].Expression;
            }

            if (sourceExpression == null)
            {
                return null;
            }

            var sourceExpressionType = context.SemanticModel.GetTypeInfo(sourceExpression).Type;
            return TryGetQueryableElementType(sourceExpressionType);
        }

        private static ITypeSymbol TryGetQueryableElementType(ITypeSymbol type)
        {
            if (type == null)
            {
                return null;
            }

            if (type is INamedTypeSymbol namedType
                && namedType.IsGenericType
                && namedType.ConstructedFrom.ToDisplayString() == "System.Linq.IQueryable<T>"
                && namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0];
            }

            return type.AllInterfaces
                .Where(x => x.IsGenericType && x.ConstructedFrom.ToDisplayString() == "System.Linq.IQueryable<T>")
                .Select(x => x.TypeArguments[0])
                .FirstOrDefault();
        }

        private static CreateMapUsage FindCreateMapInvocation(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
        {
            foreach (var ancestorInvocation in invocation.AncestorsAndSelf().OfType<InvocationExpressionSyntax>())
            {
                var createMap = TryGetCreateMapUsage(context, ancestorInvocation);
                if (createMap != null)
                {
                    return createMap;
                }

                if (ancestorInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    createMap = FindCreateMapInExpression(context, memberAccess.Expression);
                    if (createMap != null)
                    {
                        return createMap;
                    }
                }
            }

            return null;
        }

        private static CreateMapUsage FindCreateMapInExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            while (expression != null)
            {
                if (expression is InvocationExpressionSyntax invocation)
                {
                    var createMap = TryGetCreateMapUsage(context, invocation);
                    if (createMap != null)
                    {
                        return createMap;
                    }

                    expression = invocation.Expression is MemberAccessExpressionSyntax memberAccess
                        ? memberAccess.Expression
                        : null;
                    continue;
                }

                if (expression is MemberAccessExpressionSyntax nestedMemberAccess)
                {
                    expression = nestedMemberAccess.Expression;
                    continue;
                }

                return null;
            }

            return null;
        }

        private static CreateMapUsage TryGetCreateMapUsage(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null
                || symbol.Name != "CreateMap"
                || symbol.TypeArguments.Length != 2
                || !IsOctoMapSymbol(symbol))
            {
                return null;
            }

            return new CreateMapUsage(symbol.TypeArguments[0], symbol.TypeArguments[1]);
        }

        private static bool IsProjected(RuntimeOnlyMapUsage usage, IReadOnlyList<ProjectedMapUsage> projectedMaps)
            => projectedMaps.Any(projectedMap =>
                SymbolEqualityComparer.Default.Equals(projectedMap.DestinationType, usage.DestinationType)
                && (projectedMap.SourceType == null || SymbolEqualityComparer.Default.Equals(projectedMap.SourceType, usage.SourceType)));

        private static bool IsProjectToCall(IMethodSymbol symbol)
            => symbol.Name == "ProjectTo"
                && symbol.TypeArguments.Length == 1
                && IsOctoMapSymbol(symbol);

        private static string GetMethodName(InvocationExpressionSyntax invocation)
            => invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => null
            };

        private static bool IsOctoMapSymbol(IMethodSymbol symbol)
        {
            var containingType = symbol.ContainingType;
            while (containingType != null)
            {
                if (containingType.ContainingNamespace.ToDisplayString() == "OctoMap")
                {
                    return true;
                }

                containingType = containingType.ContainingType;
            }

            return false;
        }

        private sealed class RuntimeOnlyMapUsage
        {
            public RuntimeOnlyMapUsage(ITypeSymbol sourceType, ITypeSymbol destinationType, string methodName, Location location)
            {
                SourceType = sourceType;
                DestinationType = destinationType;
                MethodName = methodName;
                Location = location;
            }

            public ITypeSymbol SourceType { get; }

            public ITypeSymbol DestinationType { get; }

            public string MethodName { get; }

            public Location Location { get; }
        }

        private sealed class ProjectedMapUsage
        {
            public ProjectedMapUsage(ITypeSymbol sourceType, ITypeSymbol destinationType)
            {
                SourceType = sourceType;
                DestinationType = destinationType;
            }

            public ITypeSymbol SourceType { get; }

            public ITypeSymbol DestinationType { get; }
        }

        private sealed class CreateMapUsage
        {
            public CreateMapUsage(ITypeSymbol sourceType, ITypeSymbol destinationType)
            {
                SourceType = sourceType;
                DestinationType = destinationType;
            }

            public ITypeSymbol SourceType { get; }

            public ITypeSymbol DestinationType { get; }
        }
    }
}
