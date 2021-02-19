// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Application;
using Arc.CrossChannel;
using Arc.Mvvm;
using Arc.Text;
using Arc.WPF;
using Conductor.ViewServices;
using Serilog;

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1202 // Elements should be ordered by access

namespace Conductor.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public AppOptions Options => App.Options;

        private IMainViewService ViewService => App.Resolve<IMainViewService>(); // To avoid a circular dependency, get an instance when necessary.

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

        private string debugText = string.Empty;

        public string DebugText
        {
            get => this.debugText;
            set => this.SetProperty(ref this.debugText, value);
        }

        private HMSControlVM shutdownHMS = new HMSControlVM();

        public HMSControlVM ShutdownHMS
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

        private DelegateCommand? commandShutdown;

        public DelegateCommand CommandShutdown
        {
            get => this.commandShutdown ??= new DelegateCommand(
                    () =>
                    {
                        if (this.ShutdownHMS.Hour == 0 && this.ShutdownHMS.Minute == 0 && this.ShutdownHMS.Second == 0)
                        {
                            return;
                        }

                        App.Core.Shutdown(this.ShutdownHMS.Hour, this.ShutdownHMS.Minute, this.ShutdownHMS.Second);
                        this.timer.Stop();
                        this.timer.Start();
                        this.UpdateStatus(true);
                    },
                    () => !this.ActiveShutdown).ObservesProperty(() => this.ActiveShutdown);
        }

        private DelegateCommand? commandClear;

        public DelegateCommand CommandClear
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
        }

        private DelegateCommand? commandAbort;

        public DelegateCommand CommandAbort
        {
            get => this.commandAbort ??= new DelegateCommand(
                    () =>
                    {
                        App.Core.AbortShutdown();
                        this.UpdateStatus(true);
                    },
                    () => this.ActiveShutdown).ObservesProperty(() => this.ActiveShutdown);
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

        private bool activeShutdown;

        public bool ActiveShutdown
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
        }

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
