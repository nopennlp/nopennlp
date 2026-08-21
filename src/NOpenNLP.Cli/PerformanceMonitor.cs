/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// This file has been modified from the original Apache OpenNLP source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// The <see cref="PerformanceMonitor"/> measures increments to a counter.
/// During the computation it prints out current and average throughput
/// per second. After the computation is done it prints a final performance
/// report.
/// <para/>
/// <b>Note:</b>
/// This class is not thread safe. <br/>
/// Do not use this class, internal use only!
/// </summary>
public class PerformanceMonitor : IDisposable
{
    private readonly string unit;

    private readonly TextWriter @out;

    // NOpenNLP: upstream schedules the throughput line on a daemon thread through a
    // ScheduledExecutorService. A System.Threading.Timer callback runs on the thread
    // pool, whose threads are background threads, so it does not hold the process open
    // either.
    private Timer? beeperHandle;

    private long startTime = -1;

    private int counter;

    public PerformanceMonitor(TextWriter @out, string unit)
    {
        this.@out = @out;
        this.unit = unit;
    }

    // NOpenNLP: upstream's one-argument constructor defaults to System.out. The
    // evaluator tools rely on that -- their throughput goes to stdout while the basic
    // tools pass System.err explicitly -- so the default is kept.
    public PerformanceMonitor(string unit)
        : this(Console.Out, unit)
    {
    }

    public bool IsStarted => startTime != -1;

    public void IncrementCounter(int increment)
    {
        if (!IsStarted)
            throw new InvalidOperationException("Must be started first!");

        if (increment < 0)
            throw new ArgumentException(
                "increment must be zero or positive but was " + increment + "!", nameof(increment));

        counter += increment;
    }

    public void IncrementCounter() => IncrementCounter(1);

    public void Start()
    {
        if (IsStarted)
            throw new InvalidOperationException("Already started!");

        startTime = CurrentTimeMillis();
    }

    public void StartAndPrintThroughput()
    {
        Start();

        long lastTimeStamp = startTime;
        int lastCount = counter;

        void Beeper(object? state)
        {
            int deltaCount = counter - lastCount;

            long timePassedSinceLastCount = CurrentTimeMillis() - lastTimeStamp;

            double currentThroughput = timePassedSinceLastCount > 0
                ? deltaCount / ((double)timePassedSinceLastCount / 1000)
                : 0;

            long totalTimePassed = CurrentTimeMillis() - startTime;

            double averageThroughput = totalTimePassed > 0
                ? counter / ((double)totalTimePassed / 1000)
                : 0;

            @out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "current: {0:F1} " + unit + "/s avg: {1:F1} " + unit + "/s total: {2} " + unit,
                currentThroughput, averageThroughput, counter));

            lastTimeStamp = CurrentTimeMillis();
            lastCount = counter;
        }

        beeperHandle = new Timer(Beeper, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void StopAndPrintFinalResult()
    {
        if (!IsStarted)
            throw new InvalidOperationException("Must be started first!");

        beeperHandle?.Dispose();
        beeperHandle = null;

        long timePassed = CurrentTimeMillis() - startTime;

        double average = timePassed > 0 ? counter / (timePassed / 1000d) : 0;

        @out.WriteLine();
        @out.WriteLine();

        // NOpenNLP: upstream's "%.1f %n" leaves a trailing space before the newline;
        // it is reproduced so the output matches byte for byte.
        @out.WriteLine(string.Format(CultureInfo.InvariantCulture,
            "Average: {0:F1} " + unit + "/s ", average));
        @out.WriteLine("Total: " + counter + " " + unit);
        // NOpenNLP: upstream concatenates a double, and Java's Double.toString always
        // renders a decimal point -- "1.0s", "60.0s". .NET's default formatting drops
        // it, giving "1s" and "60s". J2N.Numerics.Double.ToString with the "J" format
        // reproduces Java's rendering, as it does elsewhere in the port.
        @out.WriteLine("Runtime: "
            + J2N.Numerics.Double.ToString(timePassed / 1000d, "J", CultureInfo.InvariantCulture)
            + "s");
    }

    // NOpenNLP: stands in for System.currentTimeMillis(), which the throughput maths
    // uses as a wall clock rather than as a duration.
    private static long CurrentTimeMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public void Dispose()
    {
        beeperHandle?.Dispose();
        beeperHandle = null;
        GC.SuppressFinalize(this);
    }
}
