/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Argus.Extensions;
using Avalon.Common.Models;
using Cysharp.Text;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Avalon.Lua;

namespace Avalon.Common.Triggers
{
    /// <summary>
    /// A trigger is an action that is executed based off of a pattern that is sent from the game.
    /// </summary>
    public class Trigger : BaseTrigger
    {
        public Trigger()
        {
        }

        public Trigger(string pattern, string command, string character = "", bool isSilent = false, string identifier = "",
                        TerminalTarget moveTo = TerminalTarget.None, bool gag = false, string group = "", bool disableAfterTriggered = false,
                        bool enabled = true, bool highlightLine = false, bool isLua = false, bool variableReplacement = false, bool systemTrigger = false,
                        int priority = 10000, bool stopProcessing = false)
        {
            this.Pattern = pattern;
            this.Command = command;
            this.Character = character;
            this.IsSilent = isSilent;
            this.Identifier = identifier;
            this.LuaScript = new LuaScript(identifier);
            this.MoveTo = moveTo;
            this.Gag = gag;
            this.Group = group;
            this.DisableAfterTriggered = disableAfterTriggered;
            this.Enabled = enabled;
            this.HighlightLine = highlightLine;
            this.IsLua = isLua;
            this.VariableReplacement = variableReplacement;
            this.SystemTrigger = systemTrigger;
            this.Priority = priority;
            this.StopProcessing = stopProcessing;
        }

        public Trigger(string pattern, string command, string character, bool isSilent, string identifier)
        {
            this.Pattern = pattern;
            this.Command = command;
            this.Character = character;
            this.Identifier = identifier;
            this.LuaScript = new LuaScript(identifier);
            this.IsSilent = isSilent;
        }

        public Trigger(string pattern, string command, string character, bool isSilent, string identifier, TerminalTarget moveTo, bool gag)
        {
            this.Pattern = pattern;
            this.Command = command;
            this.Character = character;
            this.Identifier = identifier;
            this.LuaScript = new LuaScript(identifier);
            this.IsSilent = isSilent;
            this.MoveTo = moveTo;
            this.Gag = gag;
        }

        /// <inheritdoc/>
        public override bool IsMatch(string line)
        {
            Match match;

            // Does this trigger contain any variables?  If so, we'll need to special handle it.  We're also
            // going to require that the VariableReplacement value is set to true so the player has to
            // specifically opt into this.  Since the Gag triggers run -a lot- on the terminal rendering
            // the bool will much faster as a first check before the string contains check.  This is
            // a micro optimization that had real payoff in the performance profiler.  Also when profiling, IndexOf
            // a char without Ordinal consistently ran faster than Contains and IndexOf with Ordinal.
            if (this.VariableReplacement && Pattern.IndexOf('@') >= 0)
            {
                // Replace any variables with their literal values.
                string tempPattern = this.Conveyor.ReplaceVariablesWithValue(Pattern);
                var tempRegex = new Regex(tempPattern, RegexOptions.IgnoreCase);
                match = tempRegex.Match(line);
            }
            else
            {
                // Run the match normal Match, this will be most all cases.
                match = this.Regex?.Match(line);
            }

            // If it's not a match, get out.
            if (match == null || !match.Success)
            {
                return false;
            }

            // If it's supposed to auto disable itself after it fires then set that.
            if (this.DisableAfterTriggered)
            {
                this.Enabled = false;
            }

            // Save the match for CLR processing if needed.
            this.Match = match;

            // This is the block that swaps matched groups into the processed command as the user
            // has requested (e.g. %0, %1, %2, %3, etc.)
            {
                // Save the text that triggered this trigger so that it can be used if needed elsewhere like in
                // a CLR trigger.
                TriggeringText = line;

                using (var sb = ZString.CreateStringBuilder())
                {
                    // Set the command that we may or may not process.  Allow the user to have the content of
                    // the last trigger if they need it.
                    sb.Append(this.Command?.Replace("%0", TriggeringText) ?? "");

                    // Go through any groups backwards that came back in the trigger match.  Groups are matched in reverse
                    // order so that %1 doesn't overwrite %12 and leave a trailing 2.
                    for (int i = match.Groups.Count - 1; i >= 0; i--)
                    {
                        // If it's a named match, we specifically named it in the trigger and thus we're going
                        // to automatically store it in a variable that can then be used later by aliases, triggers, etc.
                        // If there are variables that came back that aren't named, throw those into the more generic
                        // %1, %2, %3 values.  TODO - Is this right.. seems like it should do both if needed?
                        if (!string.IsNullOrWhiteSpace(match.Groups[i].Name) && !match.Groups[i].Name.IsNumeric() && !string.IsNullOrWhiteSpace(match.Groups[i].Value))
                        {
                            Conveyor.SetVariable(match.Groups[i].Name, match.Groups[i].Value);
                        }
                        else
                        {
                            // Replace %1, %2, etc. variables with their values from the pattern match.  ToString() was
                            // called to avoid a boxing allocation.  If it's Lua we're not going to swap these values
                            // in.  The reason for this is that Lua parameters are passed in via parameters so the script
                            // can be re-used which is MUCH more memory and CPU efficient.
                            sb.Replace($"%{i.ToString()}", match.Groups[i].Value);
                        }
                    }

                    ProcessedCommand = sb.ToString();
                }
            }

            // If the profile setting to track the last trigger date is set then set it.
            if (this.Conveyor?.ProfileSettings?.TrackTriggerLastMatched == true)
            {
                this.LastMatched = DateTime.Now;
            }

            return match.Success;
        }

        public override void Execute()
        {

        }

        private string _command = "";

        public new virtual string Command
        {
            get => _command;
            set
            {
                _command = value;

                if (this.LuaScript == null)
                {
                    this.LuaScript = new LuaScript(this.Identifier);
                }

                this.LuaScript.Code = value;
                this.LuaScript.Updated = true;
                OnPropertyChanged(nameof(Command));
            }
        }

        /// <summary>
        /// The command after it's been processed.  This is what should get sent to the game.
        /// </summary>
        [JsonIgnore]
        public string ProcessedCommand { get; private set; } = "";

        /// <summary>
        /// The text that triggered the trigger.
        /// </summary>
        [JsonIgnore]
        public string TriggeringText { get; private set; } = "";

        [JsonIgnore]
        public Match Match { get; set; }

        private string _pattern = "";

        /// <inheritdoc/>
        public string Pattern
        {
            get => _pattern;
            set
            {
                try
                {
                    // Only set the pattern if it compiled.
                    this.Regex = new Regex(value, RegexOptions.Compiled);
                    _pattern = value;
                    OnPropertyChanged(nameof(Pattern));
                }
                catch (Exception ex)
                {
                    this.Conveyor?.EchoLog($"Trigger creation error: {ex.Message}", LogType.Error);
                }
            }
        }

        private bool _isSilent = false;

        /// <summary>
        /// Whether the triggers output should be silent (not echo to the main terminal).
        /// </summary>
        public bool IsSilent
        {
            get => _isSilent;

            set
            {
                if (value != _isSilent)
                {
                    _isSilent = value;
                    OnPropertyChanged(nameof(IsSilent));
                }
            }
        }

        private bool _isLua = false;

        /// <summary>
        /// Whether the command should be executed as a Lua script.
        /// </summary>
        public bool IsLua
        {
            get => _isLua;

            set
            {
                if (value != _isLua)
                {
                    _isLua = value;
                    OnPropertyChanged(nameof(IsLua));
                }
            }
        }

        /// <summary>
        /// Represents a Lua script object.
        /// </summary>
        [JsonIgnore]
        public LuaScript LuaScript { get; set; }

        private bool _disableAfterTriggered = false;

        /// <summary>
        /// If set to true will disable the trigger after it fires.
        /// </summary>
        public bool DisableAfterTriggered
        {
            get => _disableAfterTriggered;

            set
            {
                if (value != _disableAfterTriggered)
                {
                    _disableAfterTriggered = value;
                    OnPropertyChanged(nameof(DisableAfterTriggered));
                }
            }
        }

        private bool _variableReplacement = false;

        /// <summary>
        /// Whether or not variables should be replaced in the pattern.  This is offered as
        /// a performance tweak so the player has to opt into it.
        /// </summary>
        public bool VariableReplacement
        {
            get => _variableReplacement;
            set
            {
                _variableReplacement = value;
                OnPropertyChanged(nameof(VariableReplacement));
            }
        }

        private bool _gag = false;

        /// <summary>
        /// Whether or not the matching line should be gagged from terminal.  A gagged line is hidden from view
        /// as if it does not exist but does in fact still exist in the terminal.  If triggers are disabled you
        /// will see gagged lines re-appear.  Further, gagged lines will appear in clipboard actions such as copy.
        /// </summary>
        public bool Gag
        {
            get => _gag;
            set
            {
                if (value != _gag)
                {
                    _gag = value;
                    OnPropertyChanged(nameof(Gag));
                }
            }
        }

        private TerminalTarget _moveTo = TerminalTarget.None;

        /// <summary>
        /// What terminal window to move the triggered line to.
        /// </summary>
        public TerminalTarget MoveTo
        {
            get => _moveTo;
            set
            {
                if (value != _moveTo)
                {
                    _moveTo = value;
                    OnPropertyChanged(nameof(MoveTo));
                }
            }
        }

        private bool _highlightLine = false;

        /// <summary>
        /// Whether or not the matching line should be highlighted.
        /// </summary>
        public bool HighlightLine
        {
            get => _highlightLine;
            set
            {
                if (value != _highlightLine)
                {
                    _highlightLine = value;
                    OnPropertyChanged(nameof(HighlightLine));
                }
            }
        }

        private bool _stopProcessing = false;

        /// <summary>
        /// If StopProcessing is true then the trigger processing function will stop processing any triggers after
        /// the trigger that fired here.  In order for that to happen, the trigger will need to match.  This will
        /// allow a player to allow for a very efficient trigger loop (but could also cause problems if use incorrectly
        /// in that it will stop trigger processing when this fires).  One thing to note, this is for general purpose
        /// triggers that the user executes but it does not apply to Gag triggers.  Gag triggers inherently work will
        /// gag an entire line and they stop processing as soon as one matches.
        /// </summary>
        public bool StopProcessing
        {
            get => _stopProcessing;
            set
            {
                _stopProcessing = value;
                OnPropertyChanged(nameof(StopProcessing));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            PropertyChanged?.Invoke(this, e);

        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}