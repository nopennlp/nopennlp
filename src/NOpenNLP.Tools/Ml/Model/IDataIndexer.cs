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

using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// Object which compresses events in memory and performs feature selection.
/// </summary>
public interface IDataIndexer
{
    /// <summary>
    /// Gets the array of predicates seen in each event: a 2-D array whose first
    /// dimension is the event index and array this refers to contains the contexts
    /// for that event.
    /// </summary>
    int[][] Contexts { get; }

    /// <summary>
    /// Gets an array indexed by the event index indicating the number of times a
    /// particular event was seen.
    /// </summary>
    int[] NumTimesEventsSeen { get; }

    /// <summary>
    /// Gets an array indicating the outcome index for each event.
    /// </summary>
    int[] OutcomeList { get; }

    /// <summary>
    /// Gets an array of predicate/context names indexed by context index. These
    /// indices are the value of the array returned by <see cref="Contexts"/>.
    /// </summary>
    string[] PredLabels { get; }

    /// <summary>
    /// Gets an array of the count of each predicate in the events.
    /// </summary>
    int[] PredCounts { get; }

    /// <summary>
    /// Gets an array of outcome names indexed by outcome index.
    /// </summary>
    string[] OutcomeLabels { get; }

    /// <summary>
    /// Gets the values associated with each event context, or <c>null</c> if
    /// integer values are to be used.
    /// </summary>
    float[][]? Values { get; }

    /// <summary>
    /// Gets the number of total events indexed.
    /// </summary>
    int NumEvents { get; }

    /// <summary>
    /// Sets parameters used during the data indexing.
    /// </summary>
    /// <param name="trainParams"><see cref="TrainingParameters"/></param>
    /// <param name="reportMap">the map to which report entries are written.</param>
    void Init(TrainingParameters trainParams, IDictionary<string, string>? reportMap);

    /// <summary>
    /// Performs the data indexing. Make sure the <see cref="Init"/> method is called first.
    /// </summary>
    /// <param name="eventStream">a stream of events</param>
    /// <exception cref="IOException">if there is an error during reading</exception>
    void Index(IObjectStream<Event> eventStream);
}
