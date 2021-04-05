using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace NullCheckCsAnalyzer {
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullCheckCsAnalyzerCodeFixProvider)), Shared]
    public class NullCheckCsAnalyzerCodeFixProvider : CodeFixProvider {
        public sealed override ImmutableArray<string> FixableDiagnosticIds {
            get { return ImmutableArray.Create(NullCheckCsAnalyzerAnalyzer.DiagnosticId); }
        }

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
                    //context.RegisterCodeFix(
                    //    CodeAction.Create(
                    //        title: CodeFixResources.CodeFixTitle,
                    //        createChangedDocument: c => RemoveConditionalMemberAccess(context.Document, expression as ConditionalAccessExpressionSyntax, c),
                    //        equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                    //    diagnostic);
                    return;
                case SyntaxKind.CoalesceExpression:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.CodeFixTitle,
                            createChangedDocument: c => RemoveCoalesceExpression(context.Document, expression as BinaryExpressionSyntax, c),
                            equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                        diagnostic);
                    return;
                case SyntaxKind.CoalesceAssignmentExpression:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.CodeFixTitle,
                            createChangedDocument: c => RemoveCoalesceAssignment(context.Document, expression as AssignmentExpressionSyntax, c),
                            equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                        diagnostic);
                    return;
            }

            if (expression.Kind() == SyntaxKind.ConditionalAccessExpression) {
                
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
                            title: CodeFixResources.CodeFixTitle,
                            createChangedDocument: c => RemoveIfNullCheck(context.Document, expressionParent as IfStatementSyntax, c),
                            equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                        diagnostic);
                    break;
                case SyntaxKind.ConditionalExpression:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.CodeFixTitle,
                            createChangedDocument: c => RemoveConditionalNullCheck(context.Document, expressionParent as ConditionalExpressionSyntax, c),
                            equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                        diagnostic);
                    break;
                default:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.CodeFixTitle,
                            createChangedDocument: c => RemoveBoolNullCheck(context.Document, expression, c),
                            equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                        diagnostic);
                    break;
            }
        }

        private async Task<Document> RemoveIfNullCheck(Document document, IfStatementSyntax ifStatement,
                CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxNode newRoot;
            if (ifStatement.Condition.Kind() == SyntaxKind.EqualsExpression || ifStatement.Condition.Kind() == SyntaxKind.IsPatternExpression || ifStatement.Condition.Kind() == SyntaxKind.InvocationExpression) {
                newRoot = root.RemoveNode(ifStatement, SyntaxRemoveOptions.KeepNoTrivia);
            } else {
                newRoot = root.ReplaceNode(ifStatement,
                    ifStatement.Statement.ChildNodes().Select(it => it.WithAdditionalAnnotations(Formatter.Annotation)));
            }

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RemoveBoolNullCheck(Document document, SyntaxNode nullCheckExpression,
                CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            //var currentExpression = binaryExpression as ExpressionSyntax;
            //while (currentExpression.Parent is ExpressionSyntax expression) {
            //    currentExpression = expression;
            //}

            var evaluatedType = (nullCheckExpression.Kind() == SyntaxKind.EqualsExpression || nullCheckExpression.Kind() == SyntaxKind.IsPatternExpression || nullCheckExpression.Kind() == SyntaxKind.InvocationExpression
                ? SyntaxKind.FalseLiteralExpression
                : SyntaxKind.TrueLiteralExpression);

            //var simplifiedExpression =
            //    currentExpression.ReplaceNode(binaryExpression, SyntaxFactory.LiteralExpression(evaluatedType));

            // TODO: try to simplify boolean expressions of type (true && ...) or (false && ...)
            var newRoot = root.ReplaceNode(nullCheckExpression, SyntaxFactory.LiteralExpression(evaluatedType));
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RemoveConditionalNullCheck(Document document, ConditionalExpressionSyntax conditionalExpression,
                CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            SyntaxNode newRoot;
            if (conditionalExpression.Condition.Kind() == SyntaxKind.EqualsExpression) {
                newRoot = root.ReplaceNode(conditionalExpression,
                    conditionalExpression.WhenFalse.WithAdditionalAnnotations(Formatter.Annotation));
            } else {
                newRoot = root.ReplaceNode(conditionalExpression,
                    conditionalExpression.WhenTrue.WithAdditionalAnnotations(Formatter.Annotation));
            }

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RemoveCoalesceExpression(Document document,
                BinaryExpressionSyntax coalesceExpression, CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(coalesceExpression, coalesceExpression.Left.WithoutTrivia());
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RemoveCoalesceAssignment(Document document,
                AssignmentExpressionSyntax coalesceAssignment, CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.RemoveNode(coalesceAssignment.Parent, SyntaxRemoveOptions.KeepNoTrivia);
            return document.WithSyntaxRoot(newRoot);
        }


        // TODO: make codefix for this
        //private async Task<Document> RemoveConditionalMemberAccess(Document document,
        //        ConditionalAccessExpressionSyntax conditionalAccess, CancellationToken cancellationToken) {

        //    return document.WithSyntaxRoot(newRoot);
        //}
    }
}
