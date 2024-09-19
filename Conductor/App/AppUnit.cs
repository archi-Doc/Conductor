// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Conductor.Presentation;
using Conductor.State;
using CrossChannel;
using SimpleCommandLine;

namespace Conductor;

public class AppUnit : UnitBase, IUnitPreparable, IUnitExecutable
{
    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {
            // Configuration for Unit.
            this.Configure(context =>
            {
                context.AddSingleton<AppUnit>();
                context.AddSingleton<AppClass>();
                // context.CreateInstance<AppUnit>();
                context.AddSingleton<ConductorCore>();

                // CrossChannel
                context.Services.AddCrossChannel();

                // Views and ViewModels
                context.AddTransient<SimpleWindow>();
                context.AddTransient<SimpleState>();
                context.AddTransient<NaviWindow>();
                context.AddTransient<HomePage>(); // AddSingleton
                context.AddTransient<HomePageState>();
                context.AddTransient<SettingsPage>();
                context.AddTransient<SettingsState>();
                context.AddTransient<InformationPage>();
                context.AddTransient<InformationState>();

                context.AddTransient<HMSControlState>();

                // Command
                // context.AddCommand(typeof(TestCommand));
                // context.AddCommand(typeof(TestCommand2));

                // Log filter
                context.AddSingleton<ExampleLogFilter>();

                // Logger
                context.ClearLoggerResolver();
                context.AddLoggerResolver(x =>
                {// Log source/level -> Resolver() -> Output/filter
                    x.SetOutput<FileLogger<FileLoggerOptions>>();

                    // if (x.LogLevel <= LogLevel.Debug)
                    // {
                    //    x.SetOutput<ConsoleLogger>();
                    //    return;
                    // }

                    // x.SetOutput<ConsoleAndFileLogger>();

                    // if (x.LogSourceType == typeof(TestCommand))
                    // {
                    //    x.SetFilter<ExampleLogFilter>();
                    // }
                });
            });

            this.Preload(context =>
            {
                context.RootDirectory = App.DataFolder;
                context.DataDirectory = App.DataFolder;
            });

            this.SetupOptions<FileLoggerOptions>((context, options) =>
            {// FileLoggerOptions
                var logfile = "Logs/Log.txt";
                options.Path = Path.Combine(context.RootDirectory, logfile);
                options.MaxLogCapacity = 2;
                options.ClearLogsAtStartup = false;
            });

            this.AddBuilder(CrystalBuilder());
        }

        private static CrystalControl.Builder CrystalBuilder()
        {
            return new CrystalControl.Builder()
                .ConfigureCrystal(context =>
                {
                    context.AddCrystal<AppSettings>(new()
                    {
                        NumberOfFileHistories = 0,
                        FileConfiguration = new GlobalFileConfiguration(AppSettings.Filename),
                        SaveFormat = SaveFormat.Utf8,
                    });

                    context.AddCrystal<AppOptions>(new()
                    {
                        NumberOfFileHistories = 0,
                        FileConfiguration = new GlobalFileConfiguration(AppOptions.Filename),
                        SaveFormat = SaveFormat.Utf8,
                    });
                });
        }
    }

    public class Unit : BuiltUnit
    {// Unit class for customizing behaviors.
        public record Param(string Args);

        public Unit(UnitContext context)
            : base(context)
        {
        }

        public async Task RunAsync(Param param)
        {
            // Create optional instances
            this.Context.CreateInstances();

            this.Context.SendPrepare(new());
            await this.Context.SendStartAsync(new(ThreadCore.Root));

            var parserOptions = SimpleParserOptions.Standard with
            {
                ServiceProvider = this.Context.ServiceProvider,
                RequireStrictCommandName = false,
                RequireStrictOptionName = true,
            };

            // Main
            await SimpleParser.ParseAndRunAsync(this.Context.Commands, param.Args, parserOptions);

            this.Context.SendStop(new());
            await this.Context.SendTerminateAsync(new());
        }
    }

    private class ExampleLogFilter : ILogFilter
    {
        public ExampleLogFilter(AppUnit consoleUnit)
        {
            this.consoleUnit = consoleUnit;
        }

        public ILogWriter? Filter(LogFilterParameter param)
        {// Log source/Event id/LogLevel -> Filter() -> ILog
            if (param.LogSourceType == typeof(AppClass))
            {
                // return null; // No log
                if (param.LogLevel == LogLevel.Error)
                {
                    return param.Context.TryGet<ConsoleAndFileLogger>(LogLevel.Fatal); // Error -> Fatal
                }
                else if (param.LogLevel == LogLevel.Fatal)
                {
                    return param.Context.TryGet<ConsoleAndFileLogger>(LogLevel.Error); // Fatal -> Error
                }
            }

            return param.OriginalLogger;
        }

        private AppUnit consoleUnit;
    }

    public AppUnit(UnitContext context, ILogger<AppUnit> logger, UnitOptions options)
        : base(context)
    {
        this.logger = logger;
        this.options = options;
    }

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
        this.logger.TryGet()?.Log("Unit prepared.");
        this.logger.TryGet()?.Log($"Root: {this.options.RootDirectory}");
        this.logger.TryGet()?.Log($"Data: {this.options.DataDirectory}");
    }

    async Task IUnitExecutable.StartAsync(UnitMessage.StartAsync message, CancellationToken cancellationToken)
    {
        this.logger.TryGet()?.Log("Unit started.");
    }

    void IUnitExecutable.Stop(UnitMessage.Stop message)
    {
        this.logger.TryGet()?.Log("Unit stopped.");
    }

    async Task IUnitExecutable.TerminateAsync(UnitMessage.TerminateAsync message, CancellationToken cancellationToken)
    {
        this.logger.TryGet()?.Log("Unit terminated.");
    }

    private readonly ILogger logger;
    private readonly UnitOptions options;
}
