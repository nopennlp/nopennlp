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

using J2N.Collections.ObjectModel;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Muc;

internal static class MucElementNames
{
    internal const string DocElement = "DOC";
    internal const string HeadlineElement = "HL";
    internal const string DatelineElement = "DATELINE";
    internal const string DdElement = "DD";
    internal const string SentenceElement = "s";

    // NOpenNLP: upstream builds the set in a static initializer and wraps it in
    // Collections.unmodifiableSet. A read-only collection initializer expresses the same thing.
    internal static readonly ReadOnlySet<string> ContentElements = new JCG.HashSet<string>
    {
        HeadlineElement,
        DatelineElement,
        DdElement,
        SentenceElement
    }.AsReadOnly();
}
