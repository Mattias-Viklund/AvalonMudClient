/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Argus.Memory;
using Avalon.Lua;
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

        /// <summary>
        /// A list of shared objects that will be passed to each Lua script.
        /// </summary>
        public Dictionary<string, object> SharedObjects { get; set; } = new();

        /// <inheritdoc cref="ExceptionHandler"/>
        public Action<Exception> ExceptionHandler { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public MoonSharpEngine()
        {
            // The global variables will be created once and registered.  These will be shared
            // between all script instances.
            UserData.RegisterType<MoonSharpGlobalVariables>();
            this.GlobalVariables = new MoonSharpGlobalVariables();

            MemoryPool = new ObjectPool<Script>
            {
                InitAction = l =>
                {
                    // Setup Lua
                    l.Options.CheckThreadAccess = false;

                    // Dynamic types from plugins.  These are created when they are registered and only need to be
                    // added into globals here for use.
                    foreach (var item in this.SharedObjects)
                    {
                        l.Globals.Set(item.Key, (DynValue)item.Value);
                    }

                    // Set the global variables that are specifically only available in Lua.
                    l.Globals["global"] = this.GlobalVariables;
                },
                ReturnAction = l =>
                {
                }
            };

            Script.WarmUp();
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
                ret = lua.DoString(code);
            }
            catch (Exception ex)
            {
                this?.ExceptionHandler(ex);
                throw;
            }
            finally
            {
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
                ret = await lua.DoStringAsync(executionControlToken, code);
            }
            catch (Exception ex)
            {
                this?.ExceptionHandler(ex);
                throw;
            }
            finally
            {
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
        public async Task<T> ExecuteFunctionAsync<T>(string functionName, string code, params string[] args)
        {
            // Gets a new or used but ready instance of the a Lua object to use.
            var lua = MemoryPool.Get();

            // See if the function exists, if it doesn't, we will load it based off of the code provided.
            DynValue fnc = lua.Globals.Get(functionName);

            // If the function doesn't exist report the error and get out.  The caller should have
            // loaded the function already.
            if (fnc.IsNil())
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return DynValue.Nil.ToObject<T>();
                }

                _ = lua.DoString(code, codeFriendlyName: functionName);
                fnc = lua.Globals.Get(functionName);
            }

            DynValue ret;
            var executionControlToken = new ExecutionControlToken();
            
            try
            {
                ret = await lua.CallAsync(executionControlToken, fnc, args);
            }
            catch (Exception ex)
            {
                this?.ExceptionHandler(ex);
                throw;
            }
            finally
            {
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
        public T ExecuteFunction<T>(string functionName, string code, params string[] args)
        {
            // Gets a new or used but ready instance of the a Lua object to use.
            var lua = MemoryPool.Get();

            // See if the function exists, if it doesn't, we will load it based off of the code provided.
            DynValue fnc = lua.Globals.Get(functionName);

            // If the function doesn't exist report the error and get out.  The caller should have
            // loaded the function already.
            if (fnc.IsNil())
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return DynValue.Nil.ToObject<T>();
                }

                _ = lua.DoString(code, codeFriendlyName: functionName);
                fnc = lua.Globals.Get(functionName);
            }

            DynValue ret;

            try
            {
                ret = lua.Call(fnc, args);
            }
            catch (Exception ex)
            {
                this?.ExceptionHandler(ex);
                throw;
            }
            finally
            {
                MemoryPool.Return(lua);
            }

            return ret.ToObject<T>();
        }
    }
}