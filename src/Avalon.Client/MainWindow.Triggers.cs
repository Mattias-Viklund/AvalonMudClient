/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using System;
using System.Linq;
using Avalon.Colors;
using Avalon.Common.Colors;
using Avalon.Common.Models;
using Avalon.Extensions;
using Cysharp.Text;
using MoonSharp.Interpreter;

namespace Avalon
{
    /// <summary>
    /// Partial class for trigger related events and code of the MainWindow.
    /// </summary>
    public partial class MainWindow
    {

        /// <summary>
        /// Checks a line to see if any Triggers should fire and if so executes those triggers.
        /// </summary>
        /// <param name="line"></param>
        public async void CheckTriggers(Line line)
        {
            // Don't process if the user has disabled triggers.
            if (!App.Settings.ProfileSettings.TriggersEnabled)
            {
                return;
            }

            // Don't process if the line is blank.
            if (string.IsNullOrWhiteSpace(line.Text))
            {
                return;
            }

            // Replacement triggers come, they actually alter the line in the terminal.
            this.ProcessReplacementTriggers(line);

            // Go through the immutable system triggers, system triggers are silent in that
            // they won't echo to the terminal window, they also don't adhere to attributes like
            // character or enabled.  These can and will have CLR implementations and can be loaded
            // from other DLL's as plugins.  System triggers are also unique in that they are designed
            // to be loaded from a plugin and they don't save their state in the profile.
            foreach (var item in App.InstanceGlobals.SystemTriggers)
            {
                // Skip it if it's not enabled.
                if (!item.Enabled)
                {
                    continue;
                }

                if (item.IsMatch(line.Text))
                {
                    // Run any CLR that might exist.
                    item.Execute();

                    if (!string.IsNullOrEmpty(item.Command) && item.ExecuteAs == ExecuteType.Command)
                    {
                        // If it has text but it's not lua, send it to the interpreter.
                        await Interp.Send(item.Command, item.IsSilent, false);
                    }
                    else if (!string.IsNullOrEmpty(item.Command) && (item.IsLua || item.ExecuteAs == ExecuteType.LuaMoonsharp))
                    {
                        // If it has text and it IS lua, send it to the LUA engine.
                        _ = Interp.ScriptHost.MoonSharp.ExecuteAsync<object>(item.Command);
                    }

                    // Check if we're supposed to move this line somewhere else.
                    if (item.MoveTo != TerminalTarget.None)
                    {
                        // Create a brand new line (not a shared reference) where this can be shown in the communication window.
                        var commLine = new Line
                        {
                            FormattedText = $"[{Utilities.Utilities.Timestamp()}]: {line.FormattedText}\r\n"
                        };

                        if (item.MoveTo == TerminalTarget.Terminal1)
                        {
                            Terminal1.Append(commLine);

                            if (!App.MainWindow.CustomTab1.IsSelected)
                            {
                                App.MainWindow.CustomTab1Badge.Value++;
                            }
                            else if (App.MainWindow.CustomTab1.IsSelected && App.MainWindow.CustomTab1Badge.Value != 0)
                            {
                                // Only setting this if the value isn't 0 so it doesn't trigger UI processing.
                                App.MainWindow.CustomTab1Badge.Value = 0;
                            }
                        }
                        else if (item.MoveTo == TerminalTarget.Terminal2)
                        {
                            Terminal2.Append(commLine);

                            if (!App.MainWindow.CustomTab2.IsSelected)
                            {
                                App.MainWindow.CustomTab2Badge.Value++;
                            }
                            else if (App.MainWindow.CustomTab2.IsSelected && App.MainWindow.CustomTab2Badge.Value != 0)
                            {
                                // Only setting this if the value isn't 0 so it doesn't trigger UI processing.
                                App.MainWindow.CustomTab2Badge.Value = 0;
                            }
                        }
                        else if (item.MoveTo == TerminalTarget.Terminal3)
                        {
                            Terminal3.Append(commLine);

                            if (!App.MainWindow.CustomTab3.IsSelected)
                            {
                                App.MainWindow.CustomTab3Badge.Value++;
                            }
                            else if (App.MainWindow.CustomTab3.IsSelected && App.MainWindow.CustomTab3Badge.Value != 0)
                            {
                                // Only setting this if the value isn't 0 so it doesn't trigger UI processing.
                                App.MainWindow.CustomTab3Badge.Value = 0;
                            }
                        }

                    }

                    // This breaks instead of returning, no more system triggers would be processed but user
                    // ones might.
                    if (item.StopProcessing)
                    {
                        // To help with debugging.
                        if (App.Settings.AvalonSettings.Debug)
                        {
                            App.Conveyor.EchoLog("System trigger matched that stops the processing of the rest of the trigger list.", LogType.Debug);
                        }

                        break;
                    }
                }
            }

            // Go through the TriggerList which are user defined triggers
            foreach (var item in App.Settings.ProfileSettings.TriggerList.EnabledEnumerable())
            {
                // Skip it if it's not global or for this character.
                if (!string.IsNullOrWhiteSpace(item.Character) && item.Character != App.Conveyor.GetVariable("Character"))
                {
                    continue;
                }

                if (item.IsMatch(line.Text))
                {
                    // Run any CLR that might exist.
                    item.Execute();

                    // Increment the counter.
                    item.Count++;

                    // Line Highlighting if the trigger is supposed to.  Insert the ANSI color at the start of the line.
                    if (item.HighlightLine)
                    {
                        // TODO - Allow the highlighted color to be set for each trigger.
                        int start = GameTerminal.Document.Text.LastIndexOf(line.FormattedText, StringComparison.Ordinal);
                        GameTerminal.Document.Insert(start, AnsiColors.DarkCyan);
                    }

                    // Only send if it has something in it.  Use the processed command.
                    if (!string.IsNullOrEmpty(item.ProcessedCommand) && !item.IsLua)
                    {
                        // If it has text but it's not lua, send it to the interpreter.
                        await Interp.Send(item.ProcessedCommand, false, false);
                    }
                    else if (item.IsLua && !string.IsNullOrWhiteSpace(item.ProcessedCommand))
                    {
                        // Not sure why the try/catch calling CheckTriggers wasn't catching Lua errors.  Eat
                        // the error here and allow the script host to process it's exception handler which will
                        // echo it to the terminal.
                        try
                        {
                            // If it has text and it IS lua, send it to the LUA engine.
                            _ = await Interp.ScriptHost.MoonSharp.ExecuteAsync<object>(item.ProcessedCommand);
                        }
                        catch { }
                    }

                    // Check if we're supposed to move this line somewhere else.
                    if (item.MoveTo != TerminalTarget.None)
                    {
                        // Create a brand new line (not a shared reference) where this can be shown in the communication window.
                        var commLine = new Line
                        {
                            FormattedText = $"[{Utilities.Utilities.Timestamp()}]: {line.FormattedText}\r\n"
                        };

                        if (item.MoveTo == TerminalTarget.Terminal1)
                        {
                            Terminal1.Append(commLine);

                            if (!App.MainWindow.CustomTab1.IsSelected)
                            {
                                App.MainWindow.CustomTab1Badge.Value++;
                            }
                            else if (App.MainWindow.CustomTab1.IsSelected && App.MainWindow.CustomTab1Badge.Value != 0)
                            {
                                // Only setting this if the value isn't 0 so it doesn't trigger UI processing.
                                App.MainWindow.CustomTab1Badge.Value = 0;
                            }
                        }
                        else if (item.MoveTo == TerminalTarget.Terminal2)
                        {
                            Terminal2.Append(commLine);

                            if (!App.MainWindow.CustomTab2.IsSelected)
                            {
                                App.MainWindow.CustomTab2Badge.Value++;
                            }
                            else if (App.MainWindow.CustomTab2.IsSelected && App.MainWindow.CustomTab2Badge.Value != 0)
                            {
                                // Only setting this if the value isn't 0 so it doesn't trigger UI processing.
                                App.MainWindow.CustomTab2Badge.Value = 0;
                            }
                        }
                        else if (item.MoveTo == TerminalTarget.Terminal3)
                        {
                            Terminal3.Append(commLine);

                            if (!App.MainWindow.CustomTab3.IsSelected)
                            {
                                App.MainWindow.CustomTab3Badge.Value++;
                            }
                            else if (App.MainWindow.CustomTab3.IsSelected && App.MainWindow.CustomTab3Badge.Value != 0)
                            {
                                // Only setting this if the value isn't 0 so it doesn't trigger UI processing.
                                App.MainWindow.CustomTab3Badge.Value = 0;
                            }
                        }
                    }

                    // So, if this trigger matches and i has StopProcessing set it will not process any trigger
                    // thereafter.  This lets a savvy user setup a very efficient trigger processing pipeline but
                    // can potentially cause issues if they have something that stops processing but didn't intend
                    // for it (since it would not fire any triggers after).  All triggers are set to process by
                    // default.
                    if (item.StopProcessing)
                    {
                        // To help with debugging.
                        if (App.Settings.AvalonSettings.Debug)
                        {
                            App.Conveyor.EchoLog("Regular trigger matched that stops the processing of the rest of the trigger list.", LogType.Debug);
                        }

                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Process the replacement triggers.  These will do a simple replacement on the line OR execute a Lua script
        /// that will return a string value that will become the new line.  For performance we allow this to be turned
        /// off in the settings.
        /// </summary>
        /// <param name="line"></param>
        public void ProcessReplacementTriggers(Line line)
        {
            if (App.Settings.ProfileSettings.ReplacementTriggersEnabled && App.Settings.ProfileSettings.ReplacementTriggerList.Any())
            {
                bool found = false;
                var sb2 = ZString.CreateStringBuilder();
                
                foreach (var trigger in App.Settings.ProfileSettings.ReplacementTriggerList)
                {
                    if (!trigger.Enabled)
                    {
                        continue;
                    }

                    // Simple replacement can go ahead and flop %1.. variables in, the Lua version shouldn't
                    // do that in IsMatch as it will pass those variables to Lua which should handle them.
                    var match = trigger.IsMatch(line.Text);

                    if (match.TrySuccess())
                    {
                        // We know if it's a success and it's found hasn't been set yet that we will need
                        // to process it AND the StringBuilder needs to be populated because this is the
                        // first match (of potentially more).  No point in populating the StringBuilder until
                        // we know we're going to need it.
                        if (!found)
                        {
                            sb2.AppendLine(line.Text);
                        }

                        found = true;

                        // If lua function is available, run it, otherwise, do the processed replacement
                        if (string.IsNullOrWhiteSpace(trigger.OnMatchEvent))
                        {
                            sb2.Replace(match.Value, trigger.ProcessedReplacement);
                        }
                        else
                        {
                            try
                            {
                                // Create our param list to pass to the cached Lua function.
                                var sb = Argus.Memory.StringBuilderPool.Take();
                                sb.Append(trigger.OnMatchEvent);

                                //var paramList = new string[match.Groups.Count + 1];
                                sb.Replace("%0", line.Text);

                                for (int i = 1; i < match.Groups.Count; i++)
                                {
                                    sb.Replace($"%{i.ToString()}", match.Groups[i].Value);
                                }

                                var paramList = new string[match.Groups.Count];
                                paramList[0] = line.Text;

                                for (int i = 1; i < match.Groups.Count; i++)
                                {
                                    paramList[i] = match.Groups[i].Value;
                                }

                                //string luaResult = Interp.ScriptHost.MoonSharp.Execute<string>(sb.ToString());
                                string luaResult = Interp.ScriptHost.MoonSharp.ExecuteFunction<string>(trigger.FunctionName, paramList);

                                if (!string.IsNullOrEmpty(luaResult))
                                {
                                    sb2.Replace(match.Value, luaResult);
                                    found = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                // If an error occurs, make the found false so it doesn't replace anything with a null or empty.
                                App.Conveyor.EchoError("An error occurred executing Lua on a replacement trigger.");
                                App.Conveyor.EchoError(ex.Message);
                                found = false;
                            }
                        }
                    }
                }

                if (found)
                {
                    // Since looking at the document text creates a string every time, we're going to try to look at only the 
                    // last line plus the line terminator, since this should be processed right after a line is rendered to the
                    // terminal should work (because creating a 50,000 line string every pass is a not great approach).
                    int pos = Math.Clamp(GameTerminal.Document.TextLength - line.FormattedText.Length - 2, 0, GameTerminal.Document.TextLength);
                    int start = GameTerminal.Document.LastIndexOf(line.FormattedText, pos, GameTerminal.Document.TextLength - pos, StringComparison.Ordinal);

                    // Colorize, then remove 1 for the line ending.
                    // TODO if entire line is removed it messes up the gag.. figure that out.
                    if (start >= 0)
                    {
                        Colorizer.MudToAnsiColorCodes(ref sb2);
                        this.GameTerminal.Document.Remove(start, line.FormattedText.Length + 1);
                        this.GameTerminal.Document.Insert(start, sb2.ToString());
                    }
                }

                // Return the ZString StringBuilder via Dispose.
                sb2.Dispose();
            }
        }
    }
}