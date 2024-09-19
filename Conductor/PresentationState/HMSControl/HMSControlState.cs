// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Conductor.State;

public class HMSControlState : ObservableObject
{ // Hour/Minute/Second
    #region FieldAndProperty

    private int hour;

    private int minute;

    private int second;

    private string statusText = string.Empty;

    private string hourText = string.Empty;

    private string minuteText = string.Empty;

    private string secondText = string.Empty;

    private bool isReadOnly = false;

    private bool hideSecondText = false;

    public bool Circular { get; set; } = false;

    public bool IsValid => this.hour >= 0 && this.minute >= 0 && this.second >= 0;

    public delegate void TimeModifiedEventHandler(HMSControlState sender);

    public event TimeModifiedEventHandler? TimeModified;

    public int Hour
    {
        get => this.hour;
        set => this.SetHour(value, true);
    }

    public int Minute
    {
        get => this.minute;
        set => this.SetMinute(value, true);
    }

    public int Second
    {
        get => this.second;
        set => this.SetSecond(value, true);
    }

    public string StatusText
    {
        get => this.statusText;
        set => this.SetProperty(ref this.statusText, value);
    }

    public string HourText
    {
        get => this.hourText;
        set
        {
            this.SetProperty(ref this.hourText, value);
            int.TryParse(this.hourText, out var result);
            this.SetHour(result, false);
            this.TimeModified?.Invoke(this);
        }
    }

    public string MinuteText
    {
        get => this.minuteText;
        set
        {
            this.SetProperty(ref this.minuteText, value);
            int.TryParse(this.minuteText, out var result);
            this.SetMinute(result, false);
            this.TimeModified?.Invoke(this);
        }
    }

    public string SecondText
    {
        get => this.secondText;
        set
        {
            this.SetProperty(ref this.secondText, value);
            int.TryParse(this.secondText, out var result);
            this.SetSecond(result, false);
            this.TimeModified?.Invoke(this);
        }
    }

    public bool IsReadOnly
    {
        get => this.isReadOnly;
        set => this.SetProperty(ref this.isReadOnly, value);
    }

    public bool HideSecondText
    {
        get => this.hideSecondText;
        set => this.SetProperty(ref this.hideSecondText, value);
    }

    #endregion

    public HMSControlState()
    {
    }

    public HMSControlState(int hour, int minute, int second)
    {
        this.SetTime(hour, minute, second);
    }

    public bool IncrementHour()
    {
        if (this.Hour >= 23 && this.Circular)
        {
            this.Hour %= 24;
            return true;
        }
        else if (this.Hour >= 99)
        {
            return false;
        }
        else if (this.Hour >= 0)
        {
            this.Hour++;
            return true;
        }

        return false;
    }

    public bool DecrementHour()
    {
        if (this.Hour == 0)
        {
            if (this.Circular)
            {
                this.Hour = 23;
                return true;
            }
        }
        else if (this.Hour > 0)
        {
            this.Hour--;
            return true;
        }

        return false;
    }

    public void SetTime(int hour, int minute, int second)
    {
        this.Hour = hour;
        this.Minute = minute;
        this.Second = second;
    }

    public void Clear()
    {
        this.Hour = 0;
        this.Minute = 0;
        this.Second = 0;

        this.HourText = string.Empty;
        this.MinuteText = string.Empty;
        this.SecondText = string.Empty;
    }

    public bool IncrementMinute()
    {
        if (this.Minute == 59)
        {
            if (this.IncrementHour())
            {
                this.Minute = 0;
                return true;
            }
        }
        else if (this.Minute >= 0)
        {
            this.Minute++;
            return true;
        }

        return false;
    }

    public bool DecrementMinute()
    {
        if (this.Minute == 0)
        {
            if (this.DecrementHour())
            {
                this.Minute = 59;
                return true;
            }
        }
        else if (this.Minute > 0)
        {
            this.Minute--;
            return true;
        }

        return false;
    }

    public bool IncrementSecond()
    {
        if (this.Second == 59)
        {
            if (this.IncrementMinute())
            {
                this.Second = 0;
                return true;
            }
        }
        else if (this.Second >= 0)
        {
            this.Second++;
            return true;
        }

        return false;
    }

    public bool DecrementSecond()
    {
        if (this.Second == 0)
        {
            if (this.DecrementMinute())
            {
                this.Second = 59;
                return true;
            }
        }
        else if (this.Second > 0)
        {
            this.Second--;
            return true;
        }

        return false;
    }

    public void UpDown(int index, bool updown)
    {// index 0:hour, 1:minute, 2:second  updown true:up, false:down
        if (!this.IsValid)
        {
            return;
        }

        if (updown)
        {
            if (index == 0)
            {// Hour
                this.IncrementHour();
            }
            else if (index == 1)
            {// Minute
                this.IncrementMinute();
            }
            else if (index == 2)
            {
                this.IncrementSecond();
            }
        }
        else
        {
            if (index == 0)
            {// Hour
                this.DecrementHour();
            }
            else if (index == 1)
            {// Minute
                this.DecrementMinute();
            }
            else if (index == 2)
            {
                this.DecrementSecond();
            }
        }
    }

    private void SetHour(int value, bool updateText)
    {
        if (this.hour == value)
        {
            return;
        }

        if (value < 0)
        {
            value = -1;
        }
        else if (value >= 24 && this.Circular)
        {
            value = -1;
        }

        this.SetProperty(ref this.hour, value);

        if (updateText)
        {
            this.HourText = value.ToString();
        }
    }

    private void SetMinute(int value, bool updateText)
    {
        if (this.minute == value)
        {
            return;
        }

        if (value < 0 || value >= 60)
        {
            value = -1;
        }

        this.SetProperty(ref this.minute, value);

        if (updateText)
        {
            this.MinuteText = value.ToString();
        }
    }

    private void SetSecond(int value, bool updateText)
    {
        if (this.second == value)
        {
            return;
        }

        if (value < 0 || value >= 60)
        {
            value = -1;
        }

        this.SetProperty(ref this.second, value);

        if (updateText)
        {
            this.SecondText = value.ToString();
        }
    }
}
