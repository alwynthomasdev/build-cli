using BuildCli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuildCli
{
    public class CliManager
    {
        #region CTOR
        public CliManager()
        {
            _CommandCollection = new List<CliCommandDefinition>();
        }
        #endregion

        /// <summary>
        /// Overall help text for the CLI tool, used when generating help/documentation
        /// </summary>
        public string CliDescriptionText { get; set; }
        /// <summary>
        /// Define a function for displaying the help/documentation
        /// </summary>
        public Action<string> HelpCommand { get; set; }
        /// <summary>
        /// Define a function to handle how simple errors such as command parse errors are handled and displayed (will throw BuildCliException otherwise)
        /// </summary>
        public Action<string> HandleErrorCommand { get; set; }
        /// <summary>
        /// Define a function to handle exceptions that are thrown during parse, build and execution of commands.
        /// </summary>
        public Action<Exception> HandleExceptionCommand { get; set; }

        /// <summary>
        /// Parse and run a command with all its parts in a string array. Intended to be used strait from the console/terminal where command args are passed to the program as args.
        /// </summary>
        /// <param name="commandArgs">The command details as an array of strings</param>
        public void Run(string[] commandArgs)
        {
            try
            {
                ParseResult result = Parse(commandArgs);
                CliCommand cmd = null;
                if (!string.IsNullOrWhiteSpace(result.Error))
                    cmd = Build(result);
                else cmd = GenerateErrorCommand(result.Error);
                cmd.Invoke(cmd.Parameters);
            }
            catch (Exception ex)
            {
                if (HandleExceptionCommand == null) throw;
                HandleExceptionCommand(ex);
            }
        }

        /// <summary>
        /// Parse and run a command that is in the form as a string. Intended for use when the application is reading input from the console/terminal.
        /// </summary>
        /// <param name="commandString">The command string to parse</param>
        public void Run(string commandString)
        {
            try
            {
                ParseResult result = Parse(commandString);
                CliCommand cmd = null;
                if (!string.IsNullOrWhiteSpace(result.Error))
                    cmd = Build(result);
                else cmd = GenerateErrorCommand(result.Error);
                cmd.Invoke(cmd.Parameters);
            }
            catch(Exception ex)
            {
                if (HandleExceptionCommand == null) throw;
                HandleExceptionCommand(ex);
            }
        }

        /// <summary>
        /// Add a command definition to the manager so that the manager can invoke those commands against parsed input
        /// </summary>
        public void AddCommandDefinition(CliCommandDefinition cmdDefinition)
        {
            List<string> commandNames = new List<string>();
            commandNames.AddRange(_CommandCollection.Select(x => x.Name));
            commandNames.AddRange(_CommandCollection.SelectMany(x => x.Aliases));

            if (!commandNames.Contains(cmdDefinition.Name.ToLower()))
            {
                cmdDefinition.Name = cmdDefinition.Name.ToLower();

                List<string> paramNames = new List<string>();
                paramNames.AddRange(cmdDefinition.Parameters.Select(x => x.Name));
                paramNames.AddRange(cmdDefinition.Parameters.SelectMany(x => x.Aliases));

                foreach (string pn in paramNames)
                {
                    if (paramNames.Where(x => x == pn).Count() > 1)
                        throw new Exception($"Command definition '{cmdDefinition.Name}' has duplicate parameter name '{pn}'.");
                }

                _CommandCollection.Add(cmdDefinition);
            }
            else throw new Exception($"Duplicate command definition '{cmdDefinition.Name.ToLower()}'.");
        }

        #region Privates

        List<CliCommandDefinition> _CommandCollection;

        CliCommand Build(ParseResult parseResult)
        {
            if (parseResult.CommandName == "help")
                return GenerateHelpCommand();

            CliCommandDefinition cmdDef = _CommandCollection.Where(c => c.Name == parseResult.CommandName.ToLower() || c.Aliases.Contains(parseResult.CommandName.ToLower())).SingleOrDefault();

            if (parseResult.Help)
            {
                return GenerateHelpCommand(cmdDef);
            }

            Dictionary<string, string> rawParams = parseResult.RawParameters;
            Dictionary<string, object> realParams = new Dictionary<string, object>();

            if (cmdDef != null)
            {
                foreach (string k in rawParams.Keys)
                {
                    CliParameter cliParam = cmdDef.Parameters.Where(d => d.Name.ToLower() == k.ToLower() || d.Aliases.Contains(k.ToLower()) || d.Ordinal.ToString() == k).FirstOrDefault();
                    if (cliParam != null)
                    {
                        if (!cliParam.Validator(rawParams[k]))
                        {
                            return GenerateErrorCommand(cliParam.ValidatorErrorMessage);
                        }
                        realParams[cliParam.Name] = rawParams[k];
                    }
                    else
                    {
                        int i = 0;
                        return GenerateErrorCommand($"Command '{cmdDef.Name}' has no parameter defined {(int.TryParse(k, out i) ? $"at postion {i}" : $"'{k}'")}.");
                    }
                }
                return new CliCommand { Invoke = cmdDef.Command, Parameters = realParams };
            }
            else return GenerateErrorCommand($"Command '{parseResult.CommandName}' not found.");
        }

        ParseResult Parse(string[] ary)
        {
            string cmdName = "";
            Dictionary<string, string> cmdParams = new Dictionary<string, string>();

            bool readingParam = false;
            string curParamName = "";

            for (int i = 0; i < ary.Length; i++)
            {
                if (i == 0)//this is the command name
                {
                    cmdName = ary[i];
                    if (cmdName.ToLower() == "help")
                    {
                        return new ParseResult { CommandName = "help", Help = true };
                    }
                }
                else if (i == 1 && ary[i].ToLower().Replace("-", "") == "help")
                {
                    return new ParseResult { CommandName = cmdName, Help = true };
                }
                else
                {
                    string x = ary[i];
                    //read the value as the parameter
                    if (readingParam && x.Substring(0, 1) != "-")
                    {
                        cmdParams[curParamName] = x ?? "";
                        readingParam = false;
                        continue;
                    }
                    //else there is no data, no value for this params (useful for params that represent flags)
                    else if (readingParam && x.Substring(0, 1) == "-")
                    {
                        cmdParams[curParamName] = "";
                        readingParam = false;
                    }

                    if (x.Substring(0, 1) == "-")//check is param name
                    {
                        readingParam = true;
                        x = x.Replace("-", "");
                        if (string.IsNullOrWhiteSpace(x))
                        {
                            return new ParseResult { Error = $"Unable to read parameter name." };
                        }
                        else
                        {
                            curParamName = x;
                        }
                    }
                    else//its a nameless param
                    {
                        int o = cmdParams.Keys.Count + 1;
                        cmdParams[o.ToString()] = x;
                    }

                }
            }
            return new ParseResult { CommandName = cmdName, RawParameters = cmdParams };
        }

        ParseResult Parse(string cmdString)
        {
            if (!string.IsNullOrWhiteSpace(cmdString))
            {
                string[] parts = cmdString.Split(' ');
                return Parse(parts);
            }
            else return new ParseResult { Error = $"Failed to read command." };
        }

        CliCommand GenerateHelpCommand()
        {
            if (HelpCommand == null) throw new NotImplementedException("A help command has not been defined.");

            CliCommand cmd = new CliCommand();

            Dictionary<string, object> param = new Dictionary<string, object>();
            param["HelpText"] = GenerateHelpText();
            cmd.Parameters = param;
            cmd.Invoke = (p) => HelpCommand(p["HelpText"].ToString());

            return cmd;
        }

        CliCommand GenerateHelpCommand(CliCommandDefinition cmdDef)
        {
            if (HelpCommand == null) throw new NotImplementedException("A help command has not been defined.");

            CliCommand cmd = new CliCommand();

            Dictionary<string, object> param = new Dictionary<string, object>();
            param["HelpText"] = GenerateHelpText(cmdDef);
            cmd.Parameters = param;
            cmd.Invoke = (p) => HelpCommand(p["HelpText"].ToString());

            return cmd;
        }

        string GenerateHelpText()
        {
            string txt = "";
            if (!string.IsNullOrWhiteSpace(CliDescriptionText))
                txt += $"\nDescription: {CliDescriptionText}\n\n";

            txt += "Commands:\n";
            foreach (var cmd in _CommandCollection)
            {
                string p = "";
                if (cmd.Parameters.Count > 0)
                {
                    foreach (string s in cmd.Parameters.Select(x => x.Name))
                    {
                        if (p == "") p += "-" + s;
                        else p += ", -" + s;
                    }
                }
                txt += $"\n{cmd.Name} {p}\n";
            }

            txt += "\nFor more help on the usage of individual commands enter the name of the command followed by '-help'.";
            return txt;
        }

        string GenerateHelpText(CliCommandDefinition cmdDef)
        {
            string txt = "";
            txt += $"\nCommand: {cmdDef.Name}\n";
            if (cmdDef.Aliases.Count() > 0)
                txt += $"Aliases: {CommaString(cmdDef.Aliases)}\n";
            if (!string.IsNullOrWhiteSpace(cmdDef.Description))
                txt += $"Description: {cmdDef.Description}\n";

            if (cmdDef.Parameters.Count() > 0)
            {
                txt += "\nParameters:\n";
                foreach (CliParameter param in cmdDef.Parameters)
                {
                    txt += $"\n\tParameter: {param.Name}\n";
                    txt += $"\tData Type: {param.DataType.Name}\n";
                    if (param.Aliases.Count() > 0)
                        txt += $"\tAliases: {CommaString(param.Aliases)}\n";
                    if (!string.IsNullOrWhiteSpace(param.Description))
                        txt += $"\tDescription: {param.Description}\n";
                }
            }

            return txt;
        }

        CliCommand GenerateErrorCommand(string err)
        {
            if (HandleErrorCommand == null) throw new BuildCliException(err);

            CliCommand cmd = new CliCommand();
            Dictionary<string, object> param = new Dictionary<string, object>();
            param["Error"] = err;
            cmd.Parameters = param;
            cmd.Invoke = (p) => HandleErrorCommand(p["Error"].ToString());
            return cmd;
        }

        CliCommand GenerateExceptionCommand(Exception ex)
        {
            CliCommand cmd = new CliCommand();
            Dictionary<string, object> param = new Dictionary<string, object>();
            param["Exception"] = ex;
            cmd.Parameters = param;
            cmd.Invoke = (p) => HandleExceptionCommand((Exception)p["Exception"]);
            return cmd;
        }

        string CommaString(IEnumerable<string> lst)
        {
            string r = "";
            foreach (string s in lst)
            {
                if (r == "") r += s;
                else r += ", " + s;
            }
            return r;
        }

        #endregion

    }

    public class BuildCliException : Exception
    {
        public BuildCliException(string message) : base(message) { }
        public BuildCliException(string message, Exception innerEx) : base(message, innerEx) { }
    }

}
