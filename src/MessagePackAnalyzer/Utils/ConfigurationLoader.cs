// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePackAnalyzer
{
    public static class ConfigurationLoader
    {
        public static System.Collections.Generic.IReadOnlyList<string> GetAdditionalAllowTypes(this AnalyzerOptions option)
        {
            Microsoft.CodeAnalysis.AdditionalText? config = option.AdditionalFiles.FirstOrDefault(x => System.IO.Path.GetFileName(x.Path).Equals("MessagePackAnalyzer.json", StringComparison.OrdinalIgnoreCase));
            if (config != null)
            {
                try
                {
                    var l = new List<string>();
                    var raw = config.GetText()?.ToString() ?? string.Empty;
                    using (var sr = new StringReader(raw))
                    using (var tr = new TinyJsonReader(sr))
                    {
                        while (tr.Read())
                        {
                            if (tr.TokenType == TinyJsonToken.String)
                            {
                                l.Add((string)tr.Value!);
                            }
                        }
                    }

                    return l;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Can't load MessagePackAnalyzer.json:" + ex.ToString());
                    return Array.Empty<string>();
                }
            }
            else
            {
                return Array.Empty<string>();
            }
        }
    }
}
