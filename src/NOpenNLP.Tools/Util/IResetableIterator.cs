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

using System.Collections.Generic;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// This interface makes an <see cref="IEnumerator{E}"/> resetable.
/// </summary>
// NOpenNLP: upstream extends java.util.Iterator, whose closest counterpart is
// IEnumerator<E>. IEnumerator already declares Reset(), so this interface adds
// no members of its own; it is kept so ported signatures naming
// ResetableIterator have a type to refer to.
public interface IResetableIterator<out E> : IEnumerator<E>
{
}
