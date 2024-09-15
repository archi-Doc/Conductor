// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.WinUI;
using CommunityToolkit.WinUI;
using Conductor.State;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace Conductor.Presentation;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SimpleWindow : WinUIEx.WindowEx
{
    public SimpleWindow()
    {
        this.InitializeComponent();
        this.State = App.GetService<SimpleState>();
        this.Title = App.Title;
        this.SetApplicationIcon();
        // this.RemoveIcon();

        this.Activated += this.SimpleWindow_Activated;
        this.Closed += this.SimpleWindow_Closed;
        this.AppWindow.Closing += this.AppWindow_Closing;

        this.LoadWindowPlacement(App.Settings.WindowPlacement);
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true; // Since the Closing function isn't awaiting, I'll cancel first. Sorry for writing such crappy code.

        var result = await this.ShowMessageDialogAsync(0, Hashed.Dialog.Exit, Hashed.Dialog.Yes, Hashed.Dialog.No);
        if (result.TryGetSingleResult(out var r) && r == Hashed.Dialog.Yes)
        {
            App.Exit();
        }
    }

    #region FieldAndProperty

    public SimpleState State { get; }

    #endregion

    private void SimpleWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
    }

    private void SimpleWindow_Closed(object sender, WindowEventArgs args)
    {
        // Exit1
        App.Settings.WindowPlacement = this.SaveWindowPlacement();
    }

    private async void myButton_Click(object sender, RoutedEventArgs e)
    {
        // this.myButton.Content = "Clicked";

        Scaler.ViewScale *= 1.2;
        Scaler.Refresh();
    }

    private async void myButton_Click2(object sender, RoutedEventArgs e)
    {
        // this.myButton.Content = "Clicked2";

        Scaler.ViewScale *= 0.9;
        Scaler.Refresh();
    }
}
