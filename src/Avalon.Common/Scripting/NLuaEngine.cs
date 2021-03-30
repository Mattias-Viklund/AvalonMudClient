﻿/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Argus.Memory;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// A list of shared objects that will be passed to each Lua script.
        /// </summary>
        public Dictionary<string, object> SharedObjects { get; set; } = new();

        /// <summary>
        /// Constructor
        /// </summary>
        public NLuaEngine()
        {
            LuaMemoryPool = new ObjectPool<NLua.Lua>
            {
                InitAction = l =>
                {
                    // Load any shared objects into the new instance of NLua.
                    foreach (var item in this.SharedObjects)
                    {
                        l[item.Key] = item.Value;
                    }
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

        /// <summary>
        /// Registers an instantiated object with NLua.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="prefix"></param>
        public void RegisterObject<T>(object item, string prefix)
        {
            this.SharedObjects.Add(prefix, item);
        }

        /// <summary>
        /// Calls collectgarbage() against the native Lua implementation.  This does NOT collect
        /// the .NET NLua objects in the memory pool itself.
        /// </summary>
        public void GarbageCollect()
        {
            LuaMemoryPool.InvokeAll((item) =>
            {
                if (item.IsExecuting)
                {
                    return;
                }

                item.DoString("collectgarbage()");
            });
        }

        /// <inheritdoc cref="Execute{T}"/>
        public T Execute<T>(string code)
        {
            // Gets a new or used but ready instance of the a Lua object to use.
            var lua = LuaMemoryPool.Get();

            // Execute our code.  Make sure if an exception occurs that the Lua object
            // is returned to the pool.
            object[] ret;

            // We want the exception to bubble up but we have to return the object to the
            // memory pool.
            try
            {
                ret = lua.DoString(code);
            }
            finally
            {
                LuaMemoryPool.Return(lua);
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