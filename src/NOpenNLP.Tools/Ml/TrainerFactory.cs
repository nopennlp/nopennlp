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
using System.Collections.Generic;
using NOpenNLP.Tools.Ml.Maxent;
using NOpenNLP.Tools.Ml.Naivebayes;
using NOpenNLP.Tools.Ml.Perceptron;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Ext;

namespace NOpenNLP.Tools.Ml;

/// <summary>
/// Creates trainers for the algorithm named by the training parameters.
/// </summary>
public class TrainerFactory
{
    public enum TrainerType
    {
        EVENT_MODEL_TRAINER,
        EVENT_MODEL_SEQUENCE_TRAINER,
        SEQUENCE_TRAINER
    }

    // built-in trainers
    // NOpenNLP: upstream maps the algorithm name to the trainer Class and reflects over
    // it. The two sequence trainers are generic here (see ISequenceStream), so a Type is
    // not enough to construct one; the map records the trainer type and a factory that
    // closes the generic over the caller's sequence source type instead.
    private static readonly IReadOnlyDictionary<string, BuiltinTrainer> BUILTIN_TRAINERS =
        new Dictionary<string, BuiltinTrainer>
        {
            [GISTrainer.MAXENT_VALUE] =
                new(TrainerType.EVENT_MODEL_TRAINER, _ => new GISTrainer()),
            [PerceptronTrainer.PERCEPTRON_VALUE] =
                new(TrainerType.EVENT_MODEL_TRAINER, _ => new PerceptronTrainer()),
            [NaiveBayesTrainer.NAIVE_BAYES_VALUE] =
                new(TrainerType.EVENT_MODEL_TRAINER, _ => new NaiveBayesTrainer()),
            [SimplePerceptronSequenceTrainer<object>.PERCEPTRON_SEQUENCE_VALUE] =
                new(TrainerType.EVENT_MODEL_SEQUENCE_TRAINER,
                    sequenceType => Activator.CreateInstance(
                        typeof(SimplePerceptronSequenceTrainer<>).MakeGenericType(sequenceType))!),
        };

    /// <summary>
    /// Determines the trainer type based on the algorithm parameter value.
    /// </summary>
    /// <param name="trainParams">The training parameters.</param>
    /// <returns>The trainer type, or <c>null</c> if the type couldn't be determined.</returns>
    public static TrainerType? GetTrainerType(TrainingParameters trainParams)
    {
        string? algorithmValue = trainParams.GetStringParameter(AbstractTrainer.ALGORITHM_PARAM, null);

        // Check if it is defaulting to the MAXENT trainer
        if (algorithmValue == null)
        {
            return TrainerType.EVENT_MODEL_TRAINER;
        }

        if (BUILTIN_TRAINERS.TryGetValue(algorithmValue, out BuiltinTrainer? builtin))
        {
            return builtin.Type;
        }

        // Try to load the different trainers, and return the type on success

        if (CanLoadExtension<IEventTrainer>(algorithmValue))
        {
            return TrainerType.EVENT_MODEL_TRAINER;
        }

        if (CanLoadExtension<IEventModelSequenceTrainer<object>>(algorithmValue))
        {
            return TrainerType.EVENT_MODEL_SEQUENCE_TRAINER;
        }

        if (CanLoadExtension<ISequenceTrainer<object>>(algorithmValue))
        {
            return TrainerType.SEQUENCE_TRAINER;
        }

        return null;
    }

    /// <summary>
    /// Creates a sequence trainer for the configured algorithm.
    /// </summary>
    /// <typeparam name="T">The type of the object which is the source of each sequence.</typeparam>
    public static ISequenceTrainer<T> GetSequenceModelTrainer<T>(TrainingParameters trainParams,
        IDictionary<string, string>? reportMap)
    {
        string trainerType = trainParams.GetStringParameter(AbstractTrainer.ALGORITHM_PARAM, null)
            ?? throw new ArgumentException("Trainer type couldn't be determined!");

        ISequenceTrainer<T> trainer = BUILTIN_TRAINERS.TryGetValue(trainerType, out BuiltinTrainer? builtin)
            ? (ISequenceTrainer<T>)builtin.Create(typeof(T))
            : ExtensionLoader.InstantiateExtension<ISequenceTrainer<T>>(trainerType)
                ?? throw new ExtensionNotLoadedException(
                    "Unable to load the extension: " + trainerType);

        trainer.Init(trainParams, reportMap);
        return trainer;
    }

    /// <summary>
    /// Creates an event model sequence trainer for the configured algorithm.
    /// </summary>
    /// <typeparam name="T">The type of the object which is the source of each sequence.</typeparam>
    public static IEventModelSequenceTrainer<T> GetEventModelSequenceTrainer<T>(
        TrainingParameters trainParams, IDictionary<string, string>? reportMap)
    {
        string trainerType = trainParams.GetStringParameter(AbstractTrainer.ALGORITHM_PARAM, null)
            ?? throw new ArgumentException("Trainer type couldn't be determined!");

        IEventModelSequenceTrainer<T> trainer =
            BUILTIN_TRAINERS.TryGetValue(trainerType, out BuiltinTrainer? builtin)
                ? (IEventModelSequenceTrainer<T>)builtin.Create(typeof(T))
                : ExtensionLoader.InstantiateExtension<IEventModelSequenceTrainer<T>>(trainerType)
                    ?? throw new ExtensionNotLoadedException(
                        "Unable to load the extension: " + trainerType);

        trainer.Init(trainParams, reportMap);
        return trainer;
    }

    /// <summary>
    /// Creates an event trainer for the configured algorithm, defaulting to the GIS trainer.
    /// </summary>
    public static IEventTrainer GetEventTrainer(TrainingParameters trainParams,
        IDictionary<string, string>? reportMap)
    {
        // if the trainerType is not defined -- use the GISTrainer.
        string trainerType = trainParams.GetStringParameter(AbstractTrainer.ALGORITHM_PARAM,
            GISTrainer.MAXENT_VALUE)!;

        IEventTrainer trainer = BUILTIN_TRAINERS.TryGetValue(trainerType, out BuiltinTrainer? builtin)
            ? (IEventTrainer)builtin.Create(typeof(object))
            : ExtensionLoader.InstantiateExtension<IEventTrainer>(trainerType)
                ?? throw new ExtensionNotLoadedException("Unable to load the extension: " + trainerType);

        trainer.Init(trainParams, reportMap);
        return trainer;
    }

    /// <summary>
    /// Determines whether the given training parameters are valid.
    /// </summary>
    public static bool IsValid(TrainingParameters trainParams)
    {
        string? algorithmName = trainParams.GetStringParameter(AbstractTrainer.ALGORITHM_PARAM, null);

        // If a trainer type can be determined, then the trainer is valid!
        if (algorithmName != null
            && !(BUILTIN_TRAINERS.ContainsKey(algorithmName) || GetTrainerType(trainParams) != null))
        {
            return false;
        }

        try
        {
            // require that the Cutoff and the number of iterations be an integer.
            // if they are not set, the default values will be ok.
            trainParams.GetIntParameter(AbstractTrainer.CUTOFF_PARAM, 0);
            trainParams.GetIntParameter(AbstractTrainer.ITERATIONS_PARAM, 0);
        }
        catch (FormatException)
        {
            return false;
        }

        // no reason to require that the dataIndexer be a 1-pass or 2-pass dataindexer.
        trainParams.GetStringParameter(AbstractEventTrainer.DATA_INDEXER_PARAM, null);

        return true;
    }

    private static bool CanLoadExtension<T>(string algorithmValue)
        where T : class
    {
        try
        {
            return ExtensionLoader.InstantiateExtension<T>(algorithmValue) != null;
        }
        catch (ExtensionNotLoadedException)
        {
            return false;
        }
    }

    private sealed class BuiltinTrainer(TrainerType type, Func<Type, object> create)
    {
        public TrainerType Type => type;

        public object Create(Type sequenceSourceType) => create(sequenceSourceType);
    }
}
