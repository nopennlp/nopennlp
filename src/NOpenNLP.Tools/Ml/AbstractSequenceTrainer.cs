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

using NOpenNLP.Tools.Ml.Model;

namespace NOpenNLP.Tools.Ml;

/// <summary>
/// Base class for trainers which produce a <see cref="ISequenceClassificationModel{T}"/>
/// from a stream of sequences.
/// </summary>
/// <typeparam name="T">The type of the object which is the source of each sequence.</typeparam>
public abstract class AbstractSequenceTrainer<T> : AbstractTrainer, ISequenceTrainer<T>
{
    protected AbstractSequenceTrainer()
    {
    }

    /// <summary>
    /// Trains a sequence classification model from the given sequence stream.
    /// </summary>
    public abstract ISequenceClassificationModel<string> DoTrain(ISequenceStream<T> events);

    public ISequenceClassificationModel<string> Train(ISequenceStream<T> events)
    {
        Validate();

        ISequenceClassificationModel<string> model = DoTrain(events);
        AddToReport(TRAINER_TYPE_PARAM, SequenceTrainer.SEQUENCE_VALUE);
        return model;
    }
}
