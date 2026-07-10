// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace MessagePack.Analyzers.CodeFixes;

internal static class LineEndingPreserver
{
    internal static async Task<Solution> NormalizeChangedDocumentsAsync(Solution originalSolution, Solution changedSolution, CancellationToken cancellationToken)
    {
        SolutionChanges solutionChanges = changedSolution.GetChanges(originalSolution);
        List<DocumentId> changedDocumentIds = new();
        foreach (ProjectChanges projectChange in solutionChanges.GetProjectChanges())
        {
            changedDocumentIds.AddRange(projectChange.GetChangedDocuments());
        }

        if (changedDocumentIds.Count == 0)
        {
            return changedSolution;
        }

        string? lineEnding = null;
        foreach (DocumentId documentId in changedDocumentIds)
        {
            Document? originalDocument = originalSolution.GetDocument(documentId);
            if (originalDocument is null)
            {
                continue;
            }

            SourceText originalText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            lineEnding = TryGetLineEnding(originalText);
            if (lineEnding is not null)
            {
                break;
            }
        }

        if (lineEnding is null)
        {
            return changedSolution;
        }

        return await NormalizeChangedDocumentsCoreAsync(changedSolution, changedDocumentIds, lineEnding, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<Document> NormalizeDocumentAsync(Document originalDocument, Document changedDocument, CancellationToken cancellationToken)
    {
        SourceText originalText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        string? lineEnding = TryGetLineEnding(originalText);
        if (lineEnding is null)
        {
            return changedDocument;
        }

        Solution normalizedSolution = changedDocument.Project.Solution.WithOptions(
            changedDocument.Project.Solution.Options.WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, lineEnding));
        return normalizedSolution.GetDocument(changedDocument.Id) ?? changedDocument;
    }

    private static string? TryGetLineEnding(SourceText text)
    {
        foreach (TextLine line in text.Lines)
        {
            if (line.SpanIncludingLineBreak.End <= line.Span.End)
            {
                continue;
            }

            return text.ToString(TextSpan.FromBounds(line.Span.End, line.SpanIncludingLineBreak.End));
        }

        return null;
    }

    private static async Task<Solution> NormalizeChangedDocumentsCoreAsync(Solution changedSolution, IReadOnlyList<DocumentId> documentIds, string lineEnding, CancellationToken cancellationToken)
    {
        Solution normalizedSolution = changedSolution.WithOptions(changedSolution.Options.WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, lineEnding));
        foreach (DocumentId documentId in documentIds)
        {
            Document? document = normalizedSolution.GetDocument(documentId);
            if (document is null)
            {
                continue;
            }

            document = await Simplifier.ReduceAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
            document = await Formatter.FormatAsync(document, normalizedSolution.Options, cancellationToken).ConfigureAwait(false);
            normalizedSolution = document.Project.Solution;
        }

        return normalizedSolution;
    }
}
