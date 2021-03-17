/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Avalon.Common.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Argus.Memory;
using Avalon.Common.Models;

namespace Avalon.Common.Scripting
{
    public class NLuaEngine : IScriptEngine
    {
        internal static ObjectPool<NLua.Lua> LuaMemoryPool { get; set; }

        private IScriptCommands _cmds;

        public IInterpreter Interpreter { get; set; }

        public NLuaEngine(IInterpreter interp, IScriptCommands cmds)
        {
            this.Interpreter = interp;
            _cmds = cmds;

            LuaMemoryPool = new ObjectPool<NLua.Lua>
            {
                InitAction = l =>
                {
                    // Initiate any setup actions needed for this Lua script.
                    l["lua"] = _cmds;
                },
                ReturnAction = l =>
                {
                    if (l.IsExecuting)
                    {
                        throw new Exception("Lua object was returned to the pool that was still executing.");
                    }
                }
            };
        }

        public T Execute<T>(string code)
        {
            // Gets a new or used but ready instance of the a Lua object to use.
            var lua = LuaMemoryPool.Get();

            // Execute our code.  Make sure if an exception occurs that the Lua object
            // is returned to the pool.
            object[] ret;

            ret = lua.DoString(code);
            LuaMemoryPool.Return(lua);

            // If a result was returned cast it to T and return it, if not, return the default
            // which will be null for reference types.
            if (ret.Length > 0)
            {
                return (T)ret[0];
            }

            return default(T);
        }

        public Task<T> ExecuteAsync<T>(string code)
        {
            return Task.Run(() => this.Execute<T>(code));
        }
    }
}
