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
using System.IO;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Leipzig;

internal class SampleSkipStream<T> : ObjectStreamBase<T?>
    where T : class
{
    private readonly IObjectStream<T?> samples;
    private readonly int samplesToSkip;

    /// <exception cref="IOException">if there is an error during reading</exception>
    internal SampleSkipStream(IObjectStream<T?> samples, int samplesToSkip)
    {
        this.samples = samples;
        this.samplesToSkip = samplesToSkip;

        SkipSamples();
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override T? Read() => samples.Read();

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    /// <exception cref="NotSupportedException">if reset is not supported on this stream</exception>
    public override void Reset()
    {
        samples.Reset();
        SkipSamples();
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    private void SkipSamples()
    {
        int i = 0;

        while (i < samplesToSkip && samples.Read() != null)
        {
            i++;
        }
    }
}
