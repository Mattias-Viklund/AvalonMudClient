﻿using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using Avalon.Lua;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ModernWpf.Controls;

namespace Avalon
{
    /// <summary>
    /// A simple Lua highlighted text editor for use with editing Lua scripts.
    /// </summary>
    public partial class StringEditor : Window
    {
        /// <summary>
        /// The value of the Lua text editor.
        /// </summary>
        public string Text
        {
            get => AvalonLuaEditor.Text;
            set => AvalonLuaEditor.Text = value;
        }

        /// <summary>
        /// The text for the status bar.
        /// </summary>
        public string StatusText
        {
            get => TextBlockStatus.Text;
            set => TextBlockStatus.Text = value;
        }

        private EditorType _editorMode;

        public EditorType EditorMode
        {
            get => _editorMode;
            set
            {
                _editorMode = value;

                switch (_editorMode)
                {
                    case EditorType.Text:
                        this.Title = "Text Editor";
                        break;
                    case EditorType.Lua:
                        this.Title = "Lua Editor";

                        var asm = Assembly.GetExecutingAssembly();
                        string resourceName = $"{asm.GetName().Name}.Resources.LuaDarkTheme.xshd";

                        using (var s = asm.GetManifestResourceStream(resourceName))
                        {
                            using (var reader = new XmlTextReader(s))
                            {
                                AvalonLuaEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                            }
                        }

                        this.StatusText = "Press [F1] for code snippets or type 'lua.' to see custom functions.";

                        break;
                }
            }
        }

        public enum EditorType
        {
            Text,
            Lua
        }

        /// <summary>
        /// Used for autocompletion with Lua.
        /// </summary>
        CompletionWindow _completionWindow;

        /// <summary>
        /// Constructor.
        /// </summary>
        public StringEditor()
        {
            InitializeComponent();

            AvalonLuaEditor.TextArea.TextEntering += AvalonLuaEditor_TextEntering;
            AvalonLuaEditor.TextArea.TextEntered += AvalonLuaEditor_TextEntered;
            AvalonLuaEditor.Options.ConvertTabsToSpaces = true;
        }

        /// <summary>
        /// Gets or sets the text of the action button.
        /// </summary>
        public string ActionButtonText
        {
            get => ButtonSave.Content.ToString();
            set => ButtonSave.Content = value;
        }

        /// <summary>
        /// Fires when the Window is loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StringEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AvalonLuaEditor.Focus();
        }

        private void AvalonLuaEditor_TextEntered(object sender, TextCompositionEventArgs e)
        {
            // Text colon or dot, find the word before it.
            if (e.Text == "." || e.Text == ":")
            {
                string word = GetWordBefore(AvalonLuaEditor);

                if (word == "lua")
                {
                    // Open code completion after the user has pressed dot:
                    _completionWindow = new CompletionWindow(AvalonLuaEditor.TextArea);
                    var data = _completionWindow.CompletionList.CompletionData;
                    LuaCompletion.LoadCompletionData(data, word);
                }

                if (_completionWindow != null)
                {
                    _completionWindow.Show();
                    _completionWindow.Closed += (sender, args) => _completionWindow = null;
                }

                //_completionWindow.PreviewTextInput += (sender, args) =>
                //{
                //    if (args.Text == "(")
                //    {
                //        _completionWindow.CompletionList.RequestInsertion(EventArgs.Empty);
                //    }
                //    var c = args.Text[args.Text.Length - 1];
                //    args.Handled = !char.IsLetterOrDigit(c) && c != '_';
                //};

            }
        }

        private void AvalonLuaEditor_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && _completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    _completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }

        private void AvalonLuaEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                // Open code completion after the user has pressed dot:
                _completionWindow = new CompletionWindow(AvalonLuaEditor.TextArea);
                var data = _completionWindow.CompletionList.CompletionData;
                LuaCompletion.LoadCompletionDataSnippits(data);

                _completionWindow.Show();
                _completionWindow.Closed += (sender, args) => _completionWindow = null;

            }
        }

        /// <summary>
        /// Code that is executed for the Cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// Code that is executed for the Save button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonSave_OnClick(object sender, RoutedEventArgs e)
        {
            // Allow Lua validating to be turned off, but if it's on attempt to use LoadString to check
            // for any blatant syntax errors.
            if (this.EditorMode == EditorType.Lua && App.Settings.AvalonSettings.ValidateLua)
            {
                var luaResult = await App.MainWindow.Interp.LuaCaller.ValidateAsync(this.Text);
                
                if (!luaResult.Success && luaResult.Exception != null)
                {
                    string buf = $"An error occurred on line {luaResult.Exception.ToLineNumber}\r\nMessage: {luaResult?.Exception?.Message ?? "N/A"}\r\n\r\nWould you still like to save?";

                    var confirmDialog = new YesNoDialog()
                    {
                        Title = "Syntax Error",
                        Content = buf,
                        PrimaryButtonText = "Yes",
                        SecondaryButtonText = "No"
                    };

                    var result = await confirmDialog.ShowAsync();

                    if (result == ContentDialogResult.Secondary)
                    {
                        return;
                    }
                }
            }

            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// Gets the word before the caret.  This seems to work accidentally.  Go through this when
        /// new use cases come up if wonky behavior occurs.
        /// </summary>
        /// <param name="textEditor"></param>
        public static string GetWordBefore(TextEditor textEditor)
        {
            var wordBeforeDot = string.Empty;
            var caretPosition = textEditor.CaretOffset - 2;
            var lineOffset = textEditor.Document.GetOffset(textEditor.Document.GetLocation(caretPosition));
            string text = textEditor.Document.GetText(lineOffset, 1);

            while (true)
            {
                if (text == null && text.CompareTo(' ') > 0)
                {
                    break;
                }
                if (Regex.IsMatch(text, @".*[^A-Za-z\. ]"))
                {
                    break;
                }

                if (text != "." && text != ":" && text != " ")
                {
                    wordBeforeDot = text + wordBeforeDot;
                }

                if (text == " ")
                {
                    break;
                }

                if (caretPosition == 0)
                {
                    break;
                }

                lineOffset = textEditor.Document.GetOffset(textEditor.Document.GetLocation(--caretPosition));

                text = textEditor.Document.GetText(lineOffset, 1);
            }

            return wordBeforeDot;
        }

    }
}
