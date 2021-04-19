/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Argus.Memory;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalon.Common.Scripting
{
    /// <summary>
    /// A Lua script engine provided through MoonSharp's implementation of Lua.
    /// </summary>
    public class MoonSharpEngine : IScriptEngine
    {
        /// <summary>
        /// A memory pool of idle Script objects that can be re-used.
        /// </summary>
        public ObjectPool<Script> MemoryPool { get; set; }

        /// <summary>
        /// Global variables available to Lua that are shared across all of our Lua sessions.
        /// </summary>
        public MoonSharpGlobalVariables GlobalVariables { get; private set; }

        /// <inheritdoc cref="SharedObjects"/>
        public Dictionary<string, object> SharedObjects { get; set; } = new();

        /// <inheritdoc cref="ExceptionHandler"/>
        public Action<Exception> ExceptionHandler { get; set; }

        /// <summary>
        /// The list of functions and code which have been loaded into this environment.
        /// </summary>
        public Dictionary<string, string> Functions { get; set; } = new();

        /// <summary>
        /// <inheritdoc cref="ScriptHost"/>
        /// </summary>
        public ScriptHost ScriptHost { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public MoonSharpEngine(ScriptHost host)
        {
            this.ScriptHost = host;

            // The global variables will be created once and registered.  These will be shared
            // between all script instances.
            UserData.RegisterType<MoonSharpGlobalVariables>();
            this.GlobalVariables = new MoonSharpGlobalVariables();

            MemoryPool = new ObjectPool<Script>
            {
                InitAction = this.InitializeScript,
                ReturnAction = script =>
                {
                }
            };

            Script.WarmUp();
        }

        /// <summary>
        /// The default options and references to set on our <see cref="Script"/> objects.
        /// </summary>
        /// <param name="script"></param>
        private void InitializeScript(Script script)
        {
            // Setup Lua
            script.Options.CheckThreadAccess = false;

            // Dynamic types from plugins.  These are created when they are registered and only need to be
            // added into globals here for use.
            foreach (var item in this.SharedObjects)
            {
                script.Globals.Set(item.Key, (DynValue)item.Value);
            }

            // Set the global variables that are specifically only available in Lua.
            script.Globals["global"] = this.GlobalVariables;

            foreach (var func in this.Functions)
            {
                script.DoString(func.Value, codeFriendlyName: func.Key);
            }
        }

        /// <inheritdoc cref="RegisterObject{T}"/>
        public void RegisterObject<T>(Type t, object item, string prefix)
        {
            // Registering any object forces the memory pool to clear since those objects
            // will need to be loaded
            if (MemoryPool.Count() > 0)
            {
                MemoryPool.Clear();
            }

            // Only add the type in if it hasn't been added previously.
            if (item == null || this.SharedObjects.ContainsKey(prefix))
            {
                return;
            }

            // Register the type if it's not already registered.
            if (!UserData.IsTypeRegistered(t))
            {
                UserData.RegisterType(t);
            }

            this.SharedObjects.Add(prefix, UserData.Create(item));
        }

        /// <inheritdoc cref="Reset"/>
        public void Reset()
        {
            MemoryPool.Clear();
            this.GlobalVariables = new MoonSharpGlobalVariables();
        }

        /// <summary>
        /// MoonSharp uses the CLR garbage collector.  We'll just run the same collect call that it
        /// would run if collectgarbage was called from Lua code.
        /// </summary>
        public void GarbageCollect()
        {
            GC.Collect(2, GCCollectionMode.Forced);
        }

        /// <summary>
        /// Loads a function into all available script objects in the <see cref="MemoryPool"/>.
        /// </summary>
        /// <param name="functionName">The name of the function to call.</param>
        /// <param name="code">The Lua code to load.</param>
        public void LoadFunction(string functionName, string code)
        {
            if (string.IsNullOrWhiteSpace(functionName) || string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            try
            {
                this.MemoryPool.InvokeAll((script) =>
                {
                    // See if the function exists, if it doesn't, we will load it based off of the code provided.
                    DynValue fnc = script.Globals.Get(functionName);

                    if (fnc.IsNil())
                    {
                        // The function doesn't exist, execute the code to load it
                        _ = script.DoString(code, codeFriendlyName: functionName);
                    }
                    else
                    {
                        // The function did exist, unload and then reload it.
                        script.Globals.Remove(functionName);
                        _ = script.DoString(code, codeFriendlyName: functionName);
                    }

                    // Only add this function if it ran successfully since it will be used in
                    // other pooled script objects.
                    this.Functions[functionName] = code;
                });
            }
            catch (Exception ex)
            {
                this?.ExceptionHandler(ex);
                throw;
            }
        }

        /// <inheritdoc cref="Execute{T}"/>
        public T Execute<T>(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return DynValue.Nil.ToObject<T>();
            }

            // Gets a new or used but ready instance of the a Lua object to use.
            var lua = MemoryPool.Get();
            DynValue ret;

            try
            {
                this.ScriptHost.Statistics.ScriptsActive++;
                ret = lua.DoString(code);
            }
            catch (Exception ex)
            {
                this?.ExceptionHandler(ex);
                throw;
            }
            finally
            {
                this.ScriptHost.Statistics.ScriptsActive--;
                MemoryPool.Return(lua);
            }

            return ret.ToObject<T>();
        }

        /// <inheritdoc cref="ExecuteAsync{T}"/>
        public async Task<T> ExecuteAsync<T>(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return DynValue.Nil.ToObject<T>();
            }

            // Gets a new or used but ready instance of the a Lua object to use.
            var lua = MemoryPool.Get();
            DynValue ret;
            var executionControlToken = new ExecutionControlToken();

            try
            {
                this.ScriptHost.Statistics.ScriptsActive++;
                ret = await lua.DoStringAsync(executionControlToken, code);
            }
            catch (Exception ex)
            {
                this?.ExceptionHandler(ex);
                throw;
            }
            finally
            {
                this.ScriptHost.Statistics.ScriptsActive--;
                MemoryPool.Return(lua);
            }

            return ret.ToObject<T>();
        }

        /// <summary>
        /// Executes a function.  If the function isn't stored a copy will be loaded.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="functionName">The name of the function to call.</param>
        /// <param name="code">The Lua code to load if the function hasn't already been loaded.</param>
        /// <param name="args">Any param arguments to pass to the function.</param>
        public async Task<T> ExecuteFunctionAsync<T>(string functionName, params string[] args)
        {
            // Gets a new or used but ready instance of the a Lua object to use.
            var lua = MemoryPool.Get();

            // See if the function exists, if it doesn't, we will load it based off of the code provided.
            DynValue fnc = lua.Globals.Get(functionName);

            var ec = new ExecutionControlToken();

            // If the function doesn't exist report the error and get out.
            if (fnc.IsNil())
            {
                throw new Exception($"Function '{functionName}' was not loaded.");
            }

            DynValue ret;

            try
            {
                this.ScriptHost.Statistics.ScriptsActive++;
                ret = await lua.CallAsync(ec, fnc, args);
            }
            catch (Exception ex)
            {
                this?.ExceptionHandler(ex);
                throw;
            }
            finally
            {
                this.ScriptHost.Statistics.ScriptsActive--;
                MemoryPool.Return(lua);
            }

            return ret.ToObject<T>();
        }

        /// <summary>
        /// Executes a function.  If the function isn't stored a copy will be loaded.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="functionName">The name of the function to call.</param>
        /// <param name="code">The Lua code to load if the function hasn't already been loaded.</param>
        /// <param name="args">Any param arguments to pass to the function.</param>
        public T ExecuteFunction<T>(string functionName, params string[] args)
        {
            // Gets a new or used but ready instance of the a Lua object to use.
            var lua = MemoryPool.Get();

            // See if the function exists, if it doesn't, we will load it based off of the code provided.
            DynValue fnc = lua.Globals.Get(functionName);

            // If the function doesn't exist report the error and get out.
            if (fnc.IsNil())
            {
                throw new Exception($"Function '{functionName}' was not loaded.");
            }

            DynValue ret;

            try
            {
                this.ScriptHost.Statistics.ScriptsActive++;
                ret = lua.Call(fnc, args);
            }
            catch (Exception ex)
            {
                this?.ExceptionHandler(ex);
                throw;
            }
            finally
            {
                this.ScriptHost.Statistics.ScriptsActive--;
                MemoryPool.Return(lua);
            }

            return ret.ToObject<T>();
        }

        /// <inheritdoc cref="ExecuteStatic{T}"/>
        public T ExecuteStatic<T>(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return DynValue.Nil.ToObject<T>();
            }

            // Gets a new instance of a Script that will be discarded after use.
            var lua = new Script();
            DynValue ret;

            this.InitializeScript(lua);

            try
            {
                this.ScriptHost.Statistics.ScriptsActive++;
                ret = lua.DoString(code);
            }
            catch (Exception ex)
            {
                this?.ExceptionHandler(ex);
                throw;
            }
            finally
            {
                this.ScriptHost.Statistics.ScriptsActive--;
            }

            return ret.ToObject<T>();
        }

        /// <inheritdoc cref="ExecuteStaticAsync{T}"/>
        public async Task<T> ExecuteStaticAsync<T>(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return DynValue.Nil.ToObject<T>();
            }

            // Gets a new or used but ready instance of the a Lua object to use.
            var lua = new Script();
            DynValue ret;
            var executionControlToken = new ExecutionControlToken();

            this.InitializeScript(lua);

            try
            {
                this.ScriptHost.Statistics.ScriptsActive++;
                ret = await lua.DoStringAsync(executionControlToken, code);
            }
            catch (Exception ex)
            {
                this?.ExceptionHandler(ex);
                throw;
            }
            finally
            {
                this.ScriptHost.Statistics.ScriptsActive--;
            }

            return ret.ToObject<T>();
        }

        /// <summary>
        /// Validates Lua code for potential syntax errors but does not execute it.
        /// </summary>
        /// <param name="luaCode"></param>
        public ValidationResult Validate(string luaCode)
        {
            if (string.IsNullOrWhiteSpace(luaCode))
            {
                return new ValidationResult
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

                // Dynamic types from plugins.  These are created when they are registered and only need to be
                // added into globals here for use.
                foreach (var item in this.SharedObjects)
                {
                    lua.Globals.Set(item.Key, (DynValue)item.Value);
                }

                // Set the global variables that are specifically only available in Lua.
                lua.Globals["global"] = this.GlobalVariables;

                lua.LoadString(luaCode);

                return new ValidationResult
                {
                    Success = true
                };
            }
            catch (SyntaxErrorException ex)
            {
                return new ValidationResult
                {
                    Success = false,
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Validates Lua code for potential syntax errors but does not execute it.
        /// </summary>
        /// <param name="luaCode"></param>
        public async Task<ValidationResult> ValidateAsync(string luaCode)
        {
            if (string.IsNullOrWhiteSpace(luaCode))
            {
                return new ValidationResult
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

                // Dynamic types from plugins.  These are created when they are registered and only need to be
                // added into globals here for use.
                foreach (var item in this.SharedObjects)
                {
                    lua.Globals.Set(item.Key, (DynValue)item.Value);
                }

                // Set the global variables that are specifically only available in Lua.
                lua.Globals["global"] = this.GlobalVariables;

                await lua.LoadStringAsync(luaCode);

                return new ValidationResult
                {
                    Success = true
                };
            }
            catch (SyntaxErrorException ex)
            {
                return new ValidationResult
                {
                    Success = false,
                    Exception = ex
                };
            }
        }
    }
}