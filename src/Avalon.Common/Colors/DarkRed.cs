﻿/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2023 All rights reserved.
 * @license           : MIT
 */

namespace Avalon.Common.Colors
{
    public class DarkRed : AnsiColor
    {
        public override string AnsiCode => "\x1B[0;31m";

        public override string MudColorCode => "{r";

        public override string Name => "Dark Red";
    }
}
