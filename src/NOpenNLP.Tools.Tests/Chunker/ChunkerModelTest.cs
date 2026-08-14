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
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Chunker;

/// <summary>
/// This is the test class for <see cref="ChunkerModel"/>.
/// </summary>
public class ChunkerModelTest
{
    [Test]
    public void TestInvalidFactorySignature()
    {
        ChunkerModel model = null;
        try
        {
            model = new ChunkerModel(
                TestResources.OpenResource("/opennlp/tools/chunker/chunker170custom.bin"));
        }
        // NOpenNLP: upstream catches IllegalArgumentException; ArgumentException
        // is the .NET counterpart, thrown by BaseModel when the tool factory
        // named in the manifest cannot be initialized. Upstream asserts on
        // getMessage(); the port wraps the InvalidFormatException that carries the
        // detail, so the assertions run against the inner exception's message.
        catch (ArgumentException e)
        {
            string message = e.InnerException?.Message ?? e.Message;

            ClassicAssert.IsTrue(message.Contains("ChunkerFactory"),
                "Exception must state ChunkerFactory");
            ClassicAssert.IsTrue(message.Contains("opennlp.tools.chunker.DummyChunkerFactory"),
                "Exception must mention DummyChunkerFactory");
        }

        ClassicAssert.IsNull(model);
    }

    [Test]
    public void Test170DefaultFactory()
    {
        ClassicAssert.IsNotNull(
            new ChunkerModel(TestResources.OpenResource("/opennlp/tools/chunker/chunker170default.bin")));
    }

    [Test]
    public void Test180CustomFactory()
    {
        ClassicAssert.IsNotNull(
            new ChunkerModel(TestResources.OpenResource("/opennlp/tools/chunker/chunker180custom.bin")));
    }
}
