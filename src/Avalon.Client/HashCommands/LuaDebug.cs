﻿/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using System;
using Avalon.Common.Interfaces;
using CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Argus.Extensions;
using Avalon.Common.Scripting;
using MoonSharp.Interpreter;

namespace Avalon.HashCommands
{
    public class LuaDebug : HashCommand
    {
        public LuaDebug(IInterpreter interp) : base(interp)
        {
            this.IsAsync = true;
        }

        public override string Name { get; } = "#lua-debug";

        public override string Description { get; } = "Info about the currently Lua environment.";

        public override async Task ExecuteAsync()
        {
            // Parse the arguments and append to the file.
            var result = Parser.Default.ParseArguments<Arguments>(CreateArgs(this.Parameters))
                .WithParsed(o =>
                {
                    var sb = Argus.Memory.StringBuilderPool.Take();
                    var moonSharp = ((Interpreter)this.Interpreter).ScriptHost.MoonSharp;
                    var nLua = ((Interpreter)this.Interpreter).ScriptHost.NLua;
                    var scriptHost = ((Interpreter) this.Interpreter).ScriptHost;

                    sb.AppendLine();
                    sb.Append("{GA{gvalon {WLua Environment Info:{x\r\n");
                    sb.Append("---------------------------------------------------------------------\r\n");
                    sb.AppendFormat(" {{G * {{WScripts Active:{{x                      {{C{0}{{x\r\n", scriptHost.Statistics.ScriptsActive.ToString());
                    sb.AppendLine();

                    sb.Append("{CM{coon{CS{charp{x {WLua Environment Info:{x\r\n");
                    sb.Append("---------------------------------------------------------------------\r\n");
                    sb.AppendFormat(" {{G * {{WMemory Pool:{{x                         {{C{0}/{1}{{x\r\n", moonSharp.MemoryPool.Count().ToString(), moonSharp.MemoryPool.Max.ToString());
                    sb.AppendFormat(" {{G * {{WMemory Pool New Objects:{{x             {{C{0}{{x\r\n", moonSharp.MemoryPool.CounterNewObjects.ToString());
                    sb.AppendFormat(" {{G * {{WMemory Pool Reuse Count:{{x             {{C{0}{{x\r\n", moonSharp.MemoryPool.CounterReusedObjects.ToString());

                    int instructionCount = 0;

                    moonSharp.MemoryPool.InvokeAll((script) =>
                    {
                        instructionCount += script.InstructionCount;
                    });

                    sb.AppendFormat(" {{G * {{WMemory Pool Lua Instructions Stored:{{x {{C{0}{{x\r\n", instructionCount.ToString().FormatIfNumber(0));
                    sb.AppendFormat(" {{G * {{WGlobal Variable Count:{{x               {{C{0}{{x\r\n", moonSharp.GlobalVariables.Count.ToString());

                    sb.AppendLine();
                    sb.Append("{CM{coon{CS{charp{x Global Variables:{x\r\n");
                    sb.Append("---------------------------------------------------------------------\r\n");

                        if (moonSharp.GlobalVariables.Count == 0)
                        {
                            sb.Append("  {G* {WNo global variables are currently stored.{x");
                        }
                        else
                        {
                            foreach (string key in moonSharp.GlobalVariables.Keys)
                            {
                                sb.AppendFormat("  {{G* {{W{0}: {{C{1}{{x\r\n", key, moonSharp.GlobalVariables[key]);
                            }
                        }

                        sb.AppendLine();
                        this.Interpreter.Conveyor.EchoText(sb.ToString());
                        Argus.Memory.StringBuilderPool.Return(sb);
                });

            this.DisplayParserOutput(result);
        }

        /// <summary>
        /// The supported command line arguments.
        /// </summary>
        public class Arguments
        {
            [Option('i', "info", Required = false, HelpText = "Information about the current Lua environment.")]
            public bool Info { get; set; } = false;

            [Option('g', "global", Required = false, HelpText = "A list of Lua global variables currently stored.")]
            public bool GlobalsList { get; set; } = false;

        }

    }
}