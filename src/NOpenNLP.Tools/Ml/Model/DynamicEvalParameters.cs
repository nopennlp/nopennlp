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
using System.Linq;

namespace NOpenNLP.Tools.Ml.Model;

public class DynamicEvalParameters
{
    /// <summary>
    /// Mapping between outcomes and parameter values for each context.
    /// The integer representation of the context can be found using <c>pmap</c>.
    /// </summary>
    private readonly IList<Context> @params; // NOpenNLP: made readonly

    /// <summary>
    /// The number of outcomes being predicted.
    /// </summary>
    private readonly int numOutcomes;

    /// <summary>
    /// Creates a set of parameters which can be evaluated with the eval method.
    /// </summary>
    /// <param name="params">The parameters of the model.</param>
    /// <param name="numOutcomes">The number of outcomes.</param>
    // NOpenNLP: upstream is List<? extends Context>; IList<T> is invariant in C#,
    // so the parameter takes IList<Context> and callers with a list of a derived
    // type such as MutableContext convert it. IReadOnlyList<out T> would be
    // covariant but is not what upstream's field semantics need.
    public DynamicEvalParameters(IList<Context> @params, int numOutcomes)
    {
        this.@params = @params;
        this.numOutcomes = numOutcomes;
    }

    public virtual Context[] Params => @params.ToArray();

    public virtual int NumOutcomes => numOutcomes;
}
