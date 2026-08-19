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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml.Maxent;

/// <summary>
/// An event stream which reads real-valued events from a stream of strings, each
/// holding contextual predicates followed by the outcome.
/// </summary>
public class RealBasicEventStream(IObjectStream<string?> ds) : IObjectStream<Event?>
{
    public virtual Event? Read()
    {
        string? eventString = ds.Read();

        return eventString != null ? CreateEvent(eventString) : null;
    }

    private static Event? CreateEvent(string obs) // NOpenNLP: made static
    {
        int lastSpace = obs.LastIndexOf(' ');
        if (lastSpace == -1)
        {
            return null;
        }

        // NOpenNLP: upstream splits on the regex "\s+". RemoveEmptyEntries matches it
        // for the runs between tokens; it additionally drops the leading empty string
        // that Java produces when the text starts with whitespace, which no caller
        // relies on since a leading empty is not a contextual predicate.
        string[] contexts = obs[..lastSpace].Split([' ', '\t', '\n', '\r', '\f', '\v'],
            StringSplitOptions.RemoveEmptyEntries);
        float[]? values = RealValueFileEventStream.ParseContexts(contexts);
        return new Event(obs[(lastSpace + 1)..], contexts, values);
    }

    public virtual void Reset() => ds.Reset();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ds.Dispose();
        }
    }
}
