/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Avalon.Common.Interfaces;
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
        /// Global variables available to Lua that are shared across all of our Lua sessions.
        /// </summary>
        public MoonSharpGlobalVariables GlobalVariables { get; }

        /// <summary>
        /// A list of shared objects that will be passed to each Lua script.
        /// </summary>
        public Dictionary<string, object> SharedObjects { get; set; } = new();

        /// <summary>
        /// Constructor
        /// </summary>
        public MoonSharpEngine()
        {
            UserData.RegisterType<MoonSharpGlobalVariables>();
            this.GlobalVariables = new MoonSharpGlobalVariables();
        }

        /// <summary>
        /// Registers an instantiated object (the object and the type).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="prefix"></param>
        public void RegisterObject<T>(object item, string prefix)
        {
            // Only add the type in if it hasn't been added previously.
            if (item == null || this.SharedObjects.ContainsKey(prefix))
            {
                return;
            }

            UserData.RegisterType<T>();

            this.SharedObjects.Add(prefix, UserData.Create(item));
        }

        /// <summary>
        /// Clears the custom loaded types from LuaCaller.RegisterType.
        /// </summary>
        public void ClearTypes()
        {
            this.SharedObjects.Clear();
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

            // Dynamic types from plugins.  These are created when they are registered and only need to be
            // added into globals here for use.
            foreach (var item in this.SharedObjects)
            {
                lua.Globals.Set(item.Key, (DynValue)item.Value);
            }

            // Set the global variables that are specifically only available in Lua.
            lua.Globals["global"] = this.GlobalVariables;

            return lua;
        }

        /// <summary>
        /// No garbage collection is run for the MoonSharp implementation.
        /// </summary>
        public void GarbageCollect()
        {
        }

        /// <inheritdoc cref="Execute{T}"/>
        public T Execute<T>(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return DynValue.Nil.ToObject<T>();
            }

            var lua = this.CreateScript();

            return lua.DoString(code).ToObject<T>();
        }

        /// <inheritdoc cref="ExecuteAsync{T}"/>
        public async Task<T> ExecuteAsync<T>(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return DynValue.Nil.ToObject<T>();
            }

            var lua = this.CreateScript();
            var executionControlToken = new ExecutionControlToken();
            var ret = await lua.DoStringAsync(executionControlToken, code);

            return ret.ToObject<T>();
        }
    }
}