using System;
using System.Threading.Tasks;

namespace MoonSharp.Interpreter.REPL
{
    /// <summary>
    /// This class provides a simple REPL intepreter ready to be reused in a simple way.
    /// </summary>
    public class ReplInterpreter
    {
        private string _currentCommand = string.Empty;
        private Script _script;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplInterpreter"/> class.
        /// </summary>
        /// <param name="script">The script.</param>
        public ReplInterpreter(Script script)
        {
            _script = script;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instances handle inputs starting with a "?" as a 
        /// dynamic expression to evaluate instead of script code (likely invalid)
        /// </summary>
        public bool HandleDynamicExprs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instances handle inputs starting with a "=" as a 
        /// non-dynamic expression to evaluate (just like the Lua interpreter does by default).
        /// </summary>
        public bool HandleClassicExprsSyntax { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has a pending command 
        /// </summary>
        public virtual bool HasPendingCommand => _currentCommand.Length > 0;

        /// <summary>
        /// Gets the current pending command.
        /// </summary>
        public virtual string CurrentPendingCommand => _currentCommand;

        /// <summary>
        /// Gets the classic prompt (">" or ">>") given the current state of the interpreter
        /// </summary>
        public virtual string ClassicPrompt => this.HasPendingCommand ? ">>" : ">";

        /// <summary>
        /// Evaluate a REPL command.
        /// This method returns the result of the computation, or null if more input is needed for having valid code.
        /// In case of errors, exceptions are propagated to the caller.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>This method returns the result of the computation, or null if more input is needed for a computation.</returns>
        public virtual DynValue Evaluate(string input)
        {
            bool isFirstLine = !this.HasPendingCommand;

            bool forced = (input == "");

            _currentCommand += input;

            if (_currentCommand.Length == 0)
            {
                return DynValue.Void;
            }

            _currentCommand += "\n";

            try
            {
                DynValue result;

                if (isFirstLine && this.HandleClassicExprsSyntax && _currentCommand.StartsWith("="))
                {
                    _currentCommand = "return " + _currentCommand.Substring(1);
                }

                if (isFirstLine && this.HandleDynamicExprs && _currentCommand.StartsWith("?"))
                {
                    string code = _currentCommand.Substring(1);
                    var exp = _script.CreateDynamicExpression(code);
                    result = exp.Evaluate();
                }
                else
                {
                    var v = _script.LoadString(_currentCommand, null, "stdin");
                    result = _script.Call(v);
                }

                _currentCommand = "";
                return result;
            }
            catch (SyntaxErrorException ex)
            {
                if (forced || !ex.IsPrematureStreamTermination)
                {
                    _currentCommand = "";
                    ex.Rethrow();
                    throw;
                }

                return null;
            }
            catch (ScriptRuntimeException sre)
            {
                _currentCommand = "";
                sre.Rethrow();
                throw;
            }
            catch (Exception)
            {
                _currentCommand = "";
                throw;
            }
        }

        /// <summary>
        /// Asynchronously evaluates a REPL command.
        /// This method returns the result of the computation, or null if more input is needed for having valid code.
        /// In case of errors, exceptions are propagated to the caller.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>
        /// This method returns the result of the computation, or null if more input is needed for a computation.
        /// </returns>
        public Task<DynValue> EvaluateAsync(string input)
        {
            return Task.Factory.StartNew(() => this.Evaluate(input));
        }
    }
}