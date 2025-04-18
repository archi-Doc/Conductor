﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using System.Timers;
using Arc.WinUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conductor.PresentationState;

namespace Conductor.State;

public partial class HomePageState : ObservableObject, IState
{
    private readonly IApp app;
    private readonly AppSettings appSettings;
    private readonly ConductorCore core;
    private readonly IMessageDialogService messageDialogService;
    private readonly IConductorPresentationService conductorPresentationService;
    private readonly Timer timer;

    [ObservableProperty]
    public partial string ResultTextValue { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string IconPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConductorText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CpuStatusText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial HMSControlState ShutdownHMS { get; set; } = new HMSControlState();

    public HomePageState(IApp app, AppSettings appSettings, ConductorCore core, IMessageDialogService messageDialogService, IConductorPresentationService conductorPresentationService)
    {
        this.app = app;
        this.appSettings = appSettings;
        this.core = core;
        this.messageDialogService = messageDialogService;
        this.conductorPresentationService = conductorPresentationService;
        this.TogglePreventShutdownWhileBusy = appSettings.TogglePreventShutdownWhileBusy;

        this.SetNotifyIcon();

        this.timer = new Timer(1000); // 1 second
        this.timer.Elapsed += (sender, e) =>
        {
            this.Timer(e);
        };
        this.timer.Start();

        this.ShutdownHMS.TimeModified += sender => this.UpdateCommandState();
    }

    public void UpdateStatus(bool update)
    {
        if (update)
        {
            this.core.Status.Update();
        }

        this.ShutdownHMS.IsReadOnly = !this.core.Status.CanShutdown;
        this.ShutdownHMS.StatusText = this.core.Status.ShutdownStatusText;
        if (!this.core.Status.CanShutdown)
        {
            if (this.core.Status.ShutdownProcess == false)
            {
                this.ConductorText = HashedString.Get("core_shutdown_remaining");
                this.ShutdownHMS.Hour = this.core.Status.ShutdownHours;
                this.ShutdownHMS.Minute = this.core.Status.ShutdownMinutes;
                this.ShutdownHMS.Second = this.core.Status.ShutdownSeconds;
            }
            else
            {
                this.ConductorText = string.Format(HashedString.Get("core_shuttingdown"), this.core.Status.ShutdownTotalSeconds);
                this.ShutdownHMS.Second = 0;
            }

            if (this.core.Status.ShutdownPending_CpuUsage != 0)
            {
                this.ConductorText = string.Format(HashedString.Get("core_shuttingdown_cpu"), this.core.Status.ShutdownPending_CpuUsage);
            }
        }
        else
        {
            this.ConductorText = string.Empty;
        }

        this.CpuStatusText = string.Format(HashedString.Get("core_cpustatus"), this.core.Cpu.GetMaxAverage(), this.core.Cpu.GetAverage());
    }

    private void UpdateCommandState()
    {
        this.ShutdownCommand.NotifyCanExecuteChanged();
        this.AbortCommand.NotifyCanExecuteChanged();
        this.ClearCommand.NotifyCanExecuteChanged();
    }

    private bool CanShutdown
        => this.core.Status.CanShutdown;

    [RelayCommand(CanExecute = nameof(CanShutdown))]
    private void Shutdown()
    {
        this.core.Shutdown(this.ShutdownHMS.Hour, this.ShutdownHMS.Minute, this.ShutdownHMS.Second);
        this.timer.Stop();
        this.timer.Start();
        this.UpdateStatus(true);
        this.UpdateCommandState();
    }

    private bool CanClear
        => this.core.Status.CanShutdown &&
            (this.ShutdownHMS.HourText != string.Empty ||
            this.ShutdownHMS.MinuteText != string.Empty ||
            this.ShutdownHMS.SecondText != string.Empty);

    [RelayCommand(CanExecute = nameof(CanClear))]
    private void Clear()
    {
        this.ShutdownHMS.Clear();
        this.UpdateCommandState();
    }

    private bool CanAbort
        => !this.core.Status.CanShutdown;

    [RelayCommand(CanExecute = nameof(CanAbort))]
    private void Abort()
    {
        this.core.AbortShutdown();
        this.UpdateStatus(true);
        this.UpdateCommandState();
    }

    [RelayCommand]
    private void Taskbar(string param)
    {
        if (!Enum.TryParse<TaskbarCommandId>(param, out var id))
        {
            return;
        }

        if (id == TaskbarCommandId.TogglePreventScreenOff)
        {
            this.TogglePreventScreenOff = !this.TogglePreventScreenOff;
        }
        else if (id == TaskbarCommandId.TogglePreventSleep)
        {
            this.TogglePreventSleep = !this.TogglePreventSleep;
        }
    }

    [RelayCommand]
    private void TaskbarExit()
    {
        this.app.Exit();
    }

    [ObservableProperty]
    public partial bool TogglePreventSleep { get; set; }

    partial void OnTogglePreventSleepChanged(bool value)
    {
        this.SetNotifyIcon();
        this.core.PreventSleep = value;
    }

    [ObservableProperty]
    public partial bool TogglePreventScreenOff { get; set; }

    partial void OnTogglePreventScreenOffChanged(bool value)
    {
        this.SetNotifyIcon();
        this.core.PreventScreenOff = value;
    }

    [ObservableProperty]
    public partial bool TogglePreventShutdownWhileBusy { get; set; }

    partial void OnTogglePreventShutdownWhileBusyChanged(bool value)
    {
        this.core.PreventShutdownWhileBusy = value;
        this.appSettings.TogglePreventShutdownWhileBusy = value;
    }

    [RelayCommand]
    private void ActivateWindow()
    {
        this.conductorPresentationService.ActivateWindow();
    }

    private void Timer(ElapsedEventArgs elapsedEventArgs)
    {
        this.core.ProcessEverySecond();
        this.app.UiDispatcherQueue.TryEnqueue(() => this.UpdateStatus(false));
    }

    private void SetNotifyIcon()
    {
        var icon = @"ms-appx:///Resources/Conductor6.ico";

        if (this.TogglePreventScreenOff)
        {
            icon = @"ms-appx:///Resources/Conductor6y.ico";
        }

        if (this.TogglePreventSleep)
        {
            icon = @"ms-appx:///Resources/Conductor6o.ico";
        }

        if (this.core.ShutdownTask != null)
        {
            icon = @"ms-appx:///Resources/Conductor6r.ico";
        }

        this.IconPath = icon;
    }
}
