/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalon.Common.Scripting
{
    /// <summary>
    /// The implementation structure for a hosted scripting engine.
    /// </summary>
    public interface IScriptEngine
    {
        Dictionary<string, object> SharedObjects { get; set; }

        /// <summary>
        /// Executes code synchronously and returns <see cref="T"/> or null based.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="code"></param>
        /// <returns></returns>
        T Execute<T>(string code);

        /// <summary>
        /// Executes code asynchronously and return <see cref="T"/> or null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(string code);

        /// <summary>
        /// Executes the scripting languages garbage collecting feature if it exists.  As an
        /// example, MoonSharp's Lua interpreter uses the default .NET GC while NLua defers
        /// to the native Lua garbage collection.
        /// </summary>
        void GarbageCollect();
    }
}