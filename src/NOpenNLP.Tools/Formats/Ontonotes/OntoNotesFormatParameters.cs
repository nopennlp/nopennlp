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

namespace NOpenNLP.Tools.Formats.Ontonotes;

/// <summary>
/// The command line parameters shared by the OntoNotes corpus formats.
/// </summary>
// NOpenNLP: upstream is an interface whose annotated getter ArgumentParser reflects over,
// which the three OntoNotes factories name as their Parameters type. Parameters are data
// here rather than an annotated interface -- see IFormatParameter -- so the shared getter
// becomes a shared descriptor, in the same spirit as Formats/FormatParameters. The value
// name is upstream's verbatim; upstream declares no description.
public static class OntoNotesFormatParameters
{
    /// <summary>
    /// <c>-ontoNotesDir</c>, the root of the OntoNotes corpus to read.
    /// </summary>
    public static readonly IFormatParameter OntoNotesDir =
        new FormatParameter<string>("-ontoNotesDir", "OntoNotes 4.0 corpus directory");
}
