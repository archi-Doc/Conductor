// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable SA1512 // Single-line comments should not be followed by blank line
#pragma warning disable SA1515 // Single-line comment should be preceded by blank line

namespace Conductor
{
    public class ConductorCpu
    {
        public const int MaxSeconds = 60; // Maximum number of seconds to measure cpu usage.
        public const int ProcessInterval = 3; // Interval
        public const string CategoryName = "Processor"; // "Processor Information"
        public const string CounterName = "% Processor Time";
        public const string TotalName = "_Total";

        public ConductorCpu()
        {
            Task.Run(() => this.Initialize());
        }

        public bool Initialized { get; private set; } = false;

        // public PerformanceCounter? PerformanceCounter { get; private set; }

        public int ProcessIntervalCount { get; private set; }

        public CoreUsage? TotalUsage { get; private set; }

        public CoreUsage[]? PerCoreUsage { get; private set; }

        public void ProcessEverySecond()
        {
            if (!this.Initialized)
            {
                return;
            }

            if (++this.ProcessIntervalCount < ProcessInterval)
            {
                return;
            }

            this.ProcessIntervalCount = 0;

            this.TotalUsage!.Process();
            foreach (var x in this.PerCoreUsage!)
            {
                x.Process();
            }
        }

        public double GetMaxAverage()
        {
            if (!this.Initialized)
            {
                return 0;
            }

            var max = this.PerCoreUsage!.Max(x => x.GetAverage());
            return max;
        }

        private void Initialize()
        {
            /* var machine = ".";
            if (!PerformanceCounterCategory.Exists(category, machine))
            {
                return;
            }

            if (!PerformanceCounterCategory.CounterExists(counter, category, machine))
            {
                return;
            }*/

            // this.PerformanceCounter = new PerformanceCounter(category, counter);

            var cat = new PerformanceCounterCategory(CategoryName);
            var instances = cat.GetInstanceNames();
            var number = instances.Where(x => x != TotalName).Count();

            this.TotalUsage = new CoreUsage(this, TotalName, MaxSeconds / ProcessInterval);
            this.PerCoreUsage = new CoreUsage[number];
            var n = 0;
            foreach (var x in instances)
            {
                if (x != "_Total")
                {
                    this.PerCoreUsage[n++] = new CoreUsage(this, x, MaxSeconds / ProcessInterval);
                }
            }

            this.Initialized = true;
        }

        public class CoreUsage
        {
            private object cs = new object();
            private PerformanceCounter performanceCounter;
            private ConductorCpu conductorCpu;
            private int maxNumber;
            private Queue<float> valueQueue = new Queue<float>();
            private double accumulatedValue;

            public CoreUsage(ConductorCpu conductorCpu, string instanceName, int maxNumber)
            {
                this.conductorCpu = conductorCpu;
                this.performanceCounter = new PerformanceCounter(CategoryName, CounterName, instanceName);
                this.maxNumber = maxNumber;
                this.InstanceName = instanceName;
            }

            public string InstanceName { get; }

            public void Process()
            {
                lock (this.cs)
                {
                    var value = this.performanceCounter.NextValue();

                    this.accumulatedValue += value;
                    this.valueQueue.Enqueue(value);

                    while (this.valueQueue.Count > this.maxNumber)
                    {
                        this.accumulatedValue -= this.valueQueue.Dequeue();
                    }
                }
            }

            public double GetAverage()
            {
                lock (this.cs)
                {
                    if (this.valueQueue.Count == 0)
                    {
                        return 0;
                    }

                    return this.accumulatedValue / this.valueQueue.Count;
                    // return this.valueQueue.Average();
                }
            }
        }
    }
}
