using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OctoMap.Analyzers
{
    /// <summary>
    /// Analyzes OctoMap configuration calls that are runtime-only and cannot be used by projections.
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
            "Mapping rule is runtime-only",
            "OctoMap rule '{0}' is runtime-only and cannot be translated by ProjectTo",
            "OctoMap.Projection",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "DI resolvers, DI value converters, and lifecycle actions run during object mapping but cannot be translated by LINQ providers.");

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(RuntimeOnlyProjectionFeatureRule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var methodName = GetMethodName(invocation);
            if (methodName == null || !RuntimeOnlyMethodNames.Contains(methodName))
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null || !IsOctoMapSymbol(symbol))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                RuntimeOnlyProjectionFeatureRule,
                invocation.GetLocation(),
                methodName));
        }

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
    }
}
