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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using System;
using System.Collections.Generic;
using J2N.Collections.Generic.Extensions;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// The <see cref="AggregatedFeatureGenerator"/> aggregates a set of
/// <see cref="IAdaptiveFeatureGenerator"/>s and calls them to generate the features.
/// </summary>
public class AggregatedFeatureGenerator : IAdaptiveFeatureGenerator
{
    /// <summary>
    /// Contains all aggregated <see cref="IAdaptiveFeatureGenerator"/>s.
    /// </summary>
    private readonly ICollection<IAdaptiveFeatureGenerator> generators; // NOpenNLP: made readonly

    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="generators">array of generators, null values are not permitted</param>
    public AggregatedFeatureGenerator(params IAdaptiveFeatureGenerator[] generators)
    {
        foreach (IAdaptiveFeatureGenerator generator in generators)
        {
            // NOpenNLP: ArgumentNullException.ThrowIfNull is net6.0+.
            if (generator is null)
            {
                throw new ArgumentNullException(nameof(generator), "null values in generators are not permitted");
            }
        }

        this.generators = new List<IAdaptiveFeatureGenerator>(generators);
        this.generators = this.generators.AsReadOnly();
    }

    public AggregatedFeatureGenerator(ICollection<IAdaptiveFeatureGenerator> generators) : this([.. generators])
    {
    }

    /// <summary>
    /// Calls the <see cref="IAdaptiveFeatureGenerator.ClearAdaptiveData()"/> method
    /// on all aggregated <see cref="IAdaptiveFeatureGenerator"/>s.
    /// </summary>
    public virtual void ClearAdaptiveData()
    {
        foreach (IAdaptiveFeatureGenerator generator in generators)
        {
            generator.ClearAdaptiveData();
        }
    }

    /// <summary>
    /// Calls the <see cref="IAdaptiveFeatureGenerator.CreateFeatures(System.Collections.Generic.IList{string}, string[], int, string[])"/>
    /// method on all aggregated <see cref="IAdaptiveFeatureGenerator"/>s.
    /// </summary>
    public virtual void CreateFeatures(IList<string> features, string[] tokens, int index, string[] previousOutcomes)
    {
        foreach (IAdaptiveFeatureGenerator generator in generators)
        {
            generator.CreateFeatures(features, tokens, index, previousOutcomes);
        }
    }

    /// <summary>
    /// Calls the <see cref="IAdaptiveFeatureGenerator.UpdateAdaptiveData(string[], string[])"/>
    /// method on all aggregated <see cref="IAdaptiveFeatureGenerator"/>s.
    /// </summary>
    public virtual void UpdateAdaptiveData(string[] tokens, string[] outcomes)
    {
        foreach (IAdaptiveFeatureGenerator generator in generators)
        {
            generator.UpdateAdaptiveData(tokens, outcomes);
        }
    }

    /// <summary>
    /// Retrieves a collection of all aggregated
    /// <see cref="IAdaptiveFeatureGenerator"/>s.
    /// </summary>
    /// <returns>all aggregated generators</returns>
    public virtual ICollection<IAdaptiveFeatureGenerator> GetGenerators()
    {
        return generators;
    }
}
