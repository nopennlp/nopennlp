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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// The interface for part of speech taggers.
/// </summary>
public interface IPOSTagger
{
    /// <summary>
    /// Assigns the sentence of tokens pos tags.
    /// </summary>
    /// <param name="sentence">The sentece of tokens to be tagged.</param>
    /// <returns>an array of pos tags for each token provided in sentence.</returns>
    string[] Tag(string[] sentence);

    string[] Tag(string[] sentence, object[] additionalContext);

    Sequence[] TopKSequences(string[] sentence);

    Sequence[] TopKSequences(string[] sentence, object[] additionaContext);
}
