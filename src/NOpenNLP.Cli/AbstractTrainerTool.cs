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

using System.IO;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Base class for trainer tools.
/// </summary>
public abstract class AbstractTrainerTool<T> : AbstractEvaluatorTool<T>
{
    /// <summary>
    /// The training parameters read from <c>-params</c>, or <c>null</c> when it was
    /// omitted.
    /// </summary>
    protected TrainingParameters? mlParams;

    protected TerminateToolException CreateTerminationIOException(IOException e)
    {
        if (e is InsufficientTrainingDataException)
        {
            return new TerminateToolException(-1, "\n\nERROR: Not enough training data\n" +
                "The provided training data is not sufficient to create enough events to train a model.\n" +
                "To resolve this error use more training data, if this doesn't help there might\n" +
                "be some fundamental problem with the training data itself.");
        }

        return new TerminateToolException(-1,
            "IO error while reading training data or indexing data: " + e.Message, e);
    }
}

/// <summary>
/// Base class for cross validator tools.
/// </summary>
// NOpenNLP: upstream's AbstractCrossValidatorTool adds nothing to AbstractTrainerTool
// either; it is kept so the ported tools extend the same class they do upstream.
public abstract class AbstractCrossValidatorTool<T> : AbstractTrainerTool<T>
{
}
