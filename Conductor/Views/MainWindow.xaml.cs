// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Application;
using Arc.CrossChannel;
using Arc.Mvvm;
using Arc.WinAPI;
using Arc.WPF;
using Conductor.ViewModels;

#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Conductor.ViewServices
{
    public enum TaskbarCommandId
    {
        /// <summary>
        /// Toggles prevention of PC sleep.
        /// </summary>
        TogglePreventSleep,

        /// <summary>
        /// Toggles prevention of screen turning off.
        /// </summary>
        TogglePreventScreenOff,
    }

    public enum Message_Save
    { // CrossChannel: Save application data.
        Prepare, // Prepare for data storage.
        Save, // Save data.
    }

    public interface IMainViewService
    {
        void Notification(NotificationMessage msg); // Notification Message

        void MessageID(MessageId id); // Message Id

        Task<MessageBoxResult> Dialog(DialogParam p); // Dialog
    }
}

namespace Conductor.Views
{
    using Conductor.ViewServices;
    using Serilog;

    /// <summary>
    /// Main Window.
    /// </summary>
    public partial class MainWindow : Window, IMainViewService
    {
        private MainViewModel vm; // ViewModel
        private Window? windowClosing = null; // Avoid an exception which occurs when Close () is called while the Window Close confirmation dialog is displayed.

        public MainWindow(MainViewModel vm)
        {
            App.Waypoint();
            this.InitializeComponent();
            this.DataContext = vm;
            this.vm = vm;

            App.Waypoint();
            try
            {
                ToolTipService.InitialShowDelayProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(1000));
            }
            catch
            {
            }

#if !DEBUG
            this.textDebug.Visibility = Visibility.Hidden;
#endif

            App.Waypoint();
            CrossChannel.Open<Message_Save>(this.Window_Save);
            Transformer.Instance.Register(this, true, false);
            App.Waypoint();

            this.Title = App.Title;
        }

        public void Notification(NotificationMessage msg)
        { // Multi-thread safe, may be called from non-UI thread/context. Use App.UI.InvokeAsync().
            App.InvokeAsyncOnUI(() =>
            {
                var result = MessageBox.Show(msg.Notification);
            });
        }

        public void MessageID(MessageId id)
        { // Multi-thread safe, may be called from non-UI thread/context. Use App.UI.InvokeAsync().
            App.InvokeAsyncOnUI(() =>
            { // UI thread.
                if (id == MessageId.SwitchCulture)
                { // Switch culture.
                    if (App.Settings.Culture == "ja")
                    {
                        App.Settings.Culture = "en";
                    }
                    else
                    {
                        App.Settings.Culture = "ja";
                    }

                    App.C4.ChangeCulture(App.Settings.Culture);
                    Arc.WPF.C4Updater.C4Update();
                }
                else if (id == MessageId.Exit)
                { // Exit application with confirmation.
                    if (this.windowClosing == null)
                    {
                        this.Close();
                    }
                }
                else if (id == MessageId.ExitWithoutConfirmation)
                { // Exit application without confirmation.
                    App.SessionEnding = true;
                    if (this.windowClosing == null)
                    {
                        this.Close();
                    }
                    else
                    {
                        this.windowClosing.Close();
                    }
                }
                else if (id == MessageId.Information)
                {
                    var mit_license = "https://opensource.org/licenses/MIT";
                    var dlg = new Arc.WPF.Dialog(this);
                    dlg.TextBlock.Inlines.Add(
    @"Copyright (c) 2020 archi-Doc
Released under the MIT license
");
                    var h = new Hyperlink() { NavigateUri = new Uri(mit_license) };
                    h.Inlines.Add(mit_license);
                    h.RequestNavigate += (s, e) =>
                    {
                        try
                        {
                            App.OpenBrowser(e.Uri.ToString());
                        }
                        catch
                        {
                        }
                    };
                    dlg.TextBlock.Inlines.Add(h);
                    dlg.ShowDialog();
                }
                else if (id == MessageId.Settings)
                {
                    var dialog = new SettingsWindow(this);
                    dialog.ShowDialog();
                }
                else if (id == MessageId.DisplayScaling)
                {
                    Transformer.Instance.Transform(App.Settings.DisplayScaling, App.Settings.DisplayScaling);
                }
                else if (id == MessageId.ActivateWindow)
                {
                    Arc.WinAPI.Methods.ActivateWindow(new System.Windows.Interop.WindowInteropHelper(this).Handle); // this.Show();
                }
                else if (id == MessageId.ActivateWindowForce)
                {
                    Arc.WinAPI.Methods.ActivateWindowForce(new System.Windows.Interop.WindowInteropHelper(this).Handle); // this.Show();
                }
            });
        }

        public async Task<MessageBoxResult> Dialog(DialogParam p)
        { // Multi-thread safe, may be called from non-UI thread/context. Use App.UI.InvokeAsync()
            var dlg = new Arc.WPF.Dialog(this, p);
            var result = await dlg.ShowDialogAsync();
            return result;
            /*var tcs = new TaskCompletionSource<MessageBoxResult>();
            await this.Dispatcher.InvokeAsync(() => { dlg.ShowDialog(); tcs.SetResult(dlg.Result); }); // Avoid dead lock.
            return tcs.Task.Result;*/
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            App.Waypoint();
            if (!App.Settings.LoadError)
            { // Change the UI before this code. The window will be displayed shortly.
                IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                Arc.WinAPI.Methods.GetMonitorDpi(hwnd, out var dpiX, out var dpiY);
                WINDOWPLACEMENT wp = App.Settings.WindowPlacement.ToWINDOWPLACEMENT2(dpiX, dpiY);
                wp.length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                wp.flags = 0;
                wp.showCmd = wp.showCmd == SW.SHOWMINIMIZED ? SW.SHOWNORMAL : wp.showCmd;
                Arc.WinAPI.Methods.SetWindowPlacement(hwnd, ref wp);
                Transformer.Instance.AdjustWindowPosition(this);
            }

            App.Waypoint();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Log.Information("Window closing.");

            if (App.Settings.CloseButtonToTaskbar)
            {
                if (App.SessionEnding == false)
                {
                    e.Cancel = true; // cancel
                    this.Hide();
                    return;

                    /* var dlg = new Arc.WPF.Dialog(this);
                    dlg.Message = C4.Instance["dialog.exit"];
                    dlg.Button = MessageBoxButton.YesNo; // button
                    dlg.Result = MessageBoxResult.Yes; // focus
                    dlg.Image = MessageBoxImage.Warning;
                    this.windowClosing = dlg;
                    dlg.ShowDialog();
                    this.windowClosing = null;
                    if (dlg.Result == MessageBoxResult.No)
                    {
                        e.Cancel = true; // cancel
                        return;
                    } */
                }
            }

            this.Window_Save(Message_Save.Prepare);
        }

        private void Window_Save(Message_Save message)
        {
            if (message == Message_Save.Prepare)
            {
                App.InvokeAsyncOnUI(() =>
                {
                    // Exit1 (Window is still visible)
                    IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    Arc.WinAPI.Methods.GetWindowPlacement(hwnd, out var wp);
                    Arc.WinAPI.Methods.GetMonitorDpi(hwnd, out var dpiX, out var dpiY);
                    App.Settings.WindowPlacement.FromWINDOWPLACEMENT2(wp, dpiX, dpiY);
                });
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (App.Settings.CloseButtonToTaskbar)
                {
                    this.Hide();
                }
                else
                {
                    this.Close();
                }
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Minimized:
                    this.Hide();
                    break;
            }
        }
    }
}
