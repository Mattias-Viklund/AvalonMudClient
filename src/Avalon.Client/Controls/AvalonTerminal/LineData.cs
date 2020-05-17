﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Avalon.Controls
{
    /// <summary>
    /// A class used to return data about the current state of a given line that the
    /// built-in AvalonEdit line doesn't return.  Not that if held this could go
    /// stale as it's a snapshot in time.
    /// </summary>
    public class LineData
    {
        public int LineNumber { get; set; }

        public string Text { get; set; }

        public string UnformattedText
        {
            get
            {
                return Avalon.Colors.Colorizer.RemoveAllAnsiCodes(this.Text);
            }
        }

        public bool IsGagged { get; set; }

        public bool IsEmptyOrWhitespace { get; set; }

        public bool IsDeleted { get; set; }

        public int LinesWithWrap { get; set; }

        public int Offset { get; set; }

        public int EndOffset { get; set; }

    }
}
