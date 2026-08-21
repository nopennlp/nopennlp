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
using System.Text;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// An <see cref="IInputStreamFactory"/> over standard input, which can only be read once.
/// </summary>
public class SystemInputStreamFactory : IInputStreamFactory
{
    private bool isTainted;

    /// <summary>
    /// The encoding standard input is read with.
    /// </summary>
    // NOpenNLP: upstream returns Charset.defaultCharset(). Console.InputEncoding is the
    // closer counterpart -- it reflects what the terminal is actually handing over,
    // which is what the tools are decoding -- but it throws on a redirected or closed
    // stdin, which is exactly how the tools are normally run, so Encoding.Default is
    // the fallback.
    public static Encoding Encoding
    {
        get
        {
            try
            {
                return Console.InputEncoding;
            }
            catch (Exception e) when (e is IOException or PlatformNotSupportedException)
            {
                return System.Text.Encoding.Default;
            }
        }
    }

    /// <inheritdoc/>
    public Stream CreateInputStream()
    {
        if (!isTainted)
        {
            isTainted = true;
            return Console.OpenStandardInput();
        }
        else
        {
            throw new NotSupportedException(
                "The System.in stream can't be re-created to read from the beginning!");
        }
    }
}
