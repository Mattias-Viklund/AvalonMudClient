/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Argus.Extensions;
using System.Text;

namespace Avalon.Lua
{
    /// <summary>
    /// Represents the current metadata of a Lua script.  This is meant to be a runtime
    /// type that is not persisted.  It's used to organize what's needed to have a script
    /// cache that can be re-used.
    /// </summary>
    public class LuaScript
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">The unique ID, generally a Guid of the object that owns this script.</param>
        public LuaScript(string id)
        {
            this.Id = id;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public LuaScript()
        {
            
        }

        /// <summary>
        /// Whether the code of the Lua script has been updated.
        /// </summary>
        public bool Updated { get; set; } = false;

        private string _id;

        /// <summary>
        /// The ID that links this script to an owning object.  It is also used to create the
        /// <see cref="FunctionName"/> when set.
        /// </summary>
        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                this.FunctionName = string.Concat("x", value.Replace("-", "").DeleteLeft(1));
            }
        }

        /// <summary>
        /// The name of the function that is stored in the Lua cache.  The function is wrapped automatically around
        /// the code with a varargs pattern.
        /// </summary>
        public string FunctionName { get; private set; }

        private string _code;

        /// <summary>
        /// The Lua code.  This code will be wrapped as a function, e.g. "function do_something(...)"
        /// </summary>
        public string Code
        {
            get => _code;
            set
            {
                // Wrap the Lua code in a function that has a varargs as a parameter.
                StringBuilder sb = null;

                try
                {
                    sb = Argus.Memory.StringBuilderPool.Take();
                    sb.AppendFormat("function {0}(...)\n", this.FunctionName);
                    sb.Append(value);
                    sb.Append("\nend");
                    _code = sb.ToString();

                    this.Updated = true;
                }
                finally
                {
                    Argus.Memory.StringBuilderPool.Return(sb);
                }
            }
        }
    }
}
