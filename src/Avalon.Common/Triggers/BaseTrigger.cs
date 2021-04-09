/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Avalon.Common.Interfaces;
using Avalon.Common.Models;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Avalon.Common.Triggers
{
    /// <summary>
    /// Base class implementation of the shared portions of Triggers.
    /// </summary>
    public abstract class BaseTrigger : ITrigger, ICloneable, INotifyPropertyChanged
    {
        public abstract bool IsMatch(string line);

        public abstract void Execute();

        /// <inheritdoc />
        public string Identifier { get; set; } = Guid.NewGuid().ToString();

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

        private string _command = "";

        /// <inheritdoc/>
        public virtual string Command
        {
            get => _command;
            set
            {
                _command = value;
                OnPropertyChanged(nameof(Command));
            }
        }

        private string _group = "";

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

        private bool _plugin = false;

        /// <inheritdoc/>
        public bool Plugin
        {
            get => _plugin;

            set
            {
                if (value != _plugin)
                {
                    _plugin = value;
                    OnPropertyChanged(nameof(Plugin));
                }
            }
        }

        private bool _lock = false;

        /// <inheritdoc/>
        public bool Lock
        {
            get => _lock;

            set
            {
                if (value != _lock)
                {
                    _lock = value;
                    OnPropertyChanged(nameof(Lock));
                }
            }
        }

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

        private int _count = 0;

        /// <inheritdoc />
        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                OnPropertyChanged(nameof(Count));
            }
        }

        private int _priority = 10000;

        /// <inheritdoc />
        public int Priority
        {
            get => _priority;
            set
            {
                _priority = value;
                OnPropertyChanged(nameof(Priority));
            }
        }

        private string _character = "";

        /// <inheritdoc/>
        public string Character
        {
            get => _character;
            set
            {
                _character = value;
                OnPropertyChanged(nameof(Character));
            }
        }

        private ExecuteType _executeType = ExecuteType.Command;

        /// <inheritdoc/>
        public ExecuteType ExecuteAs
        {
            get => _executeType;
            set
            {
                _executeType = value;
                OnPropertyChanged(nameof(ExecuteAs));
            }
        }

        /// <inheritdoc />
        [JsonIgnore]
        public Regex Regex { get; set; }

        /// <inheritdoc />
        public bool SystemTrigger { get; set; } = false;

        /// <inheritdoc />
        public string PackageId { get; set; } = "";

        /// <inheritdoc/>
        public DateTime LastMatched { get; set; } = DateTime.MinValue;

        /// <inheritdoc />
        [JsonIgnore]
        public IConveyor Conveyor { get; set; }

        /// <summary>
        /// Clones the trigger.
        /// </summary>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            PropertyChanged?.Invoke(this, e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
