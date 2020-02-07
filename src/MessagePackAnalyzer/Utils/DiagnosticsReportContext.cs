// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePackAnalyzer
{
    // Store multiple errors.
    internal class DiagnosticsReportContext
    {
        private readonly List<Diagnostic> diagnostics = new List<Diagnostic>();
        private readonly SyntaxNodeAnalysisContext context;

        public IReadOnlyList<Diagnostic> Diagnostics => this.diagnostics;

        public IReadOnlyList<string> AdditionalAllowTypes { get; }

        public DiagnosticsReportContext(SyntaxNodeAnalysisContext context)
        {
            this.context = context;
            this.AdditionalAllowTypes = this.context.Options.GetAdditionalAllowTypes();
        }

        public void Add(Diagnostic diagnostic)
        {
            this.diagnostics.Add(diagnostic);
        }

        public void ReportAll()
        {
            foreach (Diagnostic item in this.diagnostics)
            {
                this.context.ReportDiagnostic(item);
            }
        }
    }
}
