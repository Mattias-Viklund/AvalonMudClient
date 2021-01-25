﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalon.Colors;
using Avalon.Common.Models;
using Avalon.Controls;
using Avalon.Utilities;

namespace Avalon
{
    /// <summary>
    /// Partial class for network related events and code of the MainWindow.
    /// </summary>
    public partial class MainWindow
    {

        /// <summary>
        /// Connects to the server for the profile that is currently loaded.
        /// </summary>
        public void Connect()
        {
            // Activate any plugins for this game.
            ActivatePlugins();

            // Connect, then put the focus into the input text box.
            Interp.Connect(HandleLineReceived, this.HandleDataReceived, HandleConnectionClosed);            
            TitleBar.IsConnected = true;
            MenuNetworkButton.Header = "Disconnect";
            TextInput.Focus();

            // If a command has been defined to send after connect then check it and fire it off here.
            if (!string.IsNullOrWhiteSpace(App.Settings.ProfileSettings.OnConnect))
            {
                var source = new CancellationTokenSource();

                var t = Task.Run(async delegate
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(App.Settings.ProfileSettings.OnConnectDelayMilliseconds), source.Token);

                    Application.Current.Dispatcher.Invoke(new Action(async () =>
                    {
                        await Interp.Send(App.Settings.ProfileSettings.OnConnect);
                    }));
                });
            }
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            Interp.Disconnect();
            TitleBar.IsConnected = false;
            MenuNetworkButton.Header = "Connect";
        }

        /// <summary>
        /// Connects or disconnects the mud client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuNetworkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TitleBar.IsConnected == false)
                {
                    App.MainWindow.Connect();
                }
                else
                {
                    App.MainWindow.Disconnect();
                }
            }
            catch (Exception ex)
            {
                App.MainWindow.Interp.Conveyor.EchoLog($"Network Failure: {ex.Message}", LogType.Error);
            }
        }

        /// <summary>
        /// Handles when a connection is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HandleConnectionClosed(object sender, EventArgs e)
        {
            TitleBar.IsConnected = false;
            App.Conveyor.EchoLog($"Disconnected: {DateTime.Now}", LogType.Warning);
            Interp.Telnet = null;
        }

        /// <summary>
        /// This fires when a complete line has been sent from the mud.  It has already been rendered
        /// to the terminal window at this point so we want to do processing such as triggers at this
        /// point.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HandleLineReceived(object sender, string e)
        {
            // The "Text" on a line isn't always important which is why we don't always set it.  It
            // is critical in this location though in order to match triggers.
            var line = new Line
            {
                FormattedText = e,
                Text = Colorizer.RemoveAllAnsiCodes(e),
            };

            // Beep is checked here because it should only happen one time, not everytime a trigger is checked which for
            // gags can be a lot.  We'll do this when the line comes in -if- the user has the setting for it turned on.
            // One beep per line max, everything else is ignored.
            if (App.Settings.ProfileSettings.AnsiBeep && e.Contains('\a'))
            {
                App.Beep.Play();
            }

            CheckTriggers(line);

            // Check to see if we're scraping and if so append to the StringBuilder that holds the scraped text.
            // Scraping will only occur when full lines come through.
            if (App.Conveyor.ScrapeEnabled)
            {
                App.Conveyor.Scrape.Append(line.Text.AsSpan()).AppendLine();
            }
        }

        /// <summary>
        /// This is fired anytime new data is received whether it is a newline or not.  This will allow us to
        /// start putting that data into the window so the user can see it (even if the trigger can't fire until
        /// the full line is available).  Data provided here may or may not be a complete line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HandleDataReceived(object sender, string e)
        {
            try
            {
                // Get a StringBuilder from the shared pool.
                var sb = Argus.Memory.StringBuilderPool.Take();
                sb.Append(e);

                // Remove unwanted characters
                sb.RemoveUnsupportedCharacters();

                // Append the data to the terminal as it comes in.
                GameTerminal.Append(sb);

                // If the back buffer setting is enabled put the data also in there.
                if (App.Settings.AvalonSettings.BackBufferEnabled)
                {
                    GameBackBufferTerminal.Append(sb, false);
                }

                // Return the StringBuilder to the pool.
                Argus.Memory.StringBuilderPool.Return(sb);
            }
            catch (Exception ex)
            {
                this.Interp.Conveyor.EchoError($"ERROR (HandleDataReceived): {ex.Message}");
            }
        }

        /// <summary>
        /// Handles text from the interpreter that needs to be echo'd out to the terminal.  This text
        /// does not get checked for triggers, it's an echo that's coming from the interpreter or
        /// the possibly the Conveyor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InterpreterEcho(object sender, EventArgs e)
        {
            var ea = e as EchoEventArgs;
            AvalonTerminal term;

            if (ea == null)
            {
                this.Interp.Conveyor.EchoError(" Null EchoEventArgs in InterpreterEcho");
                return;
            }

            switch (ea.Terminal)
            {
                case TerminalTarget.None:
                case TerminalTarget.Main:
                    term = GameTerminal;
                    break;
                case TerminalTarget.Terminal1:
                    term = Terminal1;
                    CustomTab1Badge.IncrementOrReset(!CustomTab1.IsSelected);
                    break;
                case TerminalTarget.Terminal2:
                    term = Terminal2;
                    CustomTab2Badge.IncrementOrReset(!CustomTab2.IsSelected);
                    break;
                case TerminalTarget.Terminal3:
                    term = Terminal3;
                    CustomTab3Badge.IncrementOrReset(!CustomTab3.IsSelected);
                    break;
                default:
                    term = GameTerminal;
                    break;
            }

            if (ea.UseDefaultColors)
            {
                term.Append(ea.Text);

                // If the back buffer setting is enabled put the data also in there.
                if (App.Settings.AvalonSettings.BackBufferEnabled && ea.Terminal == TerminalTarget.Main)
                {
                    GameBackBufferTerminal.Append(ea.Text, false);
                }
            }
            else
            {
                term.Append(ea.Text, ea.ForegroundColor, ea.ReverseColors);

                // If the back buffer setting is enabled put the data also in there.
                if (App.Settings.AvalonSettings.BackBufferEnabled && ea.Terminal == TerminalTarget.Main)
                {
                    GameBackBufferTerminal.Append(ea.Text, ea.ForegroundColor, ea.ReverseColors, false);
                }
            }
        }

    }
}
