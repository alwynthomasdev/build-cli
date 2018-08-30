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
        /// Parse and invoke a command with all its parts in a string array. Intended to be used strait from the console/terminal where command args are passed to the program as args.
        /// </summary>
        /// <param name="commandArgs">The command details as an array of strings</param>
        /// <param name="onParseError">How should parse errors be handled</param>
        public void Invoke(string[] commandArgs, Action<string> onParseError)
        {
            CliCommand cmd = Parse(commandArgs);
            if (!cmd.Success) onParseError(cmd.Error);
            else cmd.Invoke(cmd.Parameters);
        }

        /// <summary>
        /// Parse and invoke a command that is in the form as a string. Intended for use when the application is reading input from the console/terminal.
        /// </summary>
        /// <param name="commandString">The command string to parse</param>
        /// <param name="onParseError">How should parse errors be handled</param>
        public void Invoke(string commandString, Action<string> onParseError)
        {
            CliCommand cmd = Parse(commandString);
            if (!cmd.Success) onParseError(cmd.Error);
            else cmd.Invoke(cmd.Parameters);
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

        CliCommand Parse(string[] cmdAry)
        {
            RawParseResult parseResult = ParseRaw(cmdAry);
            return Parse(parseResult);
        }
        CliCommand Parse(string cmdString)
        {
            RawParseResult parseResult = ParseRaw(cmdString);
            return Parse(parseResult);
        }
        CliCommand Parse(RawParseResult parseResult)
        {
            if (parseResult.Success)
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
                                return new CliCommand { Success = false, Error = cliParam.ValidatorErrorMessage };
                            }
                            realParams[cliParam.Name] = rawParams[k];
                        }
                        else
                        {
                            int i = 0;
                            return new CliCommand { Success = false, Error = $"Command '{cmdDef.Name}' has no parameter defined {(int.TryParse(k, out i) ? $"at postion {i}" : $"'{k}'")}." };
                        }
                    }
                    return new CliCommand { Success = true, Invoke = cmdDef.Command, Parameters = realParams };
                }
                else return new CliCommand { Success = false, Error = $"Command '{parseResult.CommandName}' not found." };
            }
            else return new CliCommand { Success = false, Error = parseResult.Error };
        }

        RawParseResult ParseRaw(string[] ary)
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
                        return new RawParseResult { CommandName = "help", Help = true, Success = true };
                    }
                }
                else if (i == 1 && ary[i].ToLower().Replace("-", "") == "help")
                {
                    return new RawParseResult { CommandName = cmdName, Help = true, Success = true };
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
                            return new RawParseResult { Success = false, Error = $"Unable to read parameter name." };
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
            return new RawParseResult { Success = true, CommandName = cmdName, RawParameters = cmdParams };
        }

        RawParseResult ParseRaw(string cmdString)
        {
            if (!string.IsNullOrWhiteSpace(cmdString))
            {
                string[] parts = cmdString.Split(' ');
                return ParseRaw(parts);
            }
            else
            {
                return new RawParseResult { Success = false, Error = $"Failed to read command." };
            }
        }

        CliCommand GenerateHelpCommand()
        {
            if (HelpCommand == null) throw new NotImplementedException("A help command has not been defined.");

            CliCommand cmd = new CliCommand();
            cmd.Help = true;
            cmd.Success = true;

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
            cmd.Help = true;
            cmd.Success = true;

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
}
