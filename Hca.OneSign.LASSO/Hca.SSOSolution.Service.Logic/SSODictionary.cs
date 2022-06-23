using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hca.SSOSolution.Service.Logic
{
    public class SSODictionary
    {
        public Dictionary<string, Dictionary<string, string>> SnippetDictionary { get; set; }
        public Dictionary<string, string> SsoStringDictionary { get; set; }
    }
}
