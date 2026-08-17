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

namespace NOpenNLP.Tools.Util.Model;

// NOpenNLP: the upstream SCREAMING_CASE member names are retained rather than
// renamed to .NET casing. Callers pass ModelType.MAXENT.name()/toString() straight
// through as the TrainingParameters ALGORITHM_PARAM value, which TrainerFactory
// then matches against the algorithm names baked into model manifests, so the
// member names are load-bearing strings rather than an internal enumeration.
public enum ModelType
{
    MAXENT,
    PERCEPTRON,
    PERCEPTRON_SEQUENCE
}
