/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Avalon.Common.Interfaces;
using Avalon.Common.Models;
using MoonSharp.Interpreter;

namespace Avalon.Lua
{
    /// <summary>
    /// A class to handle the code to execute Lua from various locations in the client.
    /// </summary>
    public class LuaCaller
    {
        /// <summary>
        /// Global variables available to Lua that are shared across all of our Lua sessions.
        /// </summary>
        public LuaGlobalVariables LuaGlobalVariables { get; }

        /// <summary>
        /// A counter of the number of Lua scripts that are actively executing.
        /// </summary>
        public int ActiveLuaScripts { get; set; } = 0;

        /// <summary>
        /// The number of Lua scripts that have been executed in this session.
        /// </summary>
        public int LuaScriptsRun { get; set; } = 0;

        /// <summary>
        /// The number of Lua scripts that were run from the cache.
        /// </summary>
        public int LuaScriptsRunFromCache { get; set; } = 0;

        /// <summary>
        /// The number of Lua scripts that have had an error.
        /// </summary>
        public int LuaErrorCount { get; set; } = 0;

        /// <summary>
        /// The number of SQL command that have been run through Lua.
        /// </summary>
        public int LuaSqlCommandsRun { get; set; } = 0;

        /// <summary>
        /// A reference to the mud client's current interpreter.
        /// </summary>
        private readonly IInterpreter _interpreter;

        /// <summary>
        /// Represents a shared instance of the DynValue that holds our LuaCommands object which is CLR
        /// code that we're exposing to Lua in the "lua" namespace.
        /// </summary>
        private readonly DynValue _luaCmds;

        /// <summary>
        /// Single static Random object that will need to be locked between usages.  Calls to _random
        /// should be locked for thread safety as Random is not thread safe.
        /// </summary>
        private static Random _random;

        /// <summary>
        /// The currently/dynamically loaded CLR types that can be exposed to Lua.
        /// </summary>
        private readonly Dictionary<string, DynValue> _clrTypes = new Dictionary<string, DynValue>();

        /// <summary>
        /// A object to use for locking.
        /// </summary>
        private readonly object _lockObject = new object();

        /// <summary>
        /// A shared <see cref="Script"/> object to allow for reuse of functions.
        /// </summary>
        public Script SharedScript;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="interp"></param>
        public LuaCaller(IInterpreter interp)
        {
            _interpreter = interp;
            _random = new Random();

            this.LuaGlobalVariables = new LuaGlobalVariables();

            // The CLR types we want to expose to Lua need to be registered before UserData.Create
            // can be called.  If they're not registered UserData.Create will return a null.
            UserData.RegisterType<LuaCommands>();
            UserData.RegisterType<LuaGlobalVariables>();

            _luaCmds = UserData.Create(new LuaCommands(_interpreter, _random));

            // Setup the shared script that will allow for function reuse.
            this.SharedScript = this.CreateScript();
        }

        /// <summary>
        /// Register a CLR type for use with Lua.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="prefix"></param>
        public void RegisterType(Type t, string prefix)
        {
            // Only add the type in if it hasn't been added previously.
            if (_clrTypes.ContainsKey(prefix))
            {
                return;
            }

            // Set the actual class that has the Lua commands.
            var instance = Activator.CreateInstance(t) as ILuaCommand;

            if (instance == null)
            {
                return;
            }

            instance.Interpreter = _interpreter;

            // Register the type now that we know it has been activated and is ready.
            UserData.RegisterType(t);

            // Save the DynValue which contains the instance to the activated CLR type
            // and the prefix/namespace it should be available to lua under.
            _clrTypes.Add(prefix, UserData.Create(instance));
        }

        /// <summary>
        /// Clears the custom loaded types from LuaCaller.RegisterType.
        /// </summary>
        public void ClearTypes()
        {
            _clrTypes.Clear();
        }

        /// <summary>
        /// Sends a cancel command to the game if one is defined in the settings.  Some muds have a short
        /// circuit command that will cancel all other commands that have been inputted on the server side.
        /// </summary>
        public async Task SendCancelCommandAsync()
        {
            // Cancel pending sends with the mud in case something went haywire, fire and forget.
            if (!string.IsNullOrWhiteSpace(App.Settings.ProfileSettings.GameServerCancelCommand))
            {
                await _interpreter.Send(App.Settings.ProfileSettings.GameServerCancelCommand, true, false);
            }
        }

        /// <summary>
        /// Creates a <see cref="Script"/> with the Lua global variables and custom commands
        /// setup for use.
        /// </summary>
        public Script CreateScript()
        {
            // Setup Lua
            var lua = new Script
            {
                Options = { CheckThreadAccess = false }
            };

            // Pass the lua script object our object that holds our CLR commands.  This is a DynValue that
            // has been pre-populated with our LuaCommands instance.
            lua.Globals.Set("lua", _luaCmds);

            // Dynamic types from plugins.  These are created when they are registered and only need to be
            // added into globals here for use.
            foreach (var item in _clrTypes)
            {
                lua.Globals.Set(item.Key, item.Value);
            }

            // Set the global variables that are specifically only available in Lua.
            lua.Globals["global"] = this.LuaGlobalVariables;

            return lua;
        }

        /// <summary>
        /// Executes a Lua script asynchronously.
        /// </summary>
        /// <param name="luaCode"></param>
        public async Task ExecuteAsync(string luaCode)
        {
            if (string.IsNullOrWhiteSpace(luaCode))
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(new Action(async () =>
            {
                try
                {
                    IncrementCounter(LuaCounter.ActiveScripts);
                    IncrementCounter(LuaCounter.ScriptsRun);

                    // Get the script for this environment
                    var lua = this.CreateScript();

                    // If there is a Lua global shared set of code run it, try catch it in case there
                    // is a problem with it, we don't want it to interfere with everything if there is
                    // an issue with it, we DO want to show the user that though.
                    var executionControlToken = new ExecutionControlToken();

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(App.Settings?.ProfileSettings?.LuaGlobalScript))
                        {
                            await lua.DoStringAsync(executionControlToken, App.Settings.ProfileSettings.LuaGlobalScript);
                        }
                    }
                    catch (Exception ex)
                    {
                        IncrementCounter(LuaCounter.ErrorCount);
                        LogException("There was an error in the global Lua file.", ex);
                    }

                    await lua.DoStringAsync(executionControlToken, luaCode);
                }
                catch (Exception ex)
                {
                    IncrementCounter(LuaCounter.ErrorCount);
                    LogException(ex: ex);

                    // Cancel pending sends with the mud in case something went haywire
                    await SendCancelCommandAsync();
                }
                finally
                {
                    DecrementCounter(LuaCounter.ActiveScripts);
                }
            }), DispatcherPriority.Normal);
        }

        /// <summary>
        /// Executes a Lua script synchronously.
        /// </summary>
        /// <param name="luaCode"></param>
        public DynValue Execute(string luaCode)
        {
            if (string.IsNullOrWhiteSpace(luaCode))
            {
                return DynValue.Nil;
            }

            var val = Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    IncrementCounter(LuaCounter.ActiveScripts);
                    IncrementCounter(LuaCounter.ScriptsRun);

                    // Get the script for this environment
                    var lua = this.CreateScript();

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(App.Settings?.ProfileSettings?.LuaGlobalScript))
                        {
                            lua.DoString(App.Settings.ProfileSettings.LuaGlobalScript);
                        }
                    }
                    catch (Exception ex)
                    {
                        IncrementCounter(LuaCounter.ErrorCount);
                        LogException("There was an error in the global Lua file.", ex);
                    }

                    return lua.DoString(luaCode);
                }
                catch (Exception ex)
                {
                    IncrementCounter(LuaCounter.ErrorCount);
                    LogException(ex: ex);

                    // Cancel pending sends with the mud in case something went haywire
                    SendCancelCommandAsync();
                }
                finally
                {
                    DecrementCounter(LuaCounter.ActiveScripts);
                }

                return DynValue.Nil;
            }, DispatcherPriority.Normal);

            return val;
        }

        /// <summary>
        /// Executes a shared Lua script synchronously.  This script re-use should be more memory efficient
        /// and use less CPU cycles than loading the a new Script each time (although that method provides a
        /// clean environment each time).
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="args"></param>
        /// <remarks>
        /// MoonSharp is capable of accepting an object[] and converting those values to the
        /// correct types.  Should that be required add the functionality in.  Since this is only called
        /// from RegEx data now and that's always a string type returned we're avoiding the conversion code
        /// all together.
        /// </remarks>
        public DynValue ExecuteShared(string functionName, params string[] args)
        {
            var val = Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    IncrementCounter(LuaCounter.ScriptsRun);
                    DynValue fnc = this.SharedScript.Globals.Get(functionName);

                    // If the function doesn't exist report the error and get out.  The caller should have
                    // loaded the function already.
                    if (fnc.IsNil())
                    {
                        IncrementCounter(LuaCounter.ErrorCount);
                        LogException($"Lua error: Function '{functionName}' was not found.");
                        return DynValue.Nil;
                    }

                    IncrementCounter(LuaCounter.ActiveScripts);
                    IncrementCounter(LuaCounter.ScriptsRunFromCache);

                    return App.MainWindow.Interp.LuaCaller.SharedScript.Call(fnc, args);
                }
                catch (Exception ex)
                {
                    IncrementCounter(LuaCounter.ErrorCount);
                    LogException(ex: ex);

                    // Cancel pending sends with the mud in case something went haywire
                    SendCancelCommandAsync();
                }
                finally
                {
                    DecrementCounter(LuaCounter.ActiveScripts);
                }

                return DynValue.Nil;
            }, DispatcherPriority.Normal);

            return val;
        }

        /// <summary>
        /// Loads a Lua function that can be reused if it is an exact match that has a varargs
        /// signature, e.g. "function do_something(...)".  Every time this is called a new instance
        /// of the script will be loaded.
        /// </summary>
        /// <param name="luaCode"></param>
        public void LoadSharedScript(string luaCode, string id = null)
        {
            _ = this.SharedScript.DoString(luaCode, codeFriendlyName: id);
        }

        /// <summary>
        /// Validates Lua code for potential syntax errors but does not execute it.
        /// </summary>
        /// <param name="luaCode"></param>
        public async Task<LuaValidationResult> ValidateAsync(string luaCode)
        {
            if (string.IsNullOrWhiteSpace(luaCode))
            {
                return new LuaValidationResult
                {
                    Success = true
                };
            }

            try
            {
                // Setup Lua
                var lua = new Script
                {
                    Options = { CheckThreadAccess = false }
                };

                // Pass the lua script object our object that holds our CLR commands.  This is a DynValue that
                // has been pre-populated with our LuaCommands instance.
                lua.Globals.Set("lua", _luaCmds);

                // Dynamic types from plugins.  These are created when they are registered and only need to be
                // added into globals here for use.
                foreach (var item in _clrTypes)
                {
                    lua.Globals.Set(item.Key, item.Value);
                }

                // Set the global variables that are specifically only available in Lua.
                lua.Globals["global"] = this.LuaGlobalVariables;

                if (!string.IsNullOrWhiteSpace(App.Settings?.ProfileSettings?.LuaGlobalScript))
                {
                    await lua.LoadStringAsync(App.Settings.ProfileSettings.LuaGlobalScript);
                }

                await lua.LoadStringAsync(luaCode);

                return new LuaValidationResult
                {
                    Success = true
                };
            }
            catch (SyntaxErrorException ex)
            {
                return new LuaValidationResult
                {
                    Success = false,
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Increments the specified counter and performs locking for thread safety.
        /// </summary>
        /// <param name="c">The counter type.</param>
        /// <param name="value">The value to increment the counter by, if left unspecified the default is 1.</param>
        private void IncrementCounter(LuaCounter c, int value = 1)
        {
            switch (c)
            {
                case LuaCounter.ActiveScripts:
                    lock (_lockObject)
                    {
                        this.ActiveLuaScripts += value;
                        App.MainWindow.ViewModel.LuaScriptsActive = this.ActiveLuaScripts;
                    }

                    break;
                case LuaCounter.ScriptsRun:
                    lock (_lockObject)
                    {
                        this.LuaScriptsRun += value;
                    }

                    break;
                case LuaCounter.ErrorCount:
                    lock (_lockObject)
                    {
                        this.LuaErrorCount += value;
                    }

                    break;
                case LuaCounter.SqlCount:
                    lock (_lockObject)
                    {
                        this.LuaSqlCommandsRun += value;
                    }

                    break;
                case LuaCounter.ScriptsRunFromCache:
                    lock (_lockObject)
                    {
                        this.LuaScriptsRunFromCache += value;
                    }

                    break;
            }
        }

        /// <summary>
        /// Decrements the specified counter and performs locking for thread safety.
        /// </summary>
        /// <param name="c">The counter type.</param>
        /// <param name="value">The value to decrement the counter by, if left unspecified the default is 1.</param>
        private void DecrementCounter(LuaCounter c, int value = 1)
        {
            switch (c)
            {
                case LuaCounter.ActiveScripts:
                    lock (_lockObject)
                    {
                        this.ActiveLuaScripts -= value;
                        App.MainWindow.ViewModel.LuaScriptsActive = this.ActiveLuaScripts;
                    }

                    break;
                case LuaCounter.ScriptsRun:
                    lock (_lockObject)
                    {
                        this.LuaScriptsRun -= value;
                    }

                    break;
                case LuaCounter.ErrorCount:
                    lock (_lockObject)
                    {
                        this.LuaErrorCount -= value;
                    }

                    break;
                case LuaCounter.SqlCount:
                    lock (_lockObject)
                    {
                        this.LuaSqlCommandsRun -= value;
                    }

                    break;
                case LuaCounter.ScriptsRunFromCache:
                    lock (_lockObject)
                    {
                        this.LuaScriptsRunFromCache -= value;
                    }

                    break;
            }
        }

        /// <summary>
        /// Will handle writing the exception to the console.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        private void LogException(string msg = null, Exception ex = null)
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                _interpreter.Conveyor.EchoLog($"Lua Error: {msg}", LogType.Error);
            }

            if (ex == null)
            {
                return;
            }

            // The inner exception will have Lua error with the line number, etc.  Show exception message if the
            // inner exception doesn't exist.
            if (ex.InnerException == null)
            {
                _interpreter.Conveyor.EchoLog($"Lua Exception: {ex.Message}", LogType.Error);
            }

            if (ex.InnerException != null)
            {
                if (ex.InnerException is InterpreterException innerEx)
                {
                    _interpreter.Conveyor.EchoLog($"Lua Inner Exception: {innerEx?.DecoratedMessage}", LogType.Error);
                }
                else
                {
                    _interpreter.Conveyor.EchoLog($"Lua Inner Exception: {ex.InnerException.Message}", LogType.Error);
                }

                if (ex.InnerException.Message.Contains("abort"))
                {
                    _interpreter.Conveyor.EchoLog("All active Lua scripts have been terminated.", LogType.Error);
                }
            }
        }
    }
}