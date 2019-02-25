using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MessagePackCompiler
{
    public class CodeGenerationWorkspace
    {
        AdhocWorkspace workspace;

        public static async Task<CodeGenerationWorkspace> CreateAsync(string csproj, CancellationToken cancellationToken)
        {
            XDocument document;
            using (var sr = new StreamReader(csproj, true))
            {
                var reader = new XmlTextReader(sr);
                reader.Namespaces = false;

                document = XDocument.Load(reader, LoadOptions.None);
            }

            // Legacy
            // <Project ToolsVersion=...>
            // New
            // <Project Sdk="Microsoft.NET.Sdk">

            var proj = document.Element("Project");

            var legacyFormat = proj.Attribute("Sdk")?.Value != "Microsoft.NET.Sdk";

            Console.WriteLine(legacyFormat);

            return new CodeGenerationWorkspace();
        }
    }

    public class ExpressionEvaluator
    {
        // $(ProjectName)$(ProjectPath)$(ProjectFileName)$(ProjectDir)

        // %(RecursiveDir)%(FileName)%(Extension)
    }

}
