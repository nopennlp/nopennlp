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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml;

/// <summary>
/// A trainer which produces a <see cref="IMaxentModel"/> from a stream of events.
/// </summary>
public interface IEventTrainer
{
    /// <summary>
    /// Initializes this trainer with the given parameters.
    /// </summary>
    void Init(TrainingParameters trainingParams, IDictionary<string, string>? reportMap);

    /// <summary>
    /// Trains a model from the given event stream.
    /// </summary>
    IMaxentModel Train(IObjectStream<Event?> events);

    /// <summary>
    /// Trains a model from the given, already indexed, training data.
    /// </summary>
    IMaxentModel Train(IDataIndexer indexer);
}

/// <summary>
/// Constants belonging to <see cref="IEventTrainer"/>.
/// </summary>
// NOpenNLP: upstream declares EVENT_VALUE on the EventTrainer interface itself.
// netstandard2.0 has no default interface members, so the constant lives on a
// companion class instead.
public static class EventTrainer
{
    /// <summary>
    /// The trainer type value used to report an event model trainer.
    /// </summary>
    public const string EVENT_VALUE = "Event";
}
