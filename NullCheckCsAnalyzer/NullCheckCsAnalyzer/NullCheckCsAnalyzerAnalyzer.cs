using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NullCheckCsAnalyzer {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NullCheckCsAnalyzerAnalyzer : DiagnosticAnalyzer {
        public const string DiagnosticId = "NullCheckCsAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            //context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
            //context.RegisterOperationAction(AnalyzeEqualsOperation, OperationKind.Binary);
            context.RegisterSyntaxNodeAction(AnalyzeEqualsExpression, SyntaxKind.EqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeEqualsExpression, SyntaxKind.NotEqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeConditionalAccessExpression, SyntaxKind.ConditionalAccessExpression);
        }

        private static void AnalyzeEqualsExpression(SyntaxNodeAnalysisContext context) {
            var binaryExpression = context.Node as BinaryExpressionSyntax;
            IdentifierNameSyntax identifierName = null;

            if (binaryExpression.Left.Kind() == SyntaxKind.NullLiteralExpression) {
                identifierName = binaryExpression.Right as IdentifierNameSyntax;
            }

            if (binaryExpression.Right.Kind() == SyntaxKind.NullLiteralExpression) {
                identifierName = binaryExpression.Left as IdentifierNameSyntax;
            }

            if (identifierName == null
                || context.SemanticModel.GetTypeInfo(identifierName).Nullability.Annotation != NullableAnnotation.NotAnnotated) {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, binaryExpression.GetLocation(), binaryExpression.Left);
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeConditionalAccessExpression(SyntaxNodeAnalysisContext context) {
            var conditionalAccess = context.Node as ConditionalAccessExpressionSyntax;
            if (context.SemanticModel.GetTypeInfo(conditionalAccess.Expression).Nullability.Annotation !=
                NullableAnnotation.NotAnnotated) {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, conditionalAccess.GetLocation(), conditionalAccess.Expression);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
