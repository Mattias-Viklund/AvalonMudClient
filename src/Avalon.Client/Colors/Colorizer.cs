﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Argus.Extensions;
using Avalon.Common.Colors;

namespace Avalon.Colors
{
    /// <summary>
    /// Class that handles mapping different color representations together.
    /// </summary>
    public static class Colorizer
    {

        /// <summary>
        /// Regular expression to find the last ANSI color code used.
        /// </summary>
        private static readonly Regex _escapeSequenceRegEx = new Regex(@"\x1B\[[^@-~]*[@-~]", RegexOptions.Compiled);

        /// <summary>
        /// Static list of colors we support that allows for translation between the different formats.
        /// </summary>
        /// <remarks>
        /// Black is notable here because it is set to AnsiColors.DarkGray.  The reason for this is we don't want black text to
        /// render on the black mud client background (for now).
        /// </remarks>
        public static List<ColorMap> ColorMap = new List<ColorMap>
            {
                // These should be in the order they need to be processed.
                new ColorMap { AnsiColor = AnsiColors.Clear, Brush = Brushes.LightGray },
                new ColorMap { AnsiColor = AnsiColors.Green, Brush = Brushes.Lime },
                new ColorMap { AnsiColor = AnsiColors.DarkGreen, Brush = Brushes.Green },
                new ColorMap { AnsiColor = AnsiColors.White, Brush = Brushes.White },
                new ColorMap { AnsiColor = AnsiColors.LightGray, Brush = Brushes.LightGray },
                new ColorMap { AnsiColor = AnsiColors.DarkGray, Brush = Brushes.Gray },
                new ColorMap { AnsiColor = AnsiColors.Black, Brush = Brushes.DarkGray },
                new ColorMap { AnsiColor = AnsiColors.Red, Brush = Brushes.Red },
                new ColorMap { AnsiColor = AnsiColors.DarkRed, Brush = Brushes.DarkRed },
                new ColorMap { AnsiColor = AnsiColors.Blue, Brush = Brushes.Blue },
                new ColorMap { AnsiColor = AnsiColors.DarkBlue, Brush = Brushes.DarkBlue },
                new ColorMap { AnsiColor = AnsiColors.Yellow, Brush = Brushes.Yellow},
                new ColorMap { AnsiColor = AnsiColors.DarkYellow, Brush = Brushes.Gold },
                new ColorMap { AnsiColor = AnsiColors.Cyan, Brush = Brushes.Cyan },
                new ColorMap { AnsiColor = AnsiColors.DarkCyan, Brush = Brushes.DarkCyan },
                new ColorMap { AnsiColor = AnsiColors.Purple, Brush = Brushes.Magenta },
                new ColorMap { AnsiColor = AnsiColors.DarkPurple, Brush = Brushes.DarkMagenta },
                new ColorMap { AnsiColor = AnsiColors.Orange, Brush = Brushes.Orange },
                new ColorMap { AnsiColor = AnsiColors.Pink, Brush = Brushes.HotPink },
                new ColorMap { AnsiColor = AnsiColors.Brown, Brush = Brushes.Brown },
                new ColorMap { AnsiColor = AnsiColors.Magenta, Brush = Brushes.MediumPurple }
            };

        /// <summary>
        /// Static list of styles we support that allows for translation between the different formats.  These will generally
        /// have no color so the AnsiColor will be ignored (unless we add custom control sequences to do things inside this
        /// client for the client).
        /// </summary>
        public static List<ColorMap> StyleMap = new List<ColorMap>
            {
                new ColorMap { AnsiColor = AnsiColors.Reverse, Brush = Brushes.Transparent },
                new ColorMap { AnsiColor = AnsiColors.Underline, Brush = Brushes.Transparent }
                // Remove ANSI codes that deal with moving the cursor around and one's we aren't currently supporting.
                //new ColorMap { Name = "Left", AnsiColorCode = "\x1B[1D", MudColorCode = "", Color = Brushes.Transparent },
                //new ColorMap { Name = "Right", AnsiColorCode = "\x1B[1C", MudColorCode = "", Color = Brushes.Transparent },
                //new ColorMap { Name = "Up", AnsiColorCode = "\x1B[1A", MudColorCode = "", Color = Brushes.Transparent },
                //new ColorMap { Name = "Down", AnsiColorCode = "\x1B[1B", MudColorCode = "", Color = Brushes.Transparent },
                //new ColorMap { Name = "Back", AnsiColorCode = "\x1B[1D", MudColorCode = "", Color = Brushes.Transparent },
                //new ColorMap { Name = "Blink", AnsiColorCode = "\x1B[5m", MudColorCode = "", Color = Brushes.Transparent },
            };

        /// <summary>
        /// Returns a color by the name (case insensitive).
        /// </summary>
        /// <param name="name">The friendly name of the color.</param>
        public static ColorMap ColorMapByName(string name)
        {
            return ColorMap.Find(x => string.Equals(x.AnsiColor.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Removes all known ANSI codes from the output text.
        /// </summary>
        /// <param name="buf"></param>
        public static string RemoveAllAnsiCodes(string buf)
        {
            // If there are ANSI codes don't bother RegEx matching, return the provided
            // value back to the caller.
            if (buf.IndexOf('\x1B') == -1)
            {
                return buf;
            }

            return _escapeSequenceRegEx.Replace(buf, "");
        }

        /// <summary>
        /// Removes all known ANSI codes from the output text.  Note: This updates the StringBuilder directly.
        /// </summary>
        /// <param name="sb"></param>
        public static void RemoveAllAnsiCodes(StringBuilder sb)
        {
            // If there are ANSI codes don't bother RegEx matching.
            if (sb.IndexOf('\x1B') == -1)
            {
                return;
            }

            var result = _escapeSequenceRegEx.Matches(sb.ToString());

            for (int i = result.Count - 1; i > -1; i--)
            {
                var m = result[i];
                sb.Remove(m.Index, m.Length);
            }
        }

        /// <summary>
        /// Converts ANSI color codes into mud color codes.  Note: This updates the StringBuilder directly.
        /// </summary>
        /// <param name="sb"></param>
        public static void AnsiToMudColorCodes(StringBuilder sb)
        {
            // If there are no mud color codes don't bother loop through looking for them.
            if (sb.IndexOf('\x1B') == -1)
            {
                return;
            }

            foreach (var item in ColorMap)
            {
                sb.Replace(item.AnsiColor.ToString(), item.AnsiColor.MudColorCode);
            }
        }

        /// <summary>
        /// Converts mud color codes into ANSI color codes.  Note: This updates the StringBuilder directly.
        /// </summary>
        /// <param name="sb"></param>
        public static void MudToAnsiColorCodes(StringBuilder sb)
        {
            // If there are no color codes don't bother loop through the replacements.
            if (sb.IndexOf('{') == -1)
            {
                return;
            }

            // First the colors
            foreach (var item in ColorMap.Where(x => !string.IsNullOrWhiteSpace(x.AnsiColor.MudColorCode)))
            {
                sb.Replace(item.AnsiColor.MudColorCode, item.AnsiColor.ToString());
            }

            // Next the styles
            foreach (var item in StyleMap.Where(x => !string.IsNullOrWhiteSpace(x.AnsiColor.MudColorCode)))
            {
                sb.Replace(item.AnsiColor.MudColorCode, item.AnsiColor.ToString());
            }
        }

    }
}
