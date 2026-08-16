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

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Reads a plain text file and return each line as a <see cref="string"/> object.
/// </summary>
public class PlainTextByLineStream : ObjectStreamBase<string?>
{
    private readonly Encoding encoding;

    private readonly IInputStreamFactory inputStreamFactory; // NOpenNLP: made readonly

    private TextReader? @in;

    public PlainTextByLineStream(IInputStreamFactory inputStreamFactory, string charsetName)
        : this(inputStreamFactory, Encoding.GetEncoding(charsetName))
    {
    }

    public PlainTextByLineStream(IInputStreamFactory inputStreamFactory, Encoding charset)
    {
        this.inputStreamFactory = inputStreamFactory
            ?? throw new ArgumentNullException(nameof(inputStreamFactory), "inputStreamFactory must not be null!");
        encoding = charset;

        Reset();
    }

    public override string? Read() => @in!.ReadLine();

    public override void Reset()
    {
        @in?.Dispose();

        @in = new StreamReader(inputStreamFactory.CreateInputStream(), encoding);
    }

    protected override void Dispose(bool disposing)
    {
        @in?.Dispose();
    }
}
