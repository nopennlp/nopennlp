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

using System;
using System.IO;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// This exception indicates that the provided training data is
/// insufficient to train the desired model.
/// </summary>
public class InsufficientTrainingDataException : IOException
{
    public InsufficientTrainingDataException()
    {
    }

    public InsufficientTrainingDataException(string message)
        : base(message)
    {
    }

    // NOpenNLP: upstream calls the no-arg IOException constructor and then
    // initCause(t); .NET has no initCause, so the cause is passed through the
    // (message, innerException) constructor with a null message instead.
    public InsufficientTrainingDataException(Exception t)
        : base(null, t)
    {
    }

    public InsufficientTrainingDataException(string message, Exception t)
        : base(message, t)
    {
    }
}
