/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media.TextFormatting;

namespace Avalon.Controls
{
    /// <summary>
    /// Represents a hidden element in the text editor.
    /// </summary>
    public class HiddenTextElement : VisualLineElement
    {
        public HiddenTextElement(int documentLength) : base(1, documentLength)
        {
        }

        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        {
            return new TextHidden(1);
        }
    }
}