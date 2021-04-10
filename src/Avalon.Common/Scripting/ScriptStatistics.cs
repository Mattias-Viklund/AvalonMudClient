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
    /// A class that tracks statistics for the scripting environment as a whole.
    /// </summary>
    public class ScriptStatistics
    {
        /// <summary>
        /// An object for thread locking access to resources.
        /// </summary>
        private object _lockObject = new object();

        private int _scriptsActive = 0;

        /// <summary>
        /// The number of scripts that are currently active in the <see cref="ScriptHost"/>.
        /// </summary>
        public int ScriptsActive
        {
            get => _scriptsActive;
            set
            {
                lock (_lockObject)
                {
                    _scriptsActive = value;
                }
            }
        }
    }
}
