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

namespace NOpenNLP.Tools.Ml;

// NOpenNLP: only the parameter-name constants are ported here. The rest of
// AbstractTrainer (init, getAlgorithm, getCutoff, getIterations, validate, and
// the deprecated isValid/getStringParam overloads) belongs to the trainer API,
// which is ported separately; getAlgorithm in particular defaults to
// GISTrainer.MAXENT_VALUE and so needs the GIS trainer to exist. The constants
// are declared in their upstream home because the data indexers reference them.
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
}
