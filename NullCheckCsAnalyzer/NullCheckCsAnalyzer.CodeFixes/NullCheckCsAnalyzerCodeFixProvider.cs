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
            var expression = root.FindNode(diagnosticSpan) as BinaryExpressionSyntax;

            var ifStatement = expression.Parent as IfStatementSyntax;
            if (ifStatement == null) {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.CodeFixTitle,
                        createChangedDocument: c => RemoveBoolNullCheck(context.Document, expression, c),
                        equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                    diagnostic);
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => RemoveIfNullCheck(context.Document, ifStatement, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Document> RemoveIfNullCheck(Document document, IfStatementSyntax ifStatement,
                CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            if (ifStatement.Condition.Kind() == SyntaxKind.EqualsExpression) {
                var newRoot = root.RemoveNode(ifStatement, SyntaxRemoveOptions.KeepNoTrivia);
                return document.WithSyntaxRoot(newRoot);
            } else {
                var newRoot = root.ReplaceNode(ifStatement,
                    ifStatement.Statement.ChildNodes().Select(it => it.WithAdditionalAnnotations(Formatter.Annotation)));

                return document.WithSyntaxRoot(newRoot);
            }
        }

        private async Task<Document> RemoveBoolNullCheck(Document document, BinaryExpressionSyntax binaryExpression,
                CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            //var currentExpression = binaryExpression as ExpressionSyntax;
            //while (currentExpression.Parent is ExpressionSyntax expression) {
            //    currentExpression = expression;
            //}

            var evaluatedType = (binaryExpression.Kind() == SyntaxKind.EqualsExpression
                ? SyntaxKind.FalseLiteralExpression
                : SyntaxKind.TrueLiteralExpression);

            //var simplifiedExpression =
            //    currentExpression.ReplaceNode(binaryExpression, SyntaxFactory.LiteralExpression(evaluatedType));

            // TODO: try to simplify boolean expressions of type (true && ...) or (false && ...)
            var newRoot = root.ReplaceNode(binaryExpression, SyntaxFactory.LiteralExpression(evaluatedType));
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
