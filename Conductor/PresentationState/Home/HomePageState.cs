// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Timers;
using System.Windows.Input;
using Arc.WinUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conductor.Presentation;

namespace Conductor.State;

public partial class HomePageState : ObservableObject
{
    private readonly IBasicPresentationService simpleWindowService;

    [ObservableProperty]
    private string resultTextValue = string.Empty;

    [ObservableProperty]
    private bool toggleCopyToClipboard;

    public HomePageState(IBasicPresentationService simpleWindowService)
    {
        this.simpleWindowService = simpleWindowService;
        this.ToggleCopyToClipboard = true;
    }

    public void StoreState()
    {
    }

    [RelayCommand]
    private void Generate(string param)
    {
    }

    private Timer timer;

    private string iconSource = string.Empty;

    public string IconSource
    {
        get => this.iconSource;
        set => this.SetProperty(ref this.iconSource, value);
    }

    private string conductorText = string.Empty;

    public string ConductorText
    {
        get => this.conductorText;
        set => this.SetProperty(ref this.conductorText, value);
    }

    private string cpuStatusText = string.Empty;

    public string CpuStatusText
    {
        get => this.cpuStatusText;
        set => this.SetProperty(ref this.cpuStatusText, value);
    }

    private HMSControlState shutdownHMS = new HMSControlState();

    public HMSControlState ShutdownHMS
    {
        get
        {
            return this.shutdownHMS;
        }

        set
        {
            this.SetProperty(ref this.shutdownHMS, value);
        }
    }

    [RelayCommand(CanExecute = nameof(ActiveShutdown))]
    private void Shutdown()
    {
        if (this.ShutdownHMS.Hour == 0 && this.ShutdownHMS.Minute == 0 && this.ShutdownHMS.Second == 0)
        {
            return;
        }

        App.Core.Shutdown(this.ShutdownHMS.Hour, this.ShutdownHMS.Minute, this.ShutdownHMS.Second);
        this.timer.Stop();
        this.timer.Start();
        this.UpdateStatus(true);
    }

    [RelayCommand(CanExecute =)]
    private void Clear()
    {
        this.ShutdownHMS.Clear();
    }


    /*public DelegateCommand CommandClear
    {
        get => this.commandClear ??= new DelegateCommand(
                () =>
                {
                    this.ShutdownHMS.Clear();
                },
                () => !this.ActiveShutdown &&
                (this.ShutdownHMS.HourText != string.Empty ||
                this.ShutdownHMS.MinuteText != string.Empty ||
                this.ShutdownHMS.SecondText != string.Empty))
            .ObservesProperty(() => this.ActiveShutdown)
            .ObservesProperty(() => this.ShutdownHMS.HourText)
            .ObservesProperty(() => this.ShutdownHMS.MinuteText)
            .ObservesProperty(() => this.ShutdownHMS.SecondText);
    }*/

    [RelayCommand(CanExecute = nameof(ActiveShutdown))]
    private void Abort()
    {
        App.Core.AbortShutdown();
        this.UpdateStatus(true);
    }

    private ICommand? taskbarCommand;

    public ICommand TaskbarCommand
    {
        get => this.taskbarCommand ??= new DelegateCommand<string>(
                (param) =>
                { // execute
                    var id = (TaskbarCommandId)Enum.Parse(typeof(TaskbarCommandId), param!);
                    if (id == TaskbarCommandId.TogglePreventScreenOff)
                    {
                        this.TogglePreventScreenOff = !this.TogglePreventScreenOff;
                    }
                    else if (id == TaskbarCommandId.TogglePreventSleep)
                    {
                        this.TogglePreventSleep = !this.TogglePreventSleep;
                    }
                });
    }

    private ICommand? taskbarCommandExit;

    public ICommand TaskbarCommandExit
    {
        get
        {
            return (this.taskbarCommandExit != null) ? this.taskbarCommandExit : this.taskbarCommandExit = new DelegateCommand(
                () =>
                { // execute
                    this.ViewService.MessageID(MessageId.ExitWithoutConfirmation);
                });
        }
    }

    private bool togglePreventSleep;

    public bool TogglePreventSleep
    {
        get => this.togglePreventSleep;
        set
        {
            this.SetProperty(ref this.togglePreventSleep, value);
            this.SetNotifyIcon();
            App.Core.PreventSleep = value;
        }
    }

    private bool togglePreventScreenOff;

    public bool TogglePreventScreenOff
    {
        get => this.togglePreventScreenOff;
        set
        {
            this.SetProperty(ref this.togglePreventScreenOff, value);
            this.SetNotifyIcon();
            App.Core.PreventScreenOff = value;
        }
    }

    private bool togglePreventShutdownWhileBusy;

    public bool TogglePreventShutdownWhileBusy
    {
        get => this.togglePreventShutdownWhileBusy;
        set
        {
            this.SetProperty(ref this.togglePreventShutdownWhileBusy, value);
            App.Core.PreventShutdownWhileBusy = value;
            App.Settings.TogglePreventShutdownWhileBusy = value;
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShutdownCommand))]
    private bool activeShutdown;

    partial void OnActiveShutdownChanged(bool value)
    {
        this.SetNotifyIcon();
    }

    /*public bool ActiveShutdown
    {
        get => this.activeShutdown;
        set
        {
            if (this.activeShutdown == value)
            {
                return;
            }

            this.SetProperty(ref this.activeShutdown, value);
            this.SetNotifyIcon();
        }
    }*/

    private ICommand? commandMessageId;

    public ICommand CommandMessageId
    {
        get
        {
            return (this.commandMessageId != null) ? this.commandMessageId : this.commandMessageId = new DelegateCommand<string>(
                (param) =>
                { // execute
                    var id = (MessageId)Enum.Parse(typeof(MessageId), param!);
                    this.ViewService.MessageID(id);
                });
        }
    }

    public MainViewModel()
    {
        this.TogglePreventShutdownWhileBusy = App.Settings.TogglePreventShutdownWhileBusy;

        this.SetNotifyIcon();

        this.timer = new Timer(1000); // 1 second
        this.timer.Elapsed += (sender, e) =>
        {
            this.Timer(e);
        };
        this.timer.Start();
    }

    public void Timer(ElapsedEventArgs elapsedEventArgs)
    {
        App.Core.ProcessEverySecond();
        this.UpdateStatus(false);
    }

    private void UpdateStatus(bool update)
    {
        if (update)
        {
            App.Core.Status.Update();
        }

        this.ShutdownHMS.IsReadOnly = App.Core.Status.ActiveShutdown;
        this.ShutdownHMS.StatusText = App.Core.Status.ShutdownStatusText;
        if (App.Core.Status.ActiveShutdown)
        {
            if (App.Core.Status.ShutdownProcess == false)
            {
                this.ConductorText = C4.Instance["core.shutdown.remaining"];
                this.ShutdownHMS.Hour = App.Core.Status.ShutdownHours;
                this.ShutdownHMS.Minute = App.Core.Status.ShutdownMinutes;
                this.ShutdownHMS.Second = App.Core.Status.ShutdownSeconds;
            }
            else
            {
                this.ConductorText = string.Format(C4.Instance["core.shuttingdown"], App.Core.Status.ShutdownTotalSeconds);
                this.ShutdownHMS.Second = 0;
            }

            if (App.Core.Status.ShutdownPending_CpuUsage != 0)
            {
                this.ConductorText = string.Format(C4.Instance["core.shuttingdown.cpu"], App.Core.Status.ShutdownPending_CpuUsage);
            }
        }
        else
        {
            this.ConductorText = string.Empty;
        }

        this.CpuStatusText = string.Format(App.C4["core.cpustatus"], App.Core.Cpu.GetMaxAverage(), App.Core.Cpu.GetAverage());

        // this.DebugText = App.Core.Cpu.GetMaxAverage().ToString();

        this.ActiveShutdown = App.Core.Status.ActiveShutdown;
    }

    private void SetNotifyIcon()
    {
        var icon = @"/Resources/Conductor6.ico";

        if (this.TogglePreventScreenOff)
        {
            icon = @"/Resources/Conductor6y.ico";
        }

        if (this.TogglePreventSleep)
        {
            icon = @"/Resources/Conductor6o.ico";
        }

        if (App.Core.ShutdownTask != null)
        {
            icon = @"/Resources/Conductor6r.ico";
        }

        this.IconSource = icon;
    }
}
}
