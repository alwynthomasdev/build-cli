using System;
using System.Collections.Generic;
using System.Text;

namespace BuildCli.Models
{
    /// <summary>
    /// Define a CLI command along with the expected parameters and description for generating help documentation
    /// </summary>
    public class CliCommandDefinition
    {
        #region CTOR

        public CliCommandDefinition()
        {
            Parameters = new List<CliParameter>();
            Aliases = new string[] { };
        }

        #endregion

        /// <summary>
        /// The name used to identify the command
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The command can be identified with short hand expressions defined as  aliases
        /// </summary>
        public IEnumerable<string> Aliases { get; set; }//
        /// <summary>
        /// Define the parameters that can be used with this command, how they are parsed and validated
        /// </summary>
        public List<CliParameter> Parameters { get; set; }
        /// <summary>
        /// For help/documentation
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The function to be executed
        /// </summary>
        public Action<Dictionary<string, object>> Command { get; set; }
    }
}
