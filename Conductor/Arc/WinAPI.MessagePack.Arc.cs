// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using MessagePack;

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1649 // File name should match first type name

namespace Arc.WinAPI
{
    [MessagePackObject]
    public class DipWindowPlacement
    { // Device Independent, 1/96 inch
        [Key(0)]
        public int Length { get; set; }

        [Key(1)]
        public int Flags { get; set; }

        [Key(2)]
        public SW ShowCmd { get; set; }

        [Key(3)]
        public DipPoint MinPosition { get; set; } = new DipPoint();

        [Key(4)]
        public DipPoint MaxPosition { get; set; } = new DipPoint();

        [Key(5)]
        public DipRect NormalPosition { get; set; } = new DipRect();

        public DipWindowPlacement()
        {
        }

        public void FromWINDOWPLACEMENT(WINDOWPLACEMENT wp, double dpiX, double dpiY)
        {
            this.Length = wp.length;
            this.Flags = wp.flags;
            this.ShowCmd = wp.showCmd;
            this.MinPosition.FromPOINT(wp.minPosition, dpiX, dpiY);
            this.MaxPosition.FromPOINT(wp.maxPosition, dpiX, dpiY);
            this.NormalPosition.FromRECT(wp.normalPosition, dpiX, dpiY);
        }

        public WINDOWPLACEMENT ToWINDOWPLACEMENT(double dpiX, double dpiY)
        {
            return new WINDOWPLACEMENT
            {
                length = this.Length,
                flags = this.Flags,
                showCmd = this.ShowCmd,
                minPosition = this.MinPosition.ToPOINT(dpiX, dpiY),
                maxPosition = this.MaxPosition.ToPOINT(dpiX, dpiY),
                normalPosition = this.NormalPosition.ToRECT(dpiX, dpiY),
            };
        }

        public void FromWINDOWPLACEMENT2(WINDOWPLACEMENT wp, double dpiX, double dpiY)
        {
            this.Length = wp.length;
            this.Flags = wp.flags;
            this.ShowCmd = wp.showCmd;
            this.MinPosition.FromPOINT2(wp.minPosition);
            this.MaxPosition.FromPOINT2(wp.maxPosition);
            this.NormalPosition.FromRECT2(wp.normalPosition, dpiX, dpiY);
        }

        public WINDOWPLACEMENT ToWINDOWPLACEMENT2(double dpiX, double dpiY)
        {
            return new WINDOWPLACEMENT
            {
                length = this.Length,
                flags = this.Flags,
                showCmd = this.ShowCmd,
                minPosition = this.MinPosition.ToPOINT2(),
                maxPosition = this.MaxPosition.ToPOINT2(),
                normalPosition = this.NormalPosition.ToRECT2(dpiX, dpiY),
            };
        }
    }

    [MessagePackObject]
    public class DipPoint
    { // Device Independent, 1/96 inch
        [Key(0)]
        public double X { get; set; }

        [Key(1)]
        public double Y { get; set; }

        public DipPoint(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public DipPoint(POINT point, double dpiX, double dpiY)
        {
            this.FromPOINT(point, dpiX, dpiY);
        }

        public DipPoint()
        {
        }

        public void FromPOINT(POINT point, double dpiX, double dpiY)
        {
            this.X = point.X * 96 / dpiX;
            this.Y = point.Y * 96 / dpiY;
        }

        public POINT ToPOINT(double dpiX, double dpiY)
        {
            return new POINT((int)(this.X * dpiX / 96), (int)(this.Y * dpiY / 96));
        }

        public void FromPOINT2(POINT point)
        {
            this.X = point.X;
            this.Y = point.Y;
        }

        public POINT ToPOINT2()
        {
            return new POINT((int)this.X, (int)this.Y);
        }
    }

    [MessagePackObject]
    public class DipRect
    {
        [Key(0)]
        public double Left { get; set; }

        [Key(1)]
        public double Top { get; set; }

        [Key(2)]
        public double Right { get; set; }

        [Key(3)]
        public double Bottom { get; set; }

        [IgnoreMember]
        public double Width => this.Right - this.Left;

        [IgnoreMember]
        public double Height => this.Bottom - this.Top;

        public DipRect(double left, double top, double right, double bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }

        public DipRect(RECT rect, double dpiX, double dpiY)
        {
            this.FromRECT(rect, dpiX, dpiY);
        }

        public DipRect()
        {
        }

        public void FromRECT(RECT rect, double dpiX, double dpiY)
        {
            this.Left = rect.Left * 96 / dpiX;
            this.Top = rect.Top * 96 / dpiY;
            this.Right = rect.Right * 96 / dpiX;
            this.Bottom = rect.Bottom * 96 / dpiY;
        }

        public RECT ToRECT(double dpiX, double dpiY)
        {
            return new RECT((int)(this.Left * dpiX / 96), (int)(this.Top * dpiY / 96), (int)(this.Right * dpiX / 96), (int)(this.Bottom * dpiY / 96));
        }

        public void FromRECT2(RECT rect, double dpiX, double dpiY)
        {
            this.Left = rect.Left;
            this.Top = rect.Top;
            this.Right = rect.Left + ((rect.Right - rect.Left) * 96 / dpiX);
            this.Bottom = rect.Top + ((rect.Bottom - rect.Top) * 96 / dpiY);
        }

        public RECT ToRECT2(double dpiX, double dpiY)
        {
            return new RECT((int)this.Left, (int)this.Top, (int)(this.Left + (this.Width * dpiX / 96)), (int)(this.Top + (this.Height * dpiY / 96)));
        }
    }

    /// <summary>
    /// Arc.WinAPI Methods.
    /// </summary>
    public partial class Methods
    {
        [DllImport("user32.dll")]
        internal static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);
    }

    [MessagePackObject]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        [Key(0)]
        public int length;
        [Key(1)]
        public int flags;
        [Key(2)]
        public SW showCmd;
        [Key(3)]
        public POINT minPosition;
        [Key(4)]
        public POINT maxPosition;
        [Key(5)]
        public RECT normalPosition;
    }

    [MessagePackObject]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        [Key(0)]
        public int X;
        [Key(1)]
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    [MessagePackObject]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        [Key(0)]
        public int Left;
        [Key(1)]
        public int Top;
        [Key(2)]
        public int Right;
        [Key(3)]
        public int Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }
    }

    public enum SW
    {
        HIDE = 0,
        SHOWNORMAL = 1,
        SHOWMINIMIZED = 2,
        SHOWMAXIMIZED = 3,
        SHOWNOACTIVATE = 4,
        SHOW = 5,
        MINIMIZE = 6,
        SHOWMINNOACTIVE = 7,
        SHOWNA = 8,
        RESTORE = 9,
        SHOWDEFAULT = 10,
    }
}
