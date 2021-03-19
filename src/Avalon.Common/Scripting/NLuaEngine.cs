/*
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
using System.Threading.Tasks;

namespace Avalon.Common.Scripting
{
    /// <summary>
    /// A Lua script engine provided through NLua.
    /// </summary>
    public class NLuaEngine : IScriptEngine
    {
        /// <summary>
        /// A memory pool of idle Lua objects that can be re-used.
        /// </summary>
        internal static ObjectPool<NLua.Lua> LuaMemoryPool { get; set; }

        /// <inheritdoc cref="ScriptCommands"/>
        public IScriptCommands ScriptCommands { get; set; }

        /// <inheritdoc cref="Interpreter"/>
        public IInterpreter Interpreter { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="interp"></param>
        /// <param name="scriptCommands"></param>
        public NLuaEngine(IInterpreter interp, IScriptCommands scriptCommands)
        {
            this.Interpreter = interp;
            this.ScriptCommands = scriptCommands;

            LuaMemoryPool = new ObjectPool<NLua.Lua>
            {
                InitAction = l =>
                {
                    // Initiate any setup actions needed for this Lua script.
                    l["lua"] = this.ScriptCommands;
                },
                ReturnAction = l =>
                {
                    if (l.IsExecuting)
                    {
                        // By throwing an exception here the Lua object will not be returned to the pool and
                        // will instead be disposed of.
                        throw new Exception("Attempt to return a Lua object to the pool that was still executing failed.");
                    }
                }
            };
        }

        /// <inheritdoc cref="Execute{T}"/>
        public T Execute<T>(string code)
        {
            // Gets a new or used but ready instance of the a Lua object to use.
            var lua = LuaMemoryPool.Get();

            // Execute our code.  Make sure if an exception occurs that the Lua object
            // is returned to the pool.
            object[] ret;

            try
            {
                ret = lua.DoString(code);
                LuaMemoryPool.Return(lua);
            }
            catch
            {
                LuaMemoryPool.Return(lua);
                throw;
            }

            // If a result was returned cast it to T and return it, if not, return the default
            // which will be null for reference types.
            if (ret != null && ret.Length > 0)
            {
                return (T)ret[0];
            }

            return default(T);
        }

        /// <inheritdoc cref="ExecuteAsync{T}"/>
        public Task<T> ExecuteAsync<T>(string code)
        {
            return Task.Run(() => this.Execute<T>(code));
        }
    }
}