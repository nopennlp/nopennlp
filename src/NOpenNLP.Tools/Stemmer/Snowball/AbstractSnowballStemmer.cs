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

namespace NOpenNLP.Tools.Stemmer.Snowball;

/// <summary>
/// Base class for the generated Snowball stemmers.
/// </summary>
/// <remarks>
/// Upstream declares this as <c>abstract class AbstractSnowballStemmer extends
/// SnowballProgram</c> with an abstract <c>stem()</c>. Here the abstract
/// <c>stem()</c> already comes from the vendored <see cref="Stemmer"/> runtime,
/// so this type only exists to give the generated code a stable base class name
/// to derive from, which the generator is pointed at with its <c>-p</c> flag.
/// <para/>
/// Keeping this seam means the generated files never mention the vendored
/// runtime's type name directly, so the two can be re-vendored or renamed
/// independently of the 20 generated stemmers.
/// </remarks>
public abstract class AbstractSnowballStemmer : Stemmer
{
}
