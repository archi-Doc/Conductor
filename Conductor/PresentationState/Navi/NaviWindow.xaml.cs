// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Arc.WinUI;
using CommunityToolkit.WinUI;
using CrossChannel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

namespace Conductor.Presentation;

public partial class NaviWindow : WindowEx, IBasicPresentationService, IConductorPresentationService
{
    public NaviWindow(IChannel<IBasicPresentationService> basicPresentationChannel, IChannel<IConductorPresentationService> conductorPresentationChannel)
    {
        this.InitializeComponent();
        Scaler.Register(this.layoutTransform);
        basicPresentationChannel.Open(this, true);
        conductorPresentationChannel.Open(this, true);

        this.Title = App.Title;
        this.SetApplicationIcon();
        // this.RemoveIcon();

        this.Activated += this.NaviWindow_Activated;
        this.Closed += this.NaviWindow_Closed;
        this.AppWindow.Closing += this.AppWindow_Closing;

        this.LoadWindowPlacement(App.Settings.WindowPlacement);
        this.nvHome.IsSelected = true;
    }

    #region FieldAndProperty

    #endregion

    #region IBasicPresentationService

    Task<RadioResult<ContentDialogResult>> IBasicPresentationService.MessageDialog(string title, string content, string primaryCommand, string? cancelCommand, string? secondaryCommand, CancellationToken cancellationToken)
        => App.UiDispatcherQueue.EnqueueAsync(() => this.ShowMessageDialogAsync(title, content, primaryCommand, cancelCommand, secondaryCommand, cancellationToken));

    public Task<RadioResult<bool>> TryExit(CancellationToken cancellationToken = default)
    {
        return App.UiDispatcherQueue.EnqueueAsync<RadioResult<bool>>(async () =>
        {
            var result = await this.ShowMessageDialogAsync(0, Hashed.Dialog.Exit, Hashed.Dialog.Yes, Hashed.Dialog.No, 0, cancellationToken);
            if (result.TryGetSingleResult(out var r) && r == ContentDialogResult.Primary)
            {// Exit
                App.Exit();
                return new(true);
            }
            else
            {// Canceled
                return new(false);
            }
        });
    }

    #endregion

    #region IConductorPresentationService

    public void ActivateWindow(bool force = false)
    {
        Arc.WinUI.WindowExtensions.ActivateWindow(this, force);
    }

    void IConductorPresentationService.ActivateWindow(bool force)
    {
        this.ActivateWindow(force);
    }

    #endregion

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {// The close button of the Window was pressed.
        // args.Cancel = true; // Since the Closing function isn't awaiting, I'll cancel first. Sorry for writing such crappy code.
        // await this.TryExit();

        App.Exit();
    }

    private void NaviWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
    }

    private void NaviWindow_Closed(object sender, WindowEventArgs args)
    {
        // Exit1
        App.Settings.WindowPlacement = this.SaveWindowPlacement();
    }

    private async void nvSample_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var selectedItem = (NavigationViewItem)args.SelectedItem;
        switch (selectedItem.Tag)
        {
            case "Home":
                this.contentFrame.Navigate(typeof(HomePage));
                break;
            case "Settings":
                this.contentFrame.Navigate(typeof(SettingsPage));
                break;
            case "Information":
                this.contentFrame.Navigate(typeof(InformationPage));
                break;

            default:
                break;
        }
    }

    private async void nvExit_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        await this.TryExit();
    }
}
