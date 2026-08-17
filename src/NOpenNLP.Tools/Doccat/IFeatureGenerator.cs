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

namespace NOpenNLP.Tools.Doccat;

/// <summary>
/// Interface for generating features for document categorization.
/// </summary>
public interface IFeatureGenerator
{
    /// <summary>
    /// Extract features from given text fragments
    /// </summary>
    /// <param name="text">the text fragments to extract features from</param>
    /// <param name="extraInformation">optional extra information to be used by the feature generator</param>
    /// <returns>a collection of features</returns>
    ICollection<string> ExtractFeatures(string[] text, IDictionary<string, object> extraInformation);
}
