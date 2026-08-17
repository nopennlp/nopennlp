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

namespace NOpenNLP.Tools.Parser;

/// <summary>
/// Interface for labeling nodes which contain traces so that these traces can be predicted
/// by the parser.
/// </summary>
public interface IGapLabeler
{
    /// <summary>
    /// Labels the constituents found in the stack with gap labels if appropriate.
    /// </summary>
    /// <param name="stack">The stack of un-completed constituents.</param>
    // NOpenNLP: upstream types this as java.util.Stack, which extends Vector and is
    // therefore index-addressable. Implementations read it positionally via
    // stack.get(stack.size() - n), which .NET's Stack<T> cannot do, and .NET's
    // Stack<T> also enumerates top-to-bottom rather than bottom-to-top. IList<T>
    // preserves the indexing and ordering the ported implementations rely on.
    void LabelGaps(IList<Constituent> stack);
}
