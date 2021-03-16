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

namespace Avalon.Common.Scripting
{
    public class NLuaEngine : IScriptEngine
    {
        private IScriptCommands _cmds;

        public NLuaEngine(IInterpreter interp, IScriptCommands cmds)
        {
            this.Interpreter = interp;
            _cmds = cmds;
        }

        public IInterpreter Interpreter { get; set; }

        public object Execute(string code)
        {
            using (var lua = new NLua.Lua())
            {
                lua["lua"] = _cmds;
                return lua.DoString(code)[0];
            }
        }

        public async Task<object> ExecuteAsync(string code)
        {
            // do some stuff
            var task = Task.Run(() =>
            {
                return Execute(code);
            });

            return await task;
        }

        public void Test()
        {
            using (var lua = new NLua.Lua())
            {
                lua["lua"] = _cmds;

                Parallel.For(0, 100,
                    index => {
                        lua.DoString("lua:LogInfo('{index}')");
                    });
            }
        }

        public object ExecuteFunction(string functionName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public Task<object> ExecuteFunctionAsync(string functionName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void GarbageCollect()
        {
            throw new NotImplementedException();
        }
    }
}
