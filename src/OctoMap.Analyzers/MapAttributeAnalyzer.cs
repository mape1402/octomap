using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OctoMap.Analyzers
{
    /// <summary>
    /// Analyzes OctoMap map attributes for invalid declarations.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MapAttributeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor SelfReferencingMapAttributeRule = new(
            OctoMapDiagnosticIds.SelfReferencingMapAttribute,
            "Map attribute points to the declaring type",
            "Map attribute on '{0}' points to the same type and will create a self map",
            "OctoMap.Configuration",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "MapFromAttribute and MapToAttribute should point to a different related model type.");

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(SelfReferencingMapAttributeRule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        }

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            var attribute = (AttributeSyntax)context.Node;
            var attributeType = context.SemanticModel.GetTypeInfo(attribute).Type;
            if (attributeType == null
                || (attributeType.Name != "MapFromAttribute" && attributeType.Name != "MapToAttribute")
                || attributeType.ContainingNamespace.ToDisplayString() != "OctoMap")
            {
                return;
            }

            if (attribute.Parent?.Parent is not TypeDeclarationSyntax declaringTypeSyntax)
            {
                return;
            }

            var declaringType = context.SemanticModel.GetDeclaredSymbol(declaringTypeSyntax);
            var relatedType = GetRelatedType(context, attribute);
            if (declaringType == null || relatedType == null)
            {
                return;
            }

            if (SymbolEqualityComparer.Default.Equals(declaringType, relatedType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SelfReferencingMapAttributeRule,
                    attribute.GetLocation(),
                    declaringType.Name));
            }
        }

        private static ITypeSymbol GetRelatedType(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
        {
            var firstArgument = attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
            if (firstArgument is TypeOfExpressionSyntax typeOfExpression)
            {
                return context.SemanticModel.GetTypeInfo(typeOfExpression.Type).Type;
            }

            return null;
        }
    }
}
