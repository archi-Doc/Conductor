// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Application;
using Arc.Mvvm;
using Arc.WPF;
using Conductor.ViewServices;

#pragma warning disable SA1201 // Elements should appear in the correct order

namespace Conductor.Views
{
    /// <summary>
    /// SettingsWindow.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public string CurrentCulture { get; set; }

        public List<string> CultureList { get; private set; } = new List<string>() { "en", "ja" };

        public double CurrentDisplayScaling { get; set; }

        public List<double> DisplayScaling { get; private set; } = new List<double>() { 0.25, 0.333, 0.5, 0.667, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 5 };

        public bool CloseButtonToTaskbar { get; set; }

        private IMainViewService ViewService => App.Resolve<IMainViewService>(); // To avoid a circular dependency, get an instance when necessary.

        private DelegateCommand<string>? licenseTextCommand;

        public DelegateCommand<string> LicenseTextCommand
        {
            get
            {
                return (this.licenseTextCommand != null) ? this.licenseTextCommand : this.licenseTextCommand = new DelegateCommand<string>(
                    (name) =>
                    {// execute
                        var lic = App.C4.Get(name);
                        if (lic != null)
                        {
                            this.information_license.Text = lic;
                        }

                        return;
                    });
            }
        }

        public SettingsWindow(Window owner)
        {
            this.InitializeComponent();

            // Settings
            this.FontSize = owner.FontSize;
            this.Owner = owner;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.ShowInTaskbar = false;
            this.DataContext = this;

            Transformer.Instance.Register(this);

            // Load
            this.CurrentCulture = App.Settings.Culture;
            this.CurrentDisplayScaling = App.Settings.DisplayScaling;
            this.CloseButtonToTaskbar = App.Settings.CloseButtonToTaskbar;

            this.SetInformation();
        }

        private void SetInformation()
        {
            // Version
            this.information_text.Inlines.Add(App.Title);
            this.information_text.Inlines.Add(@"
Copyright (c) 2020 archi-Doc
Released under the MIT license
");
            var mit_license = "https://opensource.org/licenses/MIT";

            var hl = new Hyperlink() { NavigateUri = new Uri(mit_license) };
            hl.Inlines.Add(mit_license);
            hl.RequestNavigate += (s, e) =>
            {
                try
                {
                    App.OpenBrowser(e.Uri.ToString());
                }
                catch
                {
                }
            };
            this.information_text.Inlines.Add(hl);
            this.information_text.Inlines.Add("\r\n\r\n    ");

            var h = new Hyperlink[3];
            h[0] = new Hyperlink() { Command = this.LicenseTextCommand, CommandParameter = "license.dryioc" };
            h[0].Inlines.Add("DryIoc");
            h[1] = new Hyperlink() { Command = this.LicenseTextCommand, CommandParameter = "license.prism" };
            h[1].Inlines.Add("Prism Library");
            h[2] = new Hyperlink() { Command = this.LicenseTextCommand, CommandParameter = "license.messagepack" };
            h[2].Inlines.Add("MessagePack for C#");

            foreach (var x in h)
            {
                this.information_text.Inlines.Add(x);
                this.information_text.Inlines.Add("\r\n    ");
            }

            this.information_text.Inlines.Add("STAR program");
        }

        /// <summary>
        /// Save settings.
        /// </summary>
        private void SaveSettings()
        {
            if (App.Settings.Culture != this.CurrentCulture)
            {// Change culture
                App.Settings.Culture = this.CurrentCulture;
                App.C4.ChangeCulture(App.Settings.Culture);
                Arc.WPF.C4Updater.C4Update();
            }

            if (App.Settings.DisplayScaling != this.CurrentDisplayScaling)
            {
                App.Settings.DisplayScaling = this.CurrentDisplayScaling;
                this.ViewService.MessageID(MessageId.DisplayScaling);
            }

            if (App.Settings.CloseButtonToTaskbar != this.CloseButtonToTaskbar)
            {
                App.Settings.CloseButtonToTaskbar = this.CloseButtonToTaskbar;
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Arc.WinAPI.Methods.RemoveIcon(this);
        }

        private void SettingsButtonOk(object sender, RoutedEventArgs e)
        {
            this.SaveSettings();
            this.Close();
        }

        private void SettingsButtonCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
        }
    }
}
