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
/// This exception indicates that a resource violates the expected data format.
/// </summary>
// NOpenNLP: upstream extends IOException, and callers rely on it -- the command line
// tools and the format factories catch IOException to turn a malformed corpus into a
// message and an exit code. Deriving from Exception instead made every one of those
// `catch (IOException)` blocks dead code, so a malformed file escaped as an unhandled
// exception with a stack trace instead of upstream's diagnostic.
public class InvalidFormatException : IOException
{
    public InvalidFormatException()
    {
    }

    public InvalidFormatException(string message) : base(message)
    {
    }

    public InvalidFormatException(Exception t) : base(null, t)
    {
    }

    public InvalidFormatException(string message, Exception t) : base(message, t)
    {
    }
}
