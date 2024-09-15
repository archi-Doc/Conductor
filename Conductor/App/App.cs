// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1202
#pragma warning disable SA1208
#pragma warning disable SA1210
#pragma warning disable SA1514

global using System;
global using Conductor;
global using Arc.Threading;
global using Arc.Unit;
global using CrystalData;
global using Microsoft.Extensions.DependencyInjection;
global using Tinyhand;

using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Conductor;

#if DISABLE_XAML_GENERATED_MAIN

// Dependencies and data persistence: AppUnit.
// Presentation-State model: StatePage is equipped with basic functionalities, it is recommended to use this as a template.

// App.GetService<T>() is used to retrieve a service of type T.
// AppClass.OnLaunched() is the entry point of the application.
// AppSettings and AppOptions are classes that save the configuration information of the app.
// IBasicPresentationService.TryExit() attempts to exit the app, while App.Exit() exits the app without confirmation.
// NaviWindow_Closed() is called when the main window is closed.

public static partial class App
{
    public const string MutexName = "Arc.Conductor"; // The name of the mutex used to prevent multiple instances of the application. Specify 'string.Empty' to allow multiple instances.
    public const string AppDataFolder = "Arc\\Conductor"; // The folder name for application data.
    public const string AppDataFile = "App.tinyhand"; // The file name for application data.
    public const string DefaultCulture = "en"; // The default culture for the application.
    public const double DefaultFontSize = 14; // The default font size for the application.

    /// <summary>
    /// Loads the localized strings for the application.
    /// </summary>
    private static void LoadStrings()
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
    private static void PrepareCulture()
    {
        try
        {
            if (Settings.Culture == string.Empty)
            {
                Settings.Culture = "en"; // English
                if (CultureInfo.CurrentUICulture.Name == "ja-JP")
                {
                    Settings.Culture = "ja";
                }
            }

            HashedString.ChangeCulture(App.Settings.Culture);
        }
        catch
        {
            Settings.Culture = App.DefaultCulture;
            HashedString.ChangeCulture(Settings.Culture);
        }
    }

    #region FieldAndProperty

    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    public static string Version { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the title of the application.
    /// </summary>
    public static string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the folder path for application data.
    /// </summary>
    public static string DataFolder { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the settings for the application.
    /// </summary>
    public static AppSettings Settings { get; private set; } = default!;

    /// <summary>
    /// Gets the options for the application.
    /// </summary>
    public static AppOptions Options { get; private set; } = default!;

    public static bool IsUiThread => uiDispatcherQueue.HasThreadAccess;

    private static Mutex? appMutex = string.IsNullOrEmpty(MutexName) ? default : new(false, MutexName);
    private static DispatcherQueue uiDispatcherQueue = default!;
    private static IServiceProvider serviceProvider = default!;
    private static Crystalizer? crystalizer;
    private static AppClass? appClass;

    #endregion

    /// <summary>
    /// The entry point of the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    [STAThread]
    private static void Main(string[] args)
    {
        LoadStrings();
        PrepareDataFolder();
        PrepareVersionAndTitle();
        if (appMutex is not null &&
            Arc.WinUI.Helper.PreventMultipleInstances(appMutex, Title))
        {
            return;
        }

        AppUnit.Unit? unit = default;
        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            XamlCheckProcessRequirements();
            Application.Start(_ =>
            {
                uiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
                var context = new DispatcherQueueSynchronizationContext(uiDispatcherQueue);
                SynchronizationContext.SetSynchronizationContext(context);

                var builder = new AppUnit.Builder();
                unit = builder.Build();
                serviceProvider = unit.Context.ServiceProvider;

                PrepareCrystalizer();
                PrepareCulture();
                appClass = GetService<AppClass>();
            });

            Task.Run(async () =>
            {// 'await task' does not work property.
                if (crystalizer is not null)
                {
                    await crystalizer.SaveAllAndTerminate();
                }

                ThreadCore.Root.Terminate();
                await ThreadCore.Root.WaitForTerminationAsync(-1);
                if (unit?.Context.ServiceProvider.GetService<UnitLogger>() is { } unitLogger)
                {
                    await unitLogger.FlushAndTerminate();
                }
            }).Wait();
        }
        finally
        {
            if (appMutex is not null)
            {
                appMutex.ReleaseMutex();
                appMutex.Close();
            }
        }
    }

    /// <summary>
    /// Retrieves a service of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the service.</typeparam>
    /// <returns>The service instance.</returns>
    public static T GetService<T>()
        where T : class
    {
        if (serviceProvider.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in Configure within AppUnit.cs.");
        }

        return service;
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    public static void Exit()
    {
        appClass?.Exit();
    }

    /// <summary>
    /// Executes an action on the UI thread.
    /// </summary>
    /// <param name="callback">The action that will be executed on the UI thread.</param>
    public static void TryEnqueueOnUI(DispatcherQueueHandler callback)
        => uiDispatcherQueue.TryEnqueue(callback);

    [LibraryImport("Microsoft.ui.xaml.dll")]
    private static partial void XamlCheckProcessRequirements();

    private static void PrepareDataFolder()
    {
        // Data Folder
        try
        {
            // UWP
            DataFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }
        catch
        {
            // not UWP
            DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppDataFolder);
        }

        try
        {
            Directory.CreateDirectory(DataFolder);
        }
        catch
        {
        }
    }

    private static void PrepareVersionAndTitle()
    {
        // Version
        try
        {
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            Version = $"{version.Major}.{version.Minor}.{version.Build}";
        }
        catch
        {
            Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        }

        // Title
        Title = HashedString.Get(Hashed.App.Name) + " " + Version;
    }

    private static void PrepareCrystalizer()
    {
        crystalizer = GetService<Crystalizer>();
        crystalizer.PrepareAndLoadAll(false).Wait();

        // Load settings and options.
        Settings = crystalizer.GetCrystal<AppSettings>().Data;
        Options = crystalizer.GetCrystal<AppOptions>().Data;
    }
}
#endif
