using System;
using System.Collections.Generic;
using System.Text;

namespace BuildCli.Models
{

    internal class ParseResult
    {
        public string CommandName { get; set; }
        public Dictionary<string, string> RawParameters { get; set; }
        public bool Help { get; set; }
    }

    internal class CliCommand
    {
        public Action<Dictionary<string, object>> Invoke { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}
