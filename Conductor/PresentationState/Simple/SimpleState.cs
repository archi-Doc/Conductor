// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.WinUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Conductor.State;

public partial class SimpleState : ObservableObject
{
    public SimpleState(AppSettings appSettings)
    {
        this.appSettings = appSettings;
    }

    private readonly AppSettings appSettings;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextUpper))]
    public partial string Text { get; set; } = string.Empty;

    public string TextUpper => this.Text.ToUpper();

    [RelayCommand]
    private void OpenDataDirectory()
    {
        try
        {
            System.Diagnostics.Process.Start("Explorer.exe", App.DataFolder);
        }
        catch
        {
        }
    }

    [RelayCommand]
    private void SwitchLanguage()
    {
        if (this.appSettings.Culture == "ja")
        {
            this.appSettings.Culture = "en";
        }
        else
        {
            this.appSettings.Culture = "ja";
        }

        HashedString.ChangeCulture(this.appSettings.Culture);
        Arc.WinUI.Stringer.Refresh();
    }
}
