/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using MoonSharp.Interpreter;

namespace Avalon.Lua
{
    /// <summary>
    /// A result from Lua code that was validated.
    /// </summary>
    public class LuaValidationResult
    {

        public bool Success { get; set; }

        public SyntaxErrorException Exception { get; set; }

    }
}
