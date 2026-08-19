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
/// Base class for trainers which produce a <see cref="IMaxentModel"/> from a stream of events.
/// </summary>
public abstract class AbstractEventTrainer : AbstractTrainer, IEventTrainer
{
    public const string DATA_INDEXER_PARAM = "DataIndexer";
    public const string DATA_INDEXER_ONE_PASS_VALUE = "OnePass";
    public const string DATA_INDEXER_TWO_PASS_VALUE = "TwoPass";
    public const string DATA_INDEXER_ONE_PASS_REAL_VALUE = "OnePassRealValue";

    protected AbstractEventTrainer()
    {
    }

    protected AbstractEventTrainer(TrainingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    /// Whether the data indexer should sort and merge the indexed events.
    /// </summary>
    public abstract bool IsSortAndMerge { get; }

    /// <summary>
    /// Creates and runs a data indexer over the given event stream.
    /// </summary>
    public virtual IDataIndexer GetDataIndexer(IObjectStream<Event?> events)
    {
        trainingParameters.Put(AbstractDataIndexer.SORT_PARAM, IsSortAndMerge);
        // If the cutoff was set, don't overwrite the value.
        if (trainingParameters.GetIntParameter(CUTOFF_PARAM, -1) == -1)
        {
            trainingParameters.Put(CUTOFF_PARAM, 5);
        }

        IDataIndexer indexer = DataIndexerFactory.GetDataIndexer(trainingParameters, reportMap);
        indexer.Index(events);
        return indexer;
    }

    /// <summary>
    /// Trains a model from the given, already indexed, training data.
    /// </summary>
    public abstract IMaxentModel DoTrain(IDataIndexer indexer);

    public IMaxentModel Train(IDataIndexer indexer)
    {
        Validate();

        if (indexer.OutcomeLabels.Length <= 1)
        {
            throw new InsufficientTrainingDataException("Training data must contain more than one outcome");
        }

        IMaxentModel model = DoTrain(indexer);
        AddToReport(TRAINER_TYPE_PARAM, EventTrainer.EVENT_VALUE);
        return model;
    }

    public IMaxentModel Train(IObjectStream<Event?> events)
    {
        Validate();

        HashSumEventStream hses = new(events);
        IDataIndexer indexer = GetDataIndexer(hses);

        // NOpenNLP: upstream is BigInteger.toString(16), which renders the
        // magnitude in lowercase hex with no leading zeros. "x" would pad to a
        // whole byte and keep a leading zero, so the padding is trimmed.
        AddToReport("Training-Eventhash", hses.CalculateHashSum().ToString("x").TrimStart('0'));
        return Train(indexer);
    }
}
