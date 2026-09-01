// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Conductor;
using Conductor.PresentationState;
using Conductor.State;
using CrossChannel;
using SimpleCommandLine;

namespace StandardWinUI;

public class AppUnit : UnitBase, IUnitPreparable, IUnitExecutable
{
    public class Builder : UnitBuilder<Product>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {
            // Configuration for Unit.
            this.Configure(context =>
            {
                // context.AddSingleton<AppUnit>();
                context.AddSingleton<StandardApp>();
                context.AddSingleton<IApp, App>();
                // context.Services.AddSingleton(x => (App)x.GetRequiredService<IApp>()); // If you want to use the App instance, please uncomment it.

                context.AddSingleton<ConductorCore>();

                // Views and ViewModels
                context.AddSingleton<NaviWindow>();
                context.AddSingleton<HomePage>(); // AddSingleton
                context.AddSingleton<HomePageState>();
                context.AddSingleton<SettingsPage>();
                context.AddSingleton<SettingsState>();
                context.AddSingleton<InformationPage>();
                context.AddSingleton<InformationState>();

                context.AddTransient<HMSControlState>();

                // Command
                // context.AddCommand(typeof(TestCommand));
                // context.AddCommand(typeof(TestCommand2));

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

            this.PreConfigure(context =>
            {
                context.ProgramDirectory = Entrypoint.DataFolder;
                context.DataDirectory = Entrypoint.DataFolder;
            }).PostConfigure(context =>
            {
                var logfile = "Logs/Log.txt";
                context.SetOptions(context.GetOptions<FileLoggerOptions>() with
                {// FileLoggerOptions
                    Path = Path.Combine(context.DataDirectory, logfile),
                    MaxLogCapacity = 2,
                    ClearLogsAtStartup = false,
                });
            });

            this.AddBuilder(CrystalBuilder());
        }

        private static CrystalUnit.Builder CrystalBuilder()
        {
            return new CrystalUnit.Builder()
                .ConfigureCrystal(context =>
                {
                    context.AddCrystal<AppSettings>(new()
                    {
                        NumberOfFileHistories = 0,
                        FileConfiguration = new GlobalFileConfiguration(AppSettings.Filename),
                        SaveFormat = SaveFormat.Utf8,
                    });
                });
        }
    }

    public class Product : UnitProduct
    {// Unit class for customizing behaviors.
        public record Param(string Args);

        public Product(UnitContext context)
            : base(context)
        {
        }

        public async Task RunAsync(Param param)
        {
            // Create optional instances
            this.Context.CreateInstances();

            await this.Context.SendPrepare();
            await this.Context.SendStart();

            var parserOptions = SimpleParserOptions.Standard with
            {
                ServiceProvider = this.Context.ServiceProvider,
                RequireStrictCommandName = false,
                RequireStrictOptionName = true,
            };

            // Main
            await SimpleParser.ParseAndExecute(this.Context.Commands, param.Args, parserOptions);

            await this.Context.SendStop();
            await this.Context.SendTerminate();
        }
    }

    public AppUnit(UnitContext context, ILogger<AppUnit> logger, UnitOptions options)
        : base(context)
    {
        this.logger = logger;
        this.options = options;
    }

    async Task IUnitPreparable.Prepare(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit prepared.");
        this.logger.GetWriter()?.Write($"Root: {this.options.ProgramDirectory}");
        this.logger.GetWriter()?.Write($"Data: {this.options.DataDirectory}");
    }

    async Task IUnitExecutable.Start(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit started.");
    }

    async Task IUnitExecutable.Stop(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit stopped.");
    }

    async Task IUnitExecutable.Terminate(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit terminated.");
    }

    private readonly ILogger logger;
    private readonly UnitOptions options;
}
