// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.WinUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Conductor.State;

public partial class SettingsState : ObservableObject
{
    public SettingsState()
    {
        this.SetLanguageText();
        this.SetScalingText();
    }

    private void SetLanguageText()
    {
        if (LanguageList.LanguageToIdentifier.TryGetValue(App.Settings.Culture, out var identifier))
        {
            this.LanguageText = HashedString.GetOrEmpty(identifier);
        }
    }

    private void SetScalingText()
    {
        this.ScalingText = Scaler.ScaleToText(Scaler.ViewScale);
    }

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

        /*if (App.Settings.Culture == "ja")
        {
            App.Settings.Culture = "en";
        }
        else
        {
            App.Settings.Culture = "ja";
        }

        HashedString.ChangeCulture(App.Settings.Culture);
        Arc.WinUI.Stringer.Refresh();*/

        // this.GetPresentationService<IMessageDialog>().Show(Hashed.App.Name, Hashed.App.Description);
    }

    [RelayCommand]
    private void SelectLanguage(string language)
    {
        if (App.Settings.Culture == language)
        {
            return;
        }

        App.Settings.Culture = language;
        HashedString.ChangeCulture(App.Settings.Culture);
        Arc.WinUI.Stringer.Refresh();
        this.SetLanguageText();
    }

    [RelayCommand]
    private void SelectScaling(double scaling)
    {
        if (Scaler.ViewScale == scaling)
        {
            return;
        }

        Scaler.ViewScale = scaling;
        Scaler.Refresh();
        this.SetScalingText();
    }

    [ObservableProperty]
    private string languageText = string.Empty;

    [ObservableProperty]
    private string scalingText = string.Empty;
}
