using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;


namespace NullCheckCsAnalyzer {
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullCheckCsAnalyzerCodeFixProvider)), Shared]
    public class NullCheckCsAnalyzerCodeFixProvider : CodeFixProvider {
        private static readonly HashSet<SyntaxKind> FalseNullChecks = new HashSet<SyntaxKind> {
            SyntaxKind.EqualsExpression, SyntaxKind.IsPatternExpression, SyntaxKind.InvocationExpression
        };

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NullCheckCsAnalyzerAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var expression = root.FindNode(diagnosticSpan);

            switch (expression.Kind()) {
                case SyntaxKind.ConditionalAccessExpression:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.NullPropagationCodeFixTitle,
                            createChangedDocument: c => RemoveConditionalMemberAccess(context.Document, expression as ConditionalAccessExpressionSyntax, c),
                            equivalenceKey: nameof(CodeFixResources.NullPropagationCodeFixTitle)),
                        diagnostic);
                    return;

                case SyntaxKind.CoalesceExpression:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.NullCheckCodeFixTitle,
                            createChangedDocument: c => RemoveCoalesceExpression(context.Document, expression as BinaryExpressionSyntax, c),
                            equivalenceKey: nameof(CodeFixResources.NullCoalesceCodeFixTitle)),
                        diagnostic);
                    return;

                case SyntaxKind.CoalesceAssignmentExpression:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.NullCheckCodeFixTitle,
                            createChangedDocument: c => RemoveCoalesceAssignment(context.Document, expression as AssignmentExpressionSyntax, c),
                            equivalenceKey: nameof(CodeFixResources.NullCoalesceCodeFixTitle)),
                        diagnostic);
                    return;
            }

            var expressionParent = expression.Parent;
            while (expressionParent != null &&  expressionParent.Kind() == SyntaxKind.ParenthesizedExpression) {
                expressionParent = expressionParent.Parent;
            }

            switch (expressionParent?.Kind()) {
                case SyntaxKind.IfStatement:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.NullCheckCodeFixTitle,
                            createChangedDocument: c => RemoveIfNullCheck(context.Document, expressionParent as IfStatementSyntax, c),
                            equivalenceKey: nameof(CodeFixResources.NullCheckCodeFixTitle)),
                        diagnostic);
                    break;

                case SyntaxKind.ConditionalExpression:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.NullCheckCodeFixTitle,
                            createChangedDocument: c => RemoveConditionalNullCheck(context.Document, expressionParent as ConditionalExpressionSyntax, c),
                            equivalenceKey: nameof(CodeFixResources.NullCheckCodeFixTitle)),
                        diagnostic);
                    break;

                default:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.NullCheckCodeFixTitle,
                            createChangedDocument: c => RemoveBoolNullCheck(context.Document, expression, c),
                            equivalenceKey: nameof(CodeFixResources.NullCheckCodeFixTitle)),
                        diagnostic);
                    break;
            }
        }

        private async Task<Document> RemoveIfNullCheck(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxNode newRoot;
            if (FalseNullChecks.Contains(ifStatement.Condition.Kind())) {
                newRoot = root.RemoveNode(ifStatement, SyntaxRemoveOptions.KeepNoTrivia);
            } else {
                newRoot = root.ReplaceNode(ifStatement,
                    ifStatement.Statement.ChildNodes().Select(it => it.WithAdditionalAnnotations(Formatter.Annotation)));
            }

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RemoveBoolNullCheck(Document document, SyntaxNode nullCheckExpression, CancellationToken cancellationToken) {
            // TODO: try to simplify boolean expressions of type (true && ...) or (false && ...)
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var evaluatedType = (FalseNullChecks.Contains(nullCheckExpression.Kind())
                ? SyntaxKind.FalseLiteralExpression
                : SyntaxKind.TrueLiteralExpression);

            var newRoot = root.ReplaceNode(nullCheckExpression, SyntaxFactory.LiteralExpression(evaluatedType));
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RemoveConditionalNullCheck(Document document, ConditionalExpressionSyntax conditionalExpression, CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newRoot = root.ReplaceNode(conditionalExpression, FalseNullChecks.Contains(conditionalExpression.Condition.Kind())
                ? conditionalExpression.WhenFalse.WithAdditionalAnnotations(Formatter.Annotation)
                : conditionalExpression.WhenTrue.WithAdditionalAnnotations(Formatter.Annotation));

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RemoveCoalesceExpression(Document document, BinaryExpressionSyntax coalesceExpression, CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(coalesceExpression, coalesceExpression.Left.WithoutTrivia());
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RemoveCoalesceAssignment(Document document, AssignmentExpressionSyntax coalesceAssignment, CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.RemoveNode(coalesceAssignment.Parent, SyntaxRemoveOptions.KeepNoTrivia);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RemoveConditionalMemberAccess(Document document, ConditionalAccessExpressionSyntax conditionalAccess, CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var questionSpan = conditionalAccess.OperatorToken.FullSpan;
            var sourceTest = conditionalAccess.ToFullString();
            var replacedExpression = SyntaxFactory.ParseExpression(
                sourceTest.Substring(0, questionSpan.Start - conditionalAccess.SpanStart) + sourceTest.Substring(questionSpan.End - conditionalAccess.SpanStart));

            var newRoot = root.ReplaceNode(conditionalAccess, replacedExpression);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
