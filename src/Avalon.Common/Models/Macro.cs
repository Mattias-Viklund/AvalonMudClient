﻿using System.ComponentModel;

namespace Avalon.Common.Models
{
    /// <summary>
    /// A command that should be executed when the specified key is pressed.  Currently the Key is stored as
    /// an int and maps to the WPF System.Windows.Input.Key.  In the future if UWP or other frameworks use something
    /// like VirtualKey that can additionally be added as another int so they effectively becomes both the Macro and
    /// the map to what it represents in each environment (without specifically references those types so this library
    /// can stay a .NET Standard library).
    /// </summary>
    public class Macro : INotifyPropertyChanged
    {

        public Macro(int key, string keyDescription, string command)
        {
            this.Key = key;
            this.KeyDescription = keyDescription;
            this.Command = command;
        }

        public Macro(int key, string command)
        {
            this.Key = key;
            this.Command = command;
        }

        public Macro()
        {

        }

        private int _key = 0;
        public int Key 
        { 
            get
            {
                return _key;
            }
            set
            {
                _key = value;
                OnPropertyChanged(nameof(Key));
            }
        }

        private string _keyDescription = "";
        public string KeyDescription
        { 
            get
            {
                return _keyDescription;
            }
            set
            {
                _keyDescription = value;
                OnPropertyChanged(nameof(KeyDescription));
            }
        }

        private string _command = "";
        public string Command
        {
            get => _command;
            set
            {
                _command = value;
                OnPropertyChanged(nameof(Command));
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
