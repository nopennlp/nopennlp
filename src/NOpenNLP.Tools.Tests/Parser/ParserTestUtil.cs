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
using System.Text;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using HeadRules = NOpenNLP.Tools.Parser.Lang.En.HeadRules;

namespace NOpenNLP.Tools.Parser;

public class ParserTestUtil
{
    public static HeadRules CreateTestHeadRules()
    {
        using Stream headRulesIn = TestResources.OpenResource("/opennlp/tools/parser/en_head_rules");
        using StreamReader reader = new(headRulesIn, Encoding.UTF8);

        return new HeadRules(reader);
    }

    /// <summary>
    /// NOpenNLP: upstream returns an anonymous <c>ObjectStream&lt;Parse&gt;</c> that
    /// re-opens the resource on every <c>reset()</c>. The named type below does the
    /// same job, since C# has no anonymous classes.
    /// </summary>
    public static IObjectStream<Parse?> OpenTestTrainingData()
    {
        OpenTestTrainingDataObjectStreamBaseAnonymousClass resetableSampleStream = new();

        resetableSampleStream.Reset();

        return resetableSampleStream;
    }

    private sealed class OpenTestTrainingDataObjectStreamBaseAnonymousClass : ObjectStreamBase<Parse?>
    {
        private IObjectStream<Parse?>? samples;

        public override Parse? Read() => samples!.Read();

        public override void Reset()
        {
            samples?.Dispose();

            // NOpenNLP: upstream wraps the body in a catch for
            // UnsupportedEncodingException that calls Assert.fail. .NET has no checked
            // counterpart and Encoding.UTF8 cannot throw it -- upstream's own comment
            // says "Should never happen" -- so there is nothing to catch here.

            // NOpenNLP: upstream uses opennlp.tools.formats.ResourceAsStreamFactory,
            // which is not ported; the ResourceAsStreamFactory in Support does the
            // same job over an embedded resource.
            IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/parser/parser.train");
            samples = new ParseSampleStream(new PlainTextByLineStream(@in, Encoding.UTF8));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                samples?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
