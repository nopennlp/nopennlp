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

using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// Interface for streams of sequences used to train sequence models.
/// </summary>
/// <typeparam name="T">The type of the object which is the source of each sequence.</typeparam>
// NOpenNLP: upstream declares `SequenceStream extends ObjectStream<Sequence>`,
// using the raw type, which forces implementors of updateContext to cast the
// argument back to Sequence<TheirSampleType> under @SuppressWarnings("unchecked").
// Making the interface generic keeps that cast out of the port and is otherwise
// equivalent.
public interface ISequenceStream<T> : IObjectStream<Sequence<T>?>
{
    /// <summary>
    /// Creates a new event array based on the outcomes predicted by the specified
    /// parameters for the specified sequence.
    /// </summary>
    /// <param name="sequence">The sequence to be evaluated.</param>
    /// <param name="model">The model to use for the evaluation.</param>
    /// <returns>event array</returns>
    Event[] UpdateContext(Sequence<T> sequence, AbstractModel model);
}
