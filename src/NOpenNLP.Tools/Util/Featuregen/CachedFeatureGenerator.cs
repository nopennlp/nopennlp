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

using NOpenNLP.Tools.Support;
using System.Collections.Generic;

namespace NOpenNLP.Tools.Util.Featuregen;

/// <summary>
/// Caches features of the aggregated <see cref="IAdaptiveFeatureGenerator"/>s.
/// </summary>
public class CachedFeatureGenerator : IAdaptiveFeatureGenerator
{
    private readonly IAdaptiveFeatureGenerator generator;
    private string[] prevTokens;
    private readonly Cache<int, IList<string>> contextsCache; // NOpenNLP: made readonly
    private long numberOfCacheHits;
    private long numberOfCacheMisses;

    public CachedFeatureGenerator(params IAdaptiveFeatureGenerator[] generators)
    {
        this.generator = new AggregatedFeatureGenerator(generators);
        contextsCache = new Cache<int, IList<string>>(100);
    }

    public CachedFeatureGenerator(IAdaptiveFeatureGenerator generator)
    {
        this.generator = generator;
        contextsCache = new Cache<int, IList<string>>(100);
    }

    public virtual void CreateFeatures(IList<string> features, string[] tokens, int index, string[] previousOutcomes)
    {
        IList<string> cacheFeatures;
        if (tokens == prevTokens)
        {
            cacheFeatures = contextsCache[index];
            if (cacheFeatures != null)
            {
                numberOfCacheHits++;
                features.AddRange(cacheFeatures);
                return;
            }
        }
        else
        {
            contextsCache.Clear();
            prevTokens = tokens;
        }

        cacheFeatures = new List<string>();
        numberOfCacheMisses++;
        generator.CreateFeatures(cacheFeatures, tokens, index, previousOutcomes);
        contextsCache.Put(index, cacheFeatures);
        features.AddRange(cacheFeatures);
    }

    public virtual void UpdateAdaptiveData(string[] tokens, string[] outcomes)
    {
        generator.UpdateAdaptiveData(tokens, outcomes);
    }

    public virtual void ClearAdaptiveData()
    {
        generator.ClearAdaptiveData();
    }

    /// <summary>
    /// Retrieves the number of times a cache hit occurred.
    /// </summary>
    /// <returns>number of cache hits</returns>
    public virtual long GetNumberOfCacheHits()
    {
        return numberOfCacheHits;
    }

    /// <summary>
    /// Retrieves the number of times a cache miss occurred.
    /// </summary>
    /// <returns>number of cache misses</returns>
    public virtual long GetNumberOfCacheMisses()
    {
        return numberOfCacheMisses;
    }

    public override string ToString()
    {
        return base.ToString() + ": hits=" + numberOfCacheHits + " misses=" + numberOfCacheMisses + " hit%" + (numberOfCacheHits > 0 ? (double)numberOfCacheHits / (numberOfCacheMisses + numberOfCacheHits) : 0);
    }

    public virtual IAdaptiveFeatureGenerator GetCachedFeatureGenerator()
    {
        return generator;
    }
}
