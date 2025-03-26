// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Conductor.State;
using Microsoft.UI.Xaml.Controls;

namespace Conductor.PresentationState;

public sealed partial class SettingsPage : Page
{
    public SettingsPage(IApp app)
    {
        this.InitializeComponent();
        this.State = app.GetAndPrepareState<SettingsState>(this);

        // language: en, key: Language.En, text: English
        foreach (var x in LanguageList.LanguageToIdentifier)
        {
            this.AddLanguage(x.Key, x.Value);
        }

        double[] scaling = [0.50d, 0.60d, 0.67d, 0.75d, 0.80d, 0.90d, 1.00d, 1.10d, 1.25d, 1.50d, 1.75d, 2.00d,];
        foreach (var x in scaling)
        {
            this.AddScaling(x);
        }
    }

    public SettingsState State { get; }

    private void AddLanguage(string language, string key)
    {
        if (!HashedString.TryGet(HashedString.IdentifierToHash(key), out var text))
        {
            return;
        }

        var item = new MenuFlyoutItem
        {
            // DataContext = this.ViewModel,
            Text = text, // $"{{Arc:Stringer Source=Settings.Language}}",
            Tag = language,
            Command = this.State.SelectLanguageCommand,
            CommandParameter = language,
        };

        Stringer.Register(item, MenuFlyoutItem.TextProperty, key);
        this.menuLanguage.Items.Add(item);
    }

    private void AddScaling(double scale)
    {
        var text = Scaler.ScaleToText(scale);
        var item = new MenuFlyoutItem
        {
            Text = text,
            // Tag = scale.ToString(),
            Command = this.State.SelectScalingCommand,
            CommandParameter = scale,
        };

        this.menuScaling.Items.Add(item);
    }
}
