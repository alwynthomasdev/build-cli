using System;
using System.Collections.Generic;
using System.Text;

namespace BuildCli.Models
{
    internal abstract class ParseResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public bool Help { get; set; }
    }

    internal class RawParseResult : ParseResult
    {
        public string CommandName { get; set; }
        public Dictionary<string, string> RawParameters { get; set; }
    }

    internal class CliCommand : ParseResult
    {
        public Action<Dictionary<string, object>> Invoke { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}
