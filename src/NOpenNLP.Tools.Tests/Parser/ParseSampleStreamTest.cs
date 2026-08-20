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

using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Parser;

public class ParseSampleStreamTest
{
    private static IObjectStream<Parse?> CreateParseSampleStream()
    {
        // NOpenNLP: upstream uses opennlp.tools.formats.ResourceAsStreamFactory,
        // which is not ported; the ResourceAsStreamFactory in Support does the
        // same job over an embedded resource.
        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/parser/test.parse");

        return new ParseSampleStream(new PlainTextByLineStream(@in, Encoding.UTF8));
    }

    [Test]
    public void TestReadTestStream()
    {
        IObjectStream<Parse?> parseStream = CreateParseSampleStream();
        ClassicAssert.NotNull(parseStream.Read());
        ClassicAssert.NotNull(parseStream.Read());
        ClassicAssert.NotNull(parseStream.Read());
        ClassicAssert.NotNull(parseStream.Read());
        ClassicAssert.IsNull(parseStream.Read());
    }
}
