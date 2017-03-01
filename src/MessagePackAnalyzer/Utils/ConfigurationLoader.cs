using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;

namespace MessagePackAnalyzer
{
    public static class ConfigurationLoader
    {
        static readonly string[] Empty = new string[0];

        public static System.Collections.Generic.IReadOnlyList<string> GetAdditionalAllowTypes(this AnalyzerOptions option)
        {
            var config = option.AdditionalFiles.FirstOrDefault(x => System.IO.Path.GetFileName(x.Path).Equals("MessagePackAnalyzer.json", StringComparison.OrdinalIgnoreCase));
            if (config != null)
            {
                try
                {
                    var l = new List<string>();
                    var raw = config.GetText().ToString();
                    using (var sr = new StringReader(raw))
                    using (var tr = new TinyJsonReader(sr))
                    {
                        while (tr.Read())
                        {
                            if (tr.TokenType == TinyJsonToken.String)
                            {
                                l.Add(tr.Value as string);
                            }
                        }
                    }

                    return l;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Can't load MessagePackAnalyzer.json:" + ex.ToString());
                    return Empty;
                }
            }
            else
            {
                return Empty;
            }
        }
    }
}