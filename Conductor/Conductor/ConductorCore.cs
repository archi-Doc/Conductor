// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Text;
using Arc.WinUI;
using CrossChannel;

#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1649 // File name should match first type name

namespace Conductor;

public enum ConductorTaskType
{
    Shutdown,
    ShutdownProcess,
}

public class ConductorTask
{
    private DateTime lastDateTime;

    private double lastTotalSeconds;

    public ConductorTask(ConductorTaskType type, DateTime dateTime)
    {
        this.Type = type;
        this.DateTime = dateTime;
    }

    public ConductorTask(ConductorTaskType type, int hour, int minute, int second)
    {
        this.Type = type;
        this.DateTime = DateTime.UtcNow + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute) + TimeSpan.FromSeconds(second);
    }

    public ConductorTaskType Type { get; set; }

    public bool RelativeTime { get; set; } // false: absolute DateTime, true: relative

    public DateTime DateTime { get; set; }

    public void GetRemainingTime(out int hour, out int minute, out int second)
    {
        var ts = this.DateTime - DateTime.UtcNow;

        if (ts.Ticks <= 0)
        {
            hour = 0;
            minute = 0;
            second = 0;
            return;
        }

        hour = (ts.Days * 24) + ts.Hours;
        minute = ts.Minutes;
        second = ts.Seconds;
        return;
    }

    public void GetCorrectedRemainingTime(out int hour, out int minute, out int second)
    {
        var dt1 = DateTime.UtcNow;

        var ts = this.DateTime - dt1;
        if (ts.Ticks <= 0)
        { // Elapsed.
            this.lastDateTime = dt1;
            this.lastTotalSeconds = 0;

            hour = 0;
            minute = 0;
            second = 0;
            return;
        }

        var t1 = this.lastTotalSeconds - Math.Round((dt1 - this.lastDateTime).TotalSeconds);
        if (Math.Abs(t1 - ts.TotalSeconds) > 1)
        {
            t1 = Math.Round(ts.TotalSeconds);
            this.lastDateTime = dt1;
            this.lastTotalSeconds = t1;
        }
        else
        {
            if (t1 != this.lastTotalSeconds)
            {
                this.lastDateTime = dt1;
                this.lastTotalSeconds = t1;
            }
        }

        var ts1 = TimeSpan.FromSeconds(t1);
        hour = (ts1.Days * 24) + ts1.Hours;
        minute = ts1.Minutes;
        second = ts1.Seconds;
        return;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append("Type: ");
        sb.Append(this.Type.ToString());
        sb.Append(", Date: ");
        sb.Append(this.DateTime.ToLocalTime().ToString());

        return sb.ToString();
    }
}

public class ConductorCore
{
    public ConductorCore(ILogger<ConductorCore> logger, Crystalizer crystalizer)
    {
        this.logger = logger;
        this.crystalizer = crystalizer;
        this.Cpu = new ConductorCpu();
        this.Status = new ConductorStatus(this);
    }

    public int ShutdownWaitingSecond { get; set; } = 60;

    public double SuppressShutdownCpuUsage { get; set; } = 50;

    public ConductorTask? ShutdownTask { get; private set; } // ConductorTaskType: Shutdown or ShutdownProcess

    public bool PreventSleep { get; set; }

    public bool PreventScreenOff { get; set; }

    public bool PreventShutdownWhileBusy { get; set; }

    public ConductorCpu Cpu { get; }

    public ConductorStatus Status { get; }

    private readonly object cs = new();
    private readonly ILogger logger;
    private readonly Crystalizer crystalizer;

    public void Shutdown(int hour, int minute, int second)
    {
        lock (this.cs)
        {
            if (this.ShutdownTask != null)
            {
                return;
            }

            this.ShutdownTask = new ConductorTask(ConductorTaskType.Shutdown, hour, minute, second);
            this.logger.TryGet()?.Log(this.ShutdownTask.ToString());
        }
    }

    public bool AbortShutdown()
    {
        lock (this.cs)
        {
            if (this.ShutdownTask != null)
            {
                this.logger.TryGet()?.Log("Abort shutdown.");
                this.ShutdownTask = null;
                return true;
            }
        }

        return false;
    }

    public void ProcessEverySecond()
    {
        this.Cpu.ProcessEverySecond();

        var now = DateTime.UtcNow;
        lock (this.cs)
        {
            this.Status.ShutdownPending_CpuUsage = 0;

            if (this.ProcessTask(now, this.ShutdownTask))
            {
            }

            this.ProcessPrevention();
        }

        this.Status.Update();
    }

    private void ProcessPrevention()
    {
        Arc.WinAPI.EXECUTION_STATE state = 0;

        if (this.PreventScreenOff)
        {
            state |= Arc.WinAPI.EXECUTION_STATE.ES_DISPLAY_REQUIRED;
        }

        if (this.PreventSleep || this.ShutdownTask != null)
        {
            state |= Arc.WinAPI.EXECUTION_STATE.ES_SYSTEM_REQUIRED;
        }

        if (state != 0)
        {
            Arc.WinAPI.Methods.SetThreadExecutionState(state);
        }
    }

    private bool ProcessTask(DateTime now, ConductorTask? task)
    {
        if (task == null)
        {
            return false;
        }

        if (task.DateTime < now)
        {
            if (task.Type == ConductorTaskType.Shutdown)
            {
                if (this.PreventShutdownWhileBusy)
                {
                    var cpuUsage = this.Cpu.GetMaxAverage();
                    if (cpuUsage > this.SuppressShutdownCpuUsage)
                    {// Cpu Usage is above the threshold.
                        this.Status.ShutdownPending_CpuUsage = cpuUsage;
                        return false;
                    }
                }

                Arc.WinAPI.Methods.SetThreadExecutionState(Arc.WinAPI.EXECUTION_STATE.ES_DISPLAY_REQUIRED);
                Radio.Send<IConductorPresentationService>().ActivateWindow(true);

                if (this.ShutdownTask == null || this.ShutdownTask.Type != ConductorTaskType.ShutdownProcess)
                {
                    this.ShutdownTask = new ConductorTask(ConductorTaskType.ShutdownProcess, 0, 0, this.ShutdownWaitingSecond);
                    this.logger.TryGet()?.Log(this.ShutdownTask.ToString());
                }
            }
            else if (task.Type == ConductorTaskType.ShutdownProcess)
            {// Cpu Usage is above the threshold.
                if (this.PreventShutdownWhileBusy)
                {
                    var cpuUsage = this.Cpu.GetMaxAverage();
                    if (cpuUsage > this.SuppressShutdownCpuUsage)
                    {// Cpu Usage is above the threshold.
                        this.Status.ShutdownPending_CpuUsage = cpuUsage;
                        // this.ShutdownTask = new ConductorTask(ConductorTaskType.Shutdown, 0, 0, 0); // revert
                        return false;
                    }
                }

                this.logger.TryGet()?.Log("Shutdown process.");

                this.crystalizer.SaveAll().Wait();

                Arc.WinAPI.Methods.AdjustToken();
                Arc.WinAPI.Methods.ExitWindowsEx(Arc.WinAPI.ExitWindows.EWX_POWEROFF, 0);
                App.Exit();

                this.ShutdownTask = null;
            }

            return true;
        }

        return false;
    }

    public class ConductorStatus
    {
        public ConductorStatus(ConductorCore core)
        {
            this.Core = core;
        }

        public ConductorCore Core { get; }

        public string Text { get; private set; } = string.Empty;

        public bool CanShutdown { get; private set; } = true;

        public bool ShutdownProcess { get; private set; }

        public string ShutdownStatusText { get; private set; } = string.Empty;

        public int ShutdownHours { get; private set; }

        public int ShutdownMinutes { get; private set; }

        public int ShutdownSeconds { get; private set; }

        public int ShutdownTotalSeconds { get; private set; }

        public double ShutdownPending_CpuUsage { get; set; }

        public void Update()
        {
            lock (this.Core.cs)
            {
                if (this.Core.ShutdownTask != null)
                {
                    this.Core.ShutdownTask.GetCorrectedRemainingTime(out var hour, out var minute, out var second);
                    this.ShutdownHours = hour;
                    this.ShutdownMinutes = minute;
                    this.ShutdownSeconds = second;
                    this.ShutdownTotalSeconds = (hour * 3600) + (minute * 60) + second;

                    this.CanShutdown = false;
                    this.ShutdownProcess = this.Core.ShutdownTask.Type == ConductorTaskType.ShutdownProcess;
                    this.ShutdownStatusText = string.Empty;
                }
                else
                {
                    this.CanShutdown = true;
                    this.ShutdownProcess = false;
                    this.ShutdownStatusText = string.Empty;
                }
            }
        }
    }
}
