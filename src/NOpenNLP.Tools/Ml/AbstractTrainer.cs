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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml;

/// <summary>
/// Base class for trainers, holding the training parameters and the report map.
/// </summary>
public abstract class AbstractTrainer
{
    public const string ALGORITHM_PARAM = "Algorithm";

    public const string TRAINER_TYPE_PARAM = "TrainerType";

    public const string CUTOFF_PARAM = "Cutoff";
    public const int CUTOFF_DEFAULT = 5;

    public const string ITERATIONS_PARAM = "Iterations";
    public const int ITERATIONS_DEFAULT = 100;

    public const string VERBOSE_PARAM = "PrintMessages";
    public const bool VERBOSE_DEFAULT = true;

    protected TrainingParameters trainingParameters = null!;
    protected IDictionary<string, string> reportMap = null!;

    protected bool printMessages;

    protected AbstractTrainer()
    {
    }

    protected AbstractTrainer(TrainingParameters parameters)
    {
        Init(parameters, new Dictionary<string, string>());
    }

    /// <summary>
    /// Initializes this trainer with the given parameters and report map.
    /// </summary>
    public virtual void Init(TrainingParameters trainingParameters, IDictionary<string, string>? reportMap)
    {
        this.trainingParameters = trainingParameters;
        this.reportMap = reportMap ?? new Dictionary<string, string>();
        printMessages = trainingParameters.GetBooleanParameter(VERBOSE_PARAM, VERBOSE_DEFAULT);
    }

    /// <summary>
    /// Gets the configured algorithm name, defaulting to maxent.
    /// </summary>
    public virtual string Algorithm =>
        trainingParameters.GetStringParameter(ALGORITHM_PARAM, GISTrainer.MAXENT_VALUE)!;

    /// <summary>
    /// Gets the configured cutoff.
    /// </summary>
    public virtual int Cutoff => trainingParameters.GetIntParameter(CUTOFF_PARAM, CUTOFF_DEFAULT);

    /// <summary>
    /// Gets the configured number of iterations.
    /// </summary>
    public virtual int Iterations => trainingParameters.GetIntParameter(ITERATIONS_PARAM, ITERATIONS_DEFAULT);

    /// <summary>
    /// Checks the parameters. If a subclass overrides this, it should call the base implementation.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if a parameter is not valid.</exception>
    public virtual void Validate()
    {
        try
        {
            trainingParameters.GetIntParameter(CUTOFF_PARAM, CUTOFF_DEFAULT);
            trainingParameters.GetIntParameter(ITERATIONS_PARAM, ITERATIONS_DEFAULT);
        }
        catch (FormatException e)
        {
            throw new ArgumentException(e.Message, e);
        }
    }

    /// <summary>
    /// Adds the key/value pair to the report map.
    /// </summary>
    protected virtual void AddToReport(string key, string value) => reportMap[key] = value;

    protected virtual void Display(string s)
    {
        if (printMessages)
        {
            Console.Write(s);
        }
    }
}
