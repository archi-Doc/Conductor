// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Application;
using Arc.WinAPI;
using Serilog;

#pragma warning disable SA1649 // File name should match first type name

namespace Arc.WPF
{
    public class Transformer
    {
        private const double MinimumScale = 0.25; // Minimum value of scale.
        private readonly double marginX = SystemParameters.ResizeFrameVerticalBorderWidth * 2;
        private readonly double marginY = (SystemParameters.ResizeFrameHorizontalBorderHeight * 2) + SystemParameters.CaptionHeight;
        private LinkedList<TransformerObject> objectList = new LinkedList<TransformerObject>();

        public Transformer()
        {
        }

        public static Transformer Instance { get; } = new Transformer();

        public double ScaleX { get; set; } = 1.0;

        public double ScaleY { get; set; } = 1.0;

        public bool Register(Window window, bool resizeWindow = false, bool independentScale = false)
        {
            lock (this.objectList)
            {
                if (this.SearchTransformObject(window) != null)
                {// Already registered.
                    return false;
                }

                var obj = new TransformerObject(window, resizeWindow, independentScale);
                this.objectList.AddLast(obj);

                if (!obj.IndependentScale)
                {
                    obj.CurrentScaleX = this.ScaleX;
                    obj.CurrentScaleY = this.ScaleY;
                }

                obj.TransformUpdated = true;
                obj.InitialPacket = new TransformerPacket(window, obj);

                window.SizeChanged += (sender, args) =>
                {
                    if (this.CheckAndResetTransformFlag((Window)sender, out var packet))
                    {
                        this.AdjustScale(packet, true);
                    }
                };
            }

            return true;
        }

        public void Cleanup()
        {
            lock (this.objectList)
            {
                var node = this.objectList.First;
                while (node != null)
                {
                    var next = node.Next;

                    if (!node.Value.Window.TryGetTarget(out var _))
                    {// Remove from list.
                        this.RemoveTransformObject(node);
                    }

                    node = next;
                }
            }
        }

        public void Transform(double scaleX, double scaleY)
        {
            if (this.ScaleX == scaleX && this.ScaleY == scaleY)
            { // Same scale ratio. No change.
                return;
            }

            var packetList = new List<TransformerPacket>();

            lock (this.objectList)
            {
                var node = this.objectList.First;
                while (node != null)
                {
                    var next = node.Next;

                    if (node.Value.Window.TryGetTarget(out var w))
                    {// Add to list.
                        if (!node.Value.IndependentScale)
                        {
                            node.Value.CurrentScaleX = scaleX;
                            node.Value.CurrentScaleY = scaleY;

                            node.Value.TransformUpdated = true;
                            var packet = new TransformerPacket(w, node.Value);
                            if (node.Value.InitialPacket == null)
                            {
                                packetList.Add(packet);
                            }
                            else
                            {
                                node.Value.InitialPacket = packet;
                            }
                        }
                    }
                    else
                    {// Remove from list.
                        this.RemoveTransformObject(node);
                    }

                    node = next;
                }

                this.ScaleX = scaleX;
                this.ScaleY = scaleY;
            }

            if (packetList.Count > 0)
            {
                this.ProcessPacket(packetList);
            }
        }

        public void Transform(Window window, double scaleX, double scaleY)
        {
            TransformerPacket? packet = null;

            lock (this.objectList)
            { // Get TransformerObject from objectList.
                var obj = this.SearchTransformObject(window);
                if (obj == null)
                {
                    return;
                }

                if (obj.IndependentScale)
                {
                    if (obj.CurrentScaleX != scaleX || obj.CurrentScaleY != scaleY)
                    {
                        obj.CurrentScaleX = scaleX;
                        obj.CurrentScaleY = scaleY;

                        obj.TransformUpdated = true;
                        if (obj.InitialPacket == null)
                        {
                            packet = new TransformerPacket(window, obj);
                        }
                        else
                        {
                            obj.InitialPacket = new TransformerPacket(window, obj);
                        }
                    }
                }
            }

            if (packet == null)
            {
                return;
            }

            this.ProcessPacket(packet);
        }

        public void AdjustWindowPosition(Window window)
        {
            App.InvokeAsyncOnUI(() =>
            { // UI thread.
                this.AdjustWindowPosition(window, null);
            });
        }

        public void AdjustScale(Window window, bool adjustPosition)
        {
            TransformerPacket? packet = null;

            lock (this.objectList)
            { // Get TransformerObject from objectList.
                var obj = this.SearchTransformObject(window);
                if (obj != null)
                { // Found.
                    packet = new TransformerPacket(window, obj);
                }
            }

            if (packet != null)
            {
                App.InvokeAsyncOnUI(() =>
                { // UI thread.
                    this.AdjustScale(packet, adjustPosition);
                });
            }
        }

        private TransformerObject? SearchTransformObject(Window window)
        { // lock (this.objectList) required.
            foreach (var x in this.objectList)
            {
                if (x.Window.TryGetTarget(out var w))
                {
                    if (w == window)
                    { // Found.
                        return x;
                    }
                }
            }

            return null;
        }

        private void RemoveTransformObject(LinkedListNode<TransformerObject> node)
        { // lock (this.objectList) required.
            this.objectList.Remove(node);
            Log.Information("transformer object removed.");
        }

        private void AdjustScale(TransformerPacket packet, bool adjustPosition)
        { // UI thread
            var element = packet.Window.FirstLogicalFrameworkElement();
            if (element == null)
            {
                return;
            }

            var trans = element.LayoutTransform as ScaleTransform;
            if (trans == null)
            {
                return;
            }

            if (!this.GetMaxScale(packet, element, out var workarea, out var maxScaleX, out var maxScaleY))
            {
                return;
            }

            if (packet.Window.SizeToContent != SizeToContent.Manual)
            { // Limit ScaleTransform.
                double scaleX = trans.ScaleX;
                double scaleY = trans.ScaleY;
                var ratio = Math.Max(scaleX / maxScaleX, scaleY / maxScaleY);
                if (ratio > 1)
                {
                    scaleX /= ratio;
                    scaleY /= ratio;
                    element.LayoutTransform = new ScaleTransform(scaleX, scaleY);
                }
            }

            // Adjust window position.
            if (adjustPosition)
            {
                this.AdjustWindowPosition(packet.Window, workarea);
            }

            return;
        }

        private void AdjustWindowPosition(Window window, DipRect? workarea)
        {
            if (window.WindowState != WindowState.Normal)
            {// Maximized or minimized.
                return;
            }

            if (workarea == null)
            {
                if (!this.GetMonitorWorkareaAndDpi(window, out var workrect, out var dpiX, out var dpiY))
                {
                    return;
                }

                workarea = new DipRect(workrect, dpiX, dpiY);
            }

            var actualWidth = window.ActualWidth; // + this.marginX;
            var actualHeight = window.ActualHeight; // + this.marginY;

            var bottom = window.Top + actualHeight;
            if (bottom > workarea.Bottom)
            {
                window.Top = workarea.Bottom - actualHeight;
            }

            var right = window.Left + actualWidth;
            if (right > workarea.Right)
            {
                window.Left = workarea.Right - actualWidth;
            }

            if (window.Top < workarea.Top)
            {
                window.Top = workarea.Top;
            }

            if (window.Left < workarea.Left)
            {
                window.Left = workarea.Left;
            }

            if (window.ActualWidth > workarea.Width)
            {
                window.Width = workarea.Width;
            }

            if (window.ActualHeight > workarea.Height)
            {
                window.Height = workarea.Height;
            }
        }

        private void InitialProcess(Window window, TransformerObject obj)
        {
            if (obj.InitialPacket != null)
            {
                // obj.InitialDesiredWidth = window.DesiredSize.Width;
                // obj.InitialDesiredHeight = window.DesiredSize.Height;
                var element = window.FirstLogicalFrameworkElement();
                if (element != null)
                {
                    obj.InitialDesiredWidth = element.DesiredSize.Width;
                    obj.InitialDesiredHeight = element.DesiredSize.Height;
                }

                this.ProcessPacket_Do(obj.InitialPacket);

                obj.InitialPacket = null;
            }
        }

        private bool CheckAndResetTransformFlag(Window window, [NotNullWhen(true)] out TransformerPacket? packet)
        {
            packet = default;

            lock (this.objectList)
            {
                var node = this.objectList.First;
                while (node != null)
                {
                    var next = node.Next;

                    if (node.Value.Window.TryGetTarget(out var w))
                    {// Add to list.
                        if (w == window)
                        {
                            if (node.Value.TransformUpdated)
                            { // Update flag is set.
                                this.InitialProcess(w, node.Value);
                                packet = new TransformerPacket(w, node.Value);
                                node.Value.TransformUpdated = false;
                                return true;
                            }
                            else
                            { // Not set. Skip other windows.
                                return false;
                            }
                        }
                    }
                    else
                    {// Remove from list.
                        this.RemoveTransformObject(node);
                    }

                    node = next;
                }
            }

            return false;
        }

        private void ProcessPacket(TransformerPacket? packet)
        {
            if (packet == null)
            {
                return;
            }

            App.InvokeAsyncOnUI(() =>
            { // UI thread.
                this.ProcessPacket_Do(packet);
            });
        }

        private void ProcessPacket(List<TransformerPacket> packetList)
        {
            App.InvokeAsyncOnUI(() =>
            { // UI thread.
                foreach (var x in packetList)
                {
                    this.ProcessPacket_Do(x);
                }
            });
        }

        private bool GetMonitorWorkareaAndDpi(Window window, out RECT workarea, out uint dpiX, out uint dpiY)
        {
            workarea = default;
            dpiX = 0;
            dpiY = 0;

            try
            {
                var hwnd = PresentationSource.FromVisual(window) as HwndSource;
                if (hwnd == null)
                {
                    return false;
                }

                var hmonitor = Arc.WinAPI.Methods.MonitorFromWindow(hwnd.Handle, MonitorDefaultTo.MONITOR_DEFAULTTONEAREST);
                Arc.WinAPI.Methods.GetDpiForMonitor(hmonitor, MonitorDpiType.Default, ref dpiX, ref dpiY);
                var monitorInfo = new MONITORINFOEX();
                Arc.WinAPI.Methods.GetMonitorInfo(hmonitor, monitorInfo);
                workarea = monitorInfo.rcWork;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool GetScale(FrameworkElement element, out double scaleX, out double scaleY)
        {
            if (element.LayoutTransform is ScaleTransform trans)
            {
                scaleX = trans.ScaleX;
                scaleY = trans.ScaleY;
                return true;
            }
            else
            {
                scaleX = 1;
                scaleY = 1;
                return false;
            }
        }

        private bool GetMaxScale(TransformerPacket packet, FrameworkElement element, [NotNullWhen(true)] out DipRect? workarea, out double scaleX, out double scaleY)
        {
            workarea = default;
            scaleX = 1;
            scaleY = 1;

            try
            {
                if (this.GetMonitorWorkareaAndDpi(packet.Window, out var workrect, out var dpiX, out var dpiY))
                {
                    workarea = new DipRect(workrect, dpiX, dpiY);
                    double remainingWidth = workarea.Width - this.marginX;
                    double remainingHeight = workarea.Height - this.marginY;

                    scaleX = remainingWidth / packet.InitialDesiredWidth; // element.DesiredSize.Width element.ActualWidth;
                    if (scaleX < MinimumScale)
                    {
                        scaleX = MinimumScale;
                    }

                    scaleY = remainingHeight / packet.InitialDesiredHeight; // / element.DesiredSize.Height;
                    if (scaleY < MinimumScale)
                    {
                        scaleY = MinimumScale;
                    }

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void ProcessPacket_Do(TransformerPacket packet)
        {// UI thread.
            var element = packet.Window.FirstLogicalFrameworkElement();
            if (element == null)
            {
                return;
            }

            double ratio = 0; // Initialize ratio.
            if (packet.ResizeWindow && packet.Window.SizeToContent == SizeToContent.Manual)
            {
                if (element.LayoutTransform is ScaleTransform trans)
                {
                    try
                    {
                        ratio = Math.Max(packet.ScaleX / trans.ScaleX, packet.ScaleY / trans.ScaleY);
                    }
                    catch
                    {
                    }
                }
            }

            element.LayoutTransform = new ScaleTransform(packet.ScaleX, packet.ScaleX);

            if (ratio > 0)
            {
                if (packet.Window.WindowState == WindowState.Normal)
                {// Resize window.
                    packet.Window.Width *= ratio;
                    packet.Window.Height *= ratio;
                    this.AdjustWindowPosition(packet.Window, null);
                }
                else
                {
                    /* var rect = packet.Window.RestoreBounds;
                    if (!rect.IsEmpty)
                    {
                        rect.Width *= ratio;
                        rect.Height *= ratio;
                    }*/
                }
            }
        }

        private class TransformerPacket
        {
            public TransformerPacket(Window window, TransformerObject obj)
            {
                this.Window = window;
                this.ScaleX = obj.CurrentScaleX;
                this.ScaleY = obj.CurrentScaleY;
                this.ResizeWindow = obj.ResizeWindow;
                this.InitialDesiredWidth = obj.InitialDesiredWidth;
                this.InitialDesiredHeight = obj.InitialDesiredHeight;
            }

            public Window Window { get; } // Window object.

            public double ScaleX { get; } // Scale.

            public double ScaleY { get; } // Scale.

            public bool ResizeWindow { get; } // Resize window.

            public double InitialDesiredWidth { get; } // Initial desired width.

            public double InitialDesiredHeight { get; } // Initial desired Height.
        }

        private class TransformerObject
        {
            public TransformerObject(Window window, bool resizeWindow, bool independentScale)
            {
                this.Window = new WeakReference<Window>(window);
                this.ResizeWindow = resizeWindow;
                this.IndependentScale = independentScale;

                this.CurrentScaleX = 1.0;
                this.CurrentScaleY = 1.0;
                this.TransformUpdated = false;
            }

            public WeakReference<Window> Window { get; } // Window object.

            public TransformerPacket? InitialPacket { get; set; } // Initial Packet.

            public double InitialDesiredWidth { get; set; } // Initial desired width.

            public double InitialDesiredHeight { get; set; } // Initial desired Height.

            public bool ResizeWindow { get; } // Resize window automatically.

            public bool IndependentScale { get; } // Window has its own scale value.

            public double CurrentScaleX { get; set; } // Current Scale.

            public double CurrentScaleY { get; set; } // Current Scale.

            public bool TransformUpdated { get; set; } // New Transform is set.
        }
    }
}
