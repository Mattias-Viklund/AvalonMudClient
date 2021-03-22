﻿/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Argus.Memory;
using Avalon.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalon.Lua;
using MoonSharp.Interpreter;

namespace Avalon.Common.Scripting
{
    /// <summary>
    /// A Lua script engine provided through MoonSharp's implementation of Lua.
    /// </summary>
    public class MoonSharpEngine : IScriptEngine
    {
        /// <inheritdoc cref="ScriptCommands"/>
        public IScriptCommands ScriptCommands { get; set; }

        /// <inheritdoc cref="Interpreter"/>
        public IInterpreter Interpreter { get; set; }

        /// <summary>
        /// Global variables available to Lua that are shared across all of our Lua sessions.
        /// </summary>
        public MoonSharpGlobalVariables GlobalVariables { get; }

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
        /// An object for thread locking access to resources.
        /// </summary>
        private object _lockObject = new object();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="interp"></param>
        /// <param name="scriptCommands"></param>
        public MoonSharpEngine(IInterpreter interp, IScriptCommands scriptCommands)
        {
            this.Interpreter = interp;
            this.ScriptCommands = scriptCommands;

            _random = new Random();

            this.GlobalVariables = new MoonSharpGlobalVariables();

            // The CLR types we want to expose to Lua need to be registered before UserData.Create
            // can be called.  If they're not registered UserData.Create will return a null.
            UserData.RegisterType<IScriptCommands>();
            UserData.RegisterType<MoonSharpGlobalVariables>();

            _luaCmds = UserData.Create(scriptCommands);
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

            instance.Interpreter = this.Interpreter;

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
        public Task SendCancelCommandAsync()
        {
            return this.Interpreter.Send("~", true, false);
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
        }

        /// <inheritdoc cref="ExecuteAsync{T}"/>
        public Task<T> ExecuteAsync<T>(string code)
        {
        }
    }
}