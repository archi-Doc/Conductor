// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Conductor.State;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace Conductor.Presentation;

public sealed partial class InformationPage : Page
{
    private const string LicenseUri = "https://opensource.org/licenses/MIT";

    public InformationPage()
    {
        this.InitializeComponent();
        this.State = App.GetService<InformationState>();

        var titleRun = new Run();
        titleRun.Text = App.Title;

        var copyrightRun = new Run();
        copyrightRun.Text = "  Copyright (c) 2024 archi-Doc\nReleased under the MIT license\n";

        var hyperlink = new Hyperlink();
        hyperlink.NavigateUri = new Uri(LicenseUri);
        hyperlink.Inlines.Add(new Run() { Text = LicenseUri, });
        hyperlink.Click += (s, e) =>
        {
            try
            {
                Arc.WinUI.Helper.OpenBrowser(hyperlink.NavigateUri.ToString());
            }
            catch
            {
            }
        };

        this.textBlock.Inlines.Add(titleRun);
        this.textBlock.Inlines.Add(copyrightRun);
        this.textBlock.Inlines.Add(hyperlink);

        // License
        this.AddLicense("License.CommunityToolkit", "Community Toolkit", true);
        this.AddLicense("License.WinUIEx", "WinUIEx");
        this.AddLicense("License.lz4net", "lz4net");
    }

    public InformationState State { get; }

    private void nvSample5_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var selectedItem = (NavigationViewItem)args.SelectedItem;
        this.ShowLicense((string)selectedItem.Tag);
    }

    private void ShowLicense(string key)
    {
        var license = HashedString.GetOrEmpty(key);
        if (!string.IsNullOrEmpty(license))
        {
            this.textBox.Text = license;
        }
    }

    private void AddLicense(string key, string title, bool isSelected = false)
    {
        var license = HashedString.GetOrEmpty(key);
        var item = new NavigationViewItem()
        {
            Content = title,
            Tag = key,
            FontSize = 14,
            IsSelected = isSelected,
        };

        this.navigationView.MenuItems.Add(item);
        if (isSelected)
        {
            this.ShowLicense(key);
        }
    }
}
