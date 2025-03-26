// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Conductor.State;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Conductor.PresentationState;

public sealed partial class HomePage : Page
{
    public HomePageState State { get; }

    public HomePage(IApp app)
    {
        this.InitializeComponent();
        this.State = app.GetAndPrepareState<HomePageState>(this);
        this.DataContext = this.State; // Set the DataContext when using Binding.
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        this.State.UpdateStatus(true);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
    }
}
