// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.UI.Xaml;

namespace StandardWinUI;

public partial class StandardApp : Application
{
    public StandardApp(IApp app)
    {
        this.app = app;
        this.InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        this.window = this.app.GetMainWindow();
        this.window.Activate();
    }

    private readonly IApp app;
    private Window? window;
}
