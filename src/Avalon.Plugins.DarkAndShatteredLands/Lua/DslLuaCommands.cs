﻿using Avalon.Common.Interfaces;
using Avalon.Plugins.DarkAndShatteredLands.Affects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avalon.Plugins.DarkAndShatteredLands.Lua
{

    /// <summary>
    /// General DSL Lua commands.
    /// </summary>
    public class DslLuaCommands : ILuaCommand
    {
        public IInterpreter Interpreter { get; set; }

        public string Namespace { get; set; } = "dsl";

        /// <summary>
        /// If the character is affected by the affect name.
        /// </summary>
        /// <param name="affectName"></param>
        public bool IsAffected(string affectName)
        {
            var affectsTrigger = (AffectsTrigger)this.Interpreter.Conveyor.FindTrigger("c40f9237-7753-4357-84a5-8e7d789853ed");

            if (affectsTrigger == null)
            {
               throw new Exception("Affects trigger was null.");
            }

            return affectsTrigger.Affects.Any(x => x.Name.Equals(affectName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns the duration in ticks of the affect.  -1 indicates permanent and -2 indicates the affect does not exist on the player.
        /// </summary>
        /// <param name="affectName"></param>
        public int AffectDuration(string affectName)
        {
            var affectsTrigger = (AffectsTrigger)this.Interpreter.Conveyor.FindTrigger("c40f9237-7753-4357-84a5-8e7d789853ed");

            if (affectsTrigger == null)
            {
                throw new Exception("Affects trigger was null.");
            }

            return affectsTrigger.Affects.FirstOrDefault(x => x.Name.Equals(affectName, StringComparison.OrdinalIgnoreCase))?.Duration ?? -2;
        }

    }

}