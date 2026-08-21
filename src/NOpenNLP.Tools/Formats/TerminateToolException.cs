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

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// Exception to terminate the execution of a command line tool.
/// <para/>
/// The exception should be thrown to indicate that the VM should be terminated with
/// the specified error code, instead of just calling <c>Environment.Exit</c> somewhere
/// in the code.
/// <para/>
/// The return code convention is the following:<br/>
/// <list type="bullet">
/// <item><description><c>0</c> in case of graceful termination</description></item>
/// <item><description><c>-1</c> in case of runtime errors, such as IO errors</description></item>
/// <item><description><c>1</c> in case of invalid parameters</description></item>
/// </list>
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
// NOpenNLP: upstream is opennlp.tools.cmdline.TerminateToolException. It lives here
// rather than in the CLI project because the format factories throw it, and the
// factories stay in the library beside the readers they wrap. Upstream can keep it in
// cmdline only because its formats module depends on cmdline; this port keeps the
// dependency pointing the other way.
public class TerminateToolException : Exception
{
    // NOpenNLP: upstream stores its own message field and overrides getMessage() to
    // return it, so the base message is always null. Passing it to the base
    // constructor gives the same observable message while keeping ToString() and
    // debugger display useful.
    public TerminateToolException(int code, string? message, Exception? t)
        : base(message, t)
    {
        Code = code;
    }

    public TerminateToolException(int code, string? message)
        : base(message)
    {
        Code = code;
    }

    public TerminateToolException(int code)
        : this(code, null)
    {
    }

    /// <summary>
    /// The code the process should terminate with.
    /// </summary>
    public int Code { get; }
}
