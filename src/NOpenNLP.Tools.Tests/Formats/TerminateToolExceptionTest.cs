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


using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats;

/// <summary>
/// Tests for the <see cref="TerminateToolException"/> class.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream keeps this test in opennlp.tools.cmdline, beside the class it
/// covers. It sits here instead because the port moved TerminateToolException into
/// NOpenNLP.Tools.Formats, so that the format factories that throw it do not drag a
/// dependency on the CLI project into the library. See the notes on the ported class.
/// </remarks>
public class TerminateToolExceptionTest
{
    [Test]
    public void TestCreation()
    {
        TerminateToolException e = new(-500);
        ClassicAssert.AreEqual(-500, e.Code);
    }
}
