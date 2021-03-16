/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using System.Threading.Tasks;
using Avalon.Common.Interfaces;

namespace Avalon.Common.Scripting
{
    public interface IScriptEngine
    {
        IInterpreter Interpreter { get; set; }

        object Execute(string code);

        Task<object> ExecuteAsync(string code);

        object ExecuteFunction(string functionName, params object[] args);

        Task<object> ExecuteFunctionAsync(string functionName, params object[] args);

        void GarbageCollect();
    }
}