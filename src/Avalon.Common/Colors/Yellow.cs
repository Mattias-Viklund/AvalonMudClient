﻿/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

namespace Avalon.Common.Colors
{
    public class Yellow : AnsiColor
    {
        public override string AnsiCode => "\x1B[1;33m";

        public override string MudColorCode => "{Y";

        public override string Name => "Yellow";
    }
}