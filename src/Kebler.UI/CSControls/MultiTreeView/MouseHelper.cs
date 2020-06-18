﻿using System.Runtime.InteropServices;
using System.Windows;

namespace Kebler.UI.CSControls.MuliTreeView
{
    internal static class MouseHelper
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref MouseHelper.Win32Point pt);

        public static Point GetMousePosition()
        {
            MouseHelper.Win32Point pt = new MouseHelper.Win32Point();
            GetCursorPos(ref pt);
            return new Point(pt.X, pt.Y);
        }

        private struct Win32Point
        {
            public int X;
            public int Y;
        }
    }
}
