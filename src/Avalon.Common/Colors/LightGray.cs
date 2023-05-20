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
    public class LightGray : AnsiColor
    {
        public override string AnsiCode => "\x1B[0;37m";

        public override string MudColorCode => "{w";

        public override string Name => "Light Gray";
    }
}