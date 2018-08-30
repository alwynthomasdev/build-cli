using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BuildCli.Models
{
    /// <summary>
    /// Defines a command parameter, how it is parsed and validated
    /// </summary>
    public class CliParameter
    {
        #region CTOR

        public CliParameter()
        {
            //define defaults...
            DataType = typeof(string);
            Aliases = new string[] { };
            //_Validator = DefaultValidator;
            _Validator = (str) =>
            {
                //Default validation works simply by attempting to convert the value from a string to the target data type
                try
                {
                    var converter = TypeDescriptor.GetConverter(DataType);
                    if (converter != null)
                    {
                        object obj = converter.ConvertFromString(str);
                        return true;
                    }
                }
                catch (Exception)
                {
                    //do nothing
                }
                return false;
            };

        }

        #endregion

        Func<string, bool> _Validator;
        string _ValidatorErrorMessage;

        /// <summary>
        /// The name that represents this parameter
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The parameter can be identified with short hand expressions defined here
        /// </summary>
        public IEnumerable<string> Aliases { get; set; }
        /// <summary>
        /// Defaults to string, use if the parameter needs to be validated against a data type
        /// </summary>
        public Type DataType { get; set; }
        /// <summary>
        /// For help/documentation
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// If parameters are not named the parser will match the parameters by the order they are entered (using the ordinal field)
        /// </summary>
        public int Ordinal { get; set; }
        /// <summary>
        /// Customizable error message used when parameter validation fails (can be conditionally overridden by the validator)
        /// </summary>
        public string ValidatorErrorMessage
        {
            get
            {
                return _ValidatorErrorMessage ?? DefaultValidatorErrorMessage();
            }
            set
            {
                _ValidatorErrorMessage = value;
            }
        }
        /// <summary>
        /// Function used to ensure the parameter value is valid
        /// </summary>
        public Func<string, bool> Validator
        {
            get
            {
                return _Validator;
            }
        }
        /// <summary>
        /// Override the parameters default validator with your own, eg ranges, regex etc
        /// </summary>
        public Func<string, bool> CustomValidator
        {
            set
            {
                _Validator = value;
            }
        }

        #region Private Methods

        //bool DefaultValidator(string str)
        //{
        //    try
        //    {
        //        var converter = TypeDescriptor.GetConverter(DataType);
        //        if (converter != null)
        //        {
        //            object obj = converter.ConvertFromString(str);
        //            return true;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        //do noting
        //    }
        //    return false;
        //}

        string DefaultValidatorErrorMessage()
        {
            return $"{Name} could not be parsed as type {DataType.ToString()}.";
        }

        #endregion

    }
}
