// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace StandardWinUI;

#if DISABLE_XAML_GENERATED_MAIN

public static partial class Entrypoint
{
    public static DispatcherQueue UiDispatcherQueue { get; private set; } = default!;

    public static string DataFolder { get; private set; } = string.Empty;

    private static Mutex? appMutex = string.IsNullOrEmpty(App.MutexName) ? default : new(false, App.MutexName);

    /// <summary>
    /// The entry point of the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    [STAThread]
    private static void Main(string[] args)
    {
        PrepareDataFolder();
        if (appMutex is not null &&
            UiHelper.PreventMultipleInstances(appMutex))
        {
            return;
        }

        AppUnit.Unit? unit = default;
        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            XamlCheckProcessRequirements(); // If an exception occurs here, run the Package project or set WindowsAppSDKSelfContained to true.
            Application.Start(_ =>
            {
                UiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
                var context = new DispatcherQueueSynchronizationContext(UiDispatcherQueue);
                SynchronizationContext.SetSynchronizationContext(context);

                var builder = new AppUnit.Builder();
                unit = builder.Build();
                var serviceProvider = unit.Context.ServiceProvider;
                var app = serviceProvider.GetRequiredService<App>();
                app.Initialize();
            });

            Task.Run(async () =>
            {// 'await task' does not work property.
                if (unit?.Context.ServiceProvider.GetService<Crystalizer>() is { } crystalizer)
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
            DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), App.DataFolderName);
        }

        try
        {
            Directory.CreateDirectory(DataFolder);
        }
        catch
        {
        }
    }

    [LibraryImport("Microsoft.ui.xaml.dll")]
    private static partial void XamlCheckProcessRequirements();
}

#endif
