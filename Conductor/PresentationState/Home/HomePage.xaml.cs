// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Conductor.State;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Conductor.Presentation;

public sealed partial class HomePage : Page
{
    public HomePageState State { get; }

    public HomePage()
    {
        this.InitializeComponent();
        this.State = App.GetService<HomePageState>();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
    }
}
