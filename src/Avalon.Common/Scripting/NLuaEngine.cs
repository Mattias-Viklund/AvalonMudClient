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
                ReturnAction = a =>
                {
                }
            };

        }

        public T Execute<T>(string code)
        {
            var lua = LuaMemoryPool.Get();
            lua["lua"] = _cmds;
            var ret = lua.DoString(code);

            LuaMemoryPool.Return(lua);

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

        public void GarbageCollect()
        {
        }
    }
}
