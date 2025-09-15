// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1202
#pragma warning disable SA1208
#pragma warning disable SA1210
#pragma warning disable SA1514

global using System;
global using Arc.Threading;
global using Arc.Unit;
global using Arc.WinUI;
global using CrystalData;
global using Microsoft.Extensions.DependencyInjection;
global using StandardWinUI;
global using Tinyhand;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Conductor;
using Conductor.PresentationState;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace StandardWinUI;

// TODO: Rename 'StandardWinUI' and modify the app-specific constants, icons and images.
// Dependencies and data persistence: AppUnit.
// Presentation-State model: 5.Advanced is equipped with basic functionalities, it is recommended to use this as a template.

// AppSettings manages the application's settings.
// IApp.GetService<T>() is used to retrieve a service of type T.
// IApp.TryExit() attempts to exit the app, while IApp.Exit() exits the app without confirmation.
// NaviWindow_Closed() is called when the main window is closed.

/// <summary>
/// App class is an application-specific class.<br/>
/// It manages various application-specific information, such as language and settings.
/// </summary>
public class App : AppBase
{
    public const string MutexName = "Arc.Conductor"; // The name of the mutex used to prevent multiple instances of the application. Specify 'string.Empty' to allow multiple instances.
    public const string DataFolderName = "Arc\\Conductor"; // The folder name for application data.
    public const string AppDataFile = "App.tinyhand"; // The file name for application data.
    public const string DefaultCulture = "en"; // The default culture for the application.
    public const double DefaultFontSize = 14; // The default font size for the application.

    /// <summary>
    /// Gets the settings for the application.
    /// </summary>
    public AppSettings Settings { get; private set; } = default!;

    private void LoadCrystalData()
    {
        var crystalizer = this.GetService<Crystalizer>();
        crystalizer.PrepareAndLoad(false).Wait();
        this.Settings = crystalizer.GetCrystal<AppSettings>().Data;
    }

    /// <summary>
    /// Loads the localized strings for the application.
    /// </summary>
    private void LoadStrings()
    {
        try
        {
            HashedString.SetDefaultCulture(DefaultCulture); // default culture
            LanguageList.Add("en", "Language.En");
            LanguageList.Add("ja", "Language.Ja");

            var asm = Assembly.GetExecutingAssembly();
            LanguageList.LoadHashedString(asm);
            HashedString.LoadAssembly("en", asm, "Resources.Strings.License.tinyhand"); // license
        }
        catch
        {
        }
    }

    /// <summary>
    /// Prepares the culture for the application.
    /// </summary>
    private void PrepareCulture()
    {
        try
        {
            if (this.Settings.Culture == string.Empty)
            {
                this.Settings.Culture = "en"; // English
                if (CultureInfo.CurrentUICulture.Name == "ja-JP")
                {
                    this.Settings.Culture = "ja";
                }
            }

            HashedString.ChangeCulture(this.Settings.Culture);
        }
        catch
        {
            this.Settings.Culture = DefaultCulture;
            HashedString.ChangeCulture(this.Settings.Culture);
        }
    }

    public override Application GetApplication()
        => this.GetService<StandardApp>();

    public override Window GetMainWindow()
        => this.GetService<NaviWindow>();

    public override Task<bool> TryExit(CancellationToken cancellationToken = default)
    {
        return this.UiDispatcherQueue.EnqueueAsync(async () =>
        {
            var result = await this.GetService<IMessageDialogService>().ShowMessageDialogAsync(0, Hashed.Dialog.Exit, Hashed.Dialog.Yes, Hashed.Dialog.No, 0, cancellationToken);
            if (result.TryGetSingleResult(out var r) && r == ContentDialogResult.Primary)
            {// Exit
                this.Exit();
                return true;
            }
            else
            {// Canceled
                return false;
            }
        });
    }

    #region Common

    public App(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        this.DataFolder = Entrypoint.DataFolder;
        this.UiDispatcherQueue = Entrypoint.UiDispatcherQueue;

        this.LoadStrings();
        this.LoadCrystalData();
        this.PrepareCulture();

        // Version
        try
        {
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            this.Version = $"{version.Major}.{version.Minor}.{version.Build}";
        }
        catch
        {
            this.Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        }

        // Title
        this.Title = HashedString.Get(Hashed.App.Name) + " " + this.Version;
    }

    #endregion
}
