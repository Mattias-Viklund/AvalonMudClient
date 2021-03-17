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
    /// <summary>
    /// The implementation structure for a hosted scripting engine.
    /// </summary>
    public interface IScriptEngine
    {
        IInterpreter Interpreter { get; set; }

        T Execute<T>(string code);

        Task<T> ExecuteAsync<T>(string code);
    }
}