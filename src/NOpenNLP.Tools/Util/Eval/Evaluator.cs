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
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Util.Eval;

/// <summary>
/// The <see cref="Evaluator{T}"/> is an abstract base class for evaluators.
/// <para/>
/// Evaluation results are the arithmetic mean of the
/// scores calculated for each reference sample.
/// </summary>
public abstract class Evaluator<T>
    where T : class
{
    // NOpenNLP: made readonly
    private readonly IList<IEvaluationMonitor<T>> listeners;

    protected Evaluator(params IEvaluationMonitor<T>[]? aListeners)
    {
        if (aListeners != null)
        {
            JCG.List<IEvaluationMonitor<T>> listenersList = new(aListeners.Length);
            foreach (IEvaluationMonitor<T> evaluationMonitor in aListeners)
            {
                if (evaluationMonitor != null)
                {
                    listenersList.Add(evaluationMonitor);
                }
            }

            listeners = listenersList.AsReadOnly();
        }
        else
        {
            listeners = [];
        }
    }

    /// <summary>
    /// Evaluates the given reference sample object.
    /// <para/>
    /// The implementation has to update the score after every invocation.
    /// </summary>
    /// <param name="reference">the reference sample.</param>
    /// <returns>the predicted sample</returns>
    protected abstract T ProcessSample(T reference);

    /// <summary>
    /// Evaluates the given reference object. The default implementation calls
    /// <see cref="ProcessSample"/>.
    /// <para/>
    /// <b>note:</b> this method will be changed to private in the future.
    /// Implementations should override <see cref="ProcessSample"/> instead.
    /// If this method is overridden, the implementation has to update the score
    /// after every invocation.
    /// </summary>
    /// <param name="sample">the sample to be evaluated</param>
    public virtual void EvaluateSample(T sample)
    {
        T predicted = ProcessSample(sample);
        if (listeners.Count > 0)
        {
            if (sample.Equals(predicted))
            {
                foreach (IEvaluationMonitor<T> listener in listeners)
                {
                    listener.CorrectlyClassified(sample, predicted);
                }
            }
            else
            {
                foreach (IEvaluationMonitor<T> listener in listeners)
                {
                    listener.Missclassified(sample, predicted);
                }
            }
        }
    }

    /// <summary>
    /// Reads all sample objects from the stream
    /// and evaluates each sample object with
    /// <see cref="EvaluateSample"/> method.
    /// </summary>
    /// <param name="samples">the stream of reference which should be evaluated.</param>
    /// <exception cref="IOException">IOException</exception>
    public void Evaluate(IObjectStream<T?> samples)
    {
        T? sample;
        while ((sample = samples.Read()) != null)
        {
            EvaluateSample(sample);
        }
    }
}
