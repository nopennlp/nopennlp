/*
 * Copyright 2026 NOpenNLP Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using NUnit.Framework;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// This test was added during the port to .NET to test additional factors that
/// apply specifically to the port. In other words, apply this attribute to the
/// test if it did not exist in Apache OpenNLP.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. Places the
/// test in the <c>NOPENNLP</c> category, so port-specific tests can be
/// included or excluded with a filter.
/// </remarks>
internal sealed class NOpenNLPSpecificAttribute() : CategoryAttribute("NOPENNLP");
