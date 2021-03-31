/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

namespace Avalon.Common.Scripting
{
    /// <summary>
    /// Container to house all of the available scripting environments.
    /// </summary>
    public class ScriptHost
    {
        /// <summary>
        /// MoonSharp Lua Engine.
        /// </summary>
        public MoonSharpEngine MoonSharp { get; set; }

        /// <summary>
        /// NLua Lua Engine.
        /// </summary>
        public NLuaEngine NLua { get; set; }

        public ScriptHost()
        {
            this.MoonSharp = new MoonSharpEngine();
            this.NLua = new NLuaEngine();
        }

        /// <summary>
        /// Registers an object with all of the available script engines.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="prefix"></param>
        public void RegisterObject<T>(object item, string prefix)
        {
            MoonSharp.RegisterObject<T>(item, prefix);
            NLua.RegisterObject<T>(item, prefix);
        }

        /// <summary>
        /// Resets all scripting engines to their default state.
        /// </summary>
        public void Reset()
        {
            MoonSharp.Reset();
            NLua.Reset();
        }
    }
}
