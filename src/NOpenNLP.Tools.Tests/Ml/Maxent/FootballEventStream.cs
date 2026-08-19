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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Ml.Maxent;

public class FootballEventStream : IObjectStream<Event?>
{
    // NOpenNLP: upstream reads the file through a URLInputStreamFactory over a
    // classpath URL. The .NET counterpart is the embedded resource, so that
    // factory class is replaced by the nested one below rather than ported.
    private readonly IObjectStream<string?> textStream = new PlainTextByLineStream(
        new ResourceInputStreamFactory("/opennlp/tools/ml/maxent/football.dat"),
        Encoding.ASCII);

    public Event? Read()
    {
        string? line = textStream.Read();
        if (line == null)
        {
            return null;
        }

        string[] tokens = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);

        string[] context = new string[tokens.Length - 1];
        Array.Copy(tokens, context, tokens.Length - 1);
        return new Event(tokens[^1], context);
    }

    public void Reset() => textStream.Reset();

    public void Dispose() => textStream.Dispose();

    private sealed class ResourceInputStreamFactory(string path) : IInputStreamFactory
    {
        public Stream CreateInputStream() => TestResources.OpenResource(path);
    }
}
