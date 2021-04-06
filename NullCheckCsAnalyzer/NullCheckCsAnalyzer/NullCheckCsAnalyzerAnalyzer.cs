﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NullCheckCsAnalyzer {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NullCheckCsAnalyzerAnalyzer : DiagnosticAnalyzer {
        public const string DiagnosticId = "NullAnalyzer";

        private static readonly LocalizableString NullCheckTitle = new LocalizableResourceString(nameof(Resources.NullCheckTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString NullCheckMessageFormat = new LocalizableResourceString(nameof(Resources.NullCheckMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString NullCheckDescription = new LocalizableResourceString(nameof(Resources.NullCheckDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString NullCoalesceTitle = new LocalizableResourceString(nameof(Resources.NullCoalesceTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString NullCoalesceMessageFormat = new LocalizableResourceString(nameof(Resources.NullCoalesceMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString NullCoalesceDescription = new LocalizableResourceString(nameof(Resources.NullCoalesceDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString NullPropagationTitle = new LocalizableResourceString(nameof(Resources.NullPropagationTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString NullPropagationMessageFormat = new LocalizableResourceString(nameof(Resources.NullPropagationMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString NullPropagationDescription = new LocalizableResourceString(nameof(Resources.NullPropagationDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = "Usage";

        public static readonly DiagnosticDescriptor NullCheckRule = new DiagnosticDescriptor(DiagnosticId, NullCheckTitle, NullCheckMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: NullCheckDescription);
        public static readonly DiagnosticDescriptor NullCoalesceRule = new DiagnosticDescriptor(DiagnosticId, NullCoalesceTitle, NullCoalesceMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: NullCoalesceDescription);
        public static readonly DiagnosticDescriptor NullPropagationRule = new DiagnosticDescriptor(DiagnosticId, NullPropagationTitle, NullPropagationMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: NullPropagationDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(NullCheckRule, NullCoalesceRule, NullPropagationRule); } }

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeEqualsExpression, SyntaxKind.EqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeEqualsExpression, SyntaxKind.NotEqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeConditionalAccessExpression, SyntaxKind.ConditionalAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIsNull, SyntaxKind.IsPatternExpression);
            context.RegisterSyntaxNodeAction(AnalyzeCoalesceOperator, SyntaxKind.CoalesceExpression);
            context.RegisterSyntaxNodeAction(AnalyzeCoalesceAssignment, SyntaxKind.CoalesceAssignmentExpression);
            context.RegisterSyntaxNodeAction(AnalyzeEqualsInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeEqualsExpression(SyntaxNodeAnalysisContext context) {
            var binaryExpression = context.Node as BinaryExpressionSyntax;
            SyntaxNode argument = null;

            if (binaryExpression.Left.Kind() == SyntaxKind.NullLiteralExpression) {
                argument = binaryExpression.Right;
            }

            if (binaryExpression.Right.Kind() == SyntaxKind.NullLiteralExpression) {
                argument = binaryExpression.Left;
            }

            if (argument == null || context.SemanticModel.GetTypeInfo(argument).Nullability.Annotation != NullableAnnotation.NotAnnotated) {
                return;
            }

            var diagnostic = Diagnostic.Create(NullCheckRule, binaryExpression.GetLocation(), argument);
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeConditionalAccessExpression(SyntaxNodeAnalysisContext context) {
            var conditionalAccess = context.Node as ConditionalAccessExpressionSyntax;
            if (context.SemanticModel.GetTypeInfo(conditionalAccess.Expression).Nullability.Annotation != NullableAnnotation.NotAnnotated) {
                return;
            }

            var diagnostic = Diagnostic.Create(NullPropagationRule, conditionalAccess.GetLocation(), conditionalAccess.Expression);
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeIsNull(SyntaxNodeAnalysisContext context) {
            var isNullExpression = context.Node as IsPatternExpressionSyntax;
            if (context.SemanticModel.GetTypeInfo(isNullExpression.Expression as IdentifierNameSyntax).Nullability.Annotation != NullableAnnotation.NotAnnotated
                || !(isNullExpression.Pattern is ConstantPatternSyntax constantPattern && constantPattern.Expression.Kind() == SyntaxKind.NullLiteralExpression)) {
                return;
            }

            var diagnostic = Diagnostic.Create(NullCheckRule, isNullExpression.GetLocation(), isNullExpression.Expression);
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeCoalesceOperator(SyntaxNodeAnalysisContext context) {
            var coalesceOperator = context.Node as BinaryExpressionSyntax;
            if (context.SemanticModel.GetTypeInfo(coalesceOperator.Left).Nullability.Annotation != NullableAnnotation.NotAnnotated) {
                return;
            }

            var diagnostic = Diagnostic.Create(NullCoalesceRule, coalesceOperator.GetLocation(), coalesceOperator.Left);
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeCoalesceAssignment(SyntaxNodeAnalysisContext context) {
            var coalesceAssignment = context.Node as AssignmentExpressionSyntax;
            if (context.SemanticModel.GetTypeInfo(coalesceAssignment.Left).Nullability.Annotation != NullableAnnotation.NotAnnotated) {
                return;
            }

            var diagnostic = Diagnostic.Create(NullCoalesceRule, coalesceAssignment.GetLocation(), coalesceAssignment.Left);
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeEqualsInvocation(SyntaxNodeAnalysisContext context) {
            var equalsInvocation = context.Node as InvocationExpressionSyntax;
            SyntaxNode argument = null;

            switch (equalsInvocation.Expression) {
                case MemberAccessExpressionSyntax memberAccess: {
                    if (equalsInvocation.ArgumentList.Arguments.Count == 1 && memberAccess.Name.Identifier.Text == "Equals"
                        && equalsInvocation.ArgumentList.Arguments[0].Expression.Kind() == SyntaxKind.NullLiteralExpression) {
                        argument = memberAccess.Expression;

                    } else if (equalsInvocation.ArgumentList.Arguments.Count == 2 && memberAccess.Name.Identifier.Text == "ReferenceEquals") {
                        if (equalsInvocation.ArgumentList.Arguments[0].Expression.Kind() == SyntaxKind.NullLiteralExpression) {
                            argument = equalsInvocation.ArgumentList.Arguments[1].Expression;
                        } 
                        
                        if (equalsInvocation.ArgumentList.Arguments[1].Expression.Kind() == SyntaxKind.NullLiteralExpression) {
                            argument = equalsInvocation.ArgumentList.Arguments[0].Expression;
                        }
                    }
                    break;
                }
                case IdentifierNameSyntax methodName: {
                    if (equalsInvocation.ArgumentList.Arguments.Count != 2 || methodName.Identifier.Text != "ReferenceEquals") {
                        return;
                    }

                    if (equalsInvocation.ArgumentList.Arguments[0].Expression.Kind() == SyntaxKind.NullLiteralExpression) {
                        argument = equalsInvocation.ArgumentList.Arguments[1].Expression;
                    }

                    if (equalsInvocation.ArgumentList.Arguments[1].Expression.Kind() == SyntaxKind.NullLiteralExpression) {
                        argument = equalsInvocation.ArgumentList.Arguments[0].Expression;
                    }

                    break;
                }
            }

            if (argument == null || context.SemanticModel.GetTypeInfo(argument).Nullability.Annotation != NullableAnnotation.NotAnnotated) {
                return;
            }

            var diagnostic = Diagnostic.Create(NullCheckRule, equalsInvocation.GetLocation(), argument);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
