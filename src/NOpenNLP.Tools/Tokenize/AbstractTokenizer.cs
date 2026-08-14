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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using J2N.Text;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Tokenize;

// NOpenNLP: this type is package-private in Apache OpenNLP 1.9.4. C# has no
// equivalent accessibility, and a public class may not derive from a less
// accessible base (CS0060), so the public TokenizerME, SimpleTokenizer and
// WhitespaceTokenizer subclasses require this to be public.
public abstract class AbstractTokenizer : ITokenizer
{
    public virtual string[] Tokenize(string s)
        => Span.SpansToStrings(TokenizePos(s), s.AsCharSequence());

    // NOpenNLP: Java allows an abstract class to leave an interface method
    // unimplemented; C# requires an explicit abstract declaration.
    public abstract Span[] TokenizePos(string s);
}
