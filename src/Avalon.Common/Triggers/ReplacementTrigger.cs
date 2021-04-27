/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Avalon.Common.Interfaces;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Argus.Extensions;
using Avalon.Common.Scripting;
using Cysharp.Text;

namespace Avalon.Common.Triggers
{
    /// <inheritdoc cref="IReplacementTrigger" />
    public class ReplacementTrigger : IReplacementTrigger, INotifyPropertyChanged, IModelInfo
    {
        public ReplacementTrigger()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        private string _id;
        
        /// <inheritdoc/>
        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                this.FunctionName = string.Concat("x", value.Replace("-", "").DeleteLeft(1));
                OnPropertyChanged(nameof(Id));
            }
        }

        private string _packageId;

        /// <inheritdoc/>
        public string PackageId
        {
            get => _packageId;
            set
            {
                _packageId = value;
                OnPropertyChanged(nameof(PackageId));
            }
        }

        private string _pattern;

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
                finally
                {
                    // TODO Report.. will need Conveyor.
                    //this.Conveyor?.EchoLog($"Trigger creation error: {ex.Message}", LogType.Error);
                }
            }
        }

        private string _replacement;

        /// <inheritdoc/>
        public string Replacement
        {
            get => _replacement;
            set
            {
                _replacement = value;
                OnPropertyChanged(nameof(Replacement));
            }
        }

        /// <inheritdoc />
        [JsonIgnore]
        public string ProcessedReplacement { get; set; }

        private bool _enabled = true;

        /// <inheritdoc/>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged(nameof(Enabled));
            }
        }

        private string _group;

        /// <inheritdoc/>
        public string Group
        {
            get => _group;
            set
            {
                _group = value;
                OnPropertyChanged(nameof(Group));
            }
        }

        private string _onMatchEvent;

        /// <inheritdoc/>
        public string OnMatchEvent
        {
            get => _onMatchEvent;
            set
            {
                // Additionally load the script when this is changed.
                _onMatchEvent = value;
                OnPropertyChanged(nameof(OnMatchEvent));
            }
        }

        /// <summary>
        /// The name of the function for the OnMatchedEvent.
        /// </summary>
        [JsonIgnore]
        public string FunctionName { get; set; }

        private bool _temp = false;

        /// <inheritdoc />
        public bool Temp
        {
            get => _temp;
            set
            {
                _temp = value;
                OnPropertyChanged(nameof(Temp));
            }
        }

        /// <summary>
        /// A reference to the scripting environment.
        /// </summary>
        [JsonIgnore]
        public ScriptHost ScriptHost { get; set; }

        /// <summary>
        /// The RegEx compiled and used for this triggers pattern matching.
        /// </summary>
        [JsonIgnore]
        public Regex Regex { get; set; }

        /// <inheritdoc/>
        public Match IsMatch(string line)
        {
            if (string.IsNullOrEmpty(this.Pattern))
            {
                return null;
            }

            var m = this.Regex?.Match(line);

            if (m == null)
            {
                return null;
            }

            if (this.Replacement == null)
            {
                this.Replacement = "";
                this.ProcessedReplacement = "";
                return m;
            }

            // Start from the replacement defined by the user.
            using (var sb = ZString.CreateStringBuilder())
            {
                sb.Append(this.Replacement);

                // Go through any groups backwards that came back in the trigger match.  Groups are matched in reverse
                // order so that %1 doesn't overwrite %12 and leave a trailing 2.
                for (int i = m.Groups.Count - 1; i >= 0; i--)
                {
                    // Replace %1, %2, etc. variables with their values from the pattern match.  ToString() was
                    // called to avoid a boxing allocation.
                    sb.Replace($"%{i.ToString()}", m.Groups[i].Value);
                }

                this.ProcessedReplacement = sb.ToString();
            }

            return m;
        }

        /// <inheritdoc />
        public bool IsEmpty()
        {
            if (string.IsNullOrWhiteSpace(this.Pattern))
            {
                return true;
            }

            return false;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            PropertyChanged?.Invoke(this, e);

        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
