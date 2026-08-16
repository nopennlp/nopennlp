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

public class CollectionObjectStream<E> : ObjectStreamBase<E?>
    where E : class
{
    private readonly ICollection<E> collection; // NOpenNLP: made readonly

    private IEnumerator<E> iterator;

    public CollectionObjectStream(ICollection<E> collection)
    {
        this.collection = collection;
        iterator = collection.GetEnumerator();
    }

    // NOpenNLP: Java's Iterator.hasNext() peeks without advancing, while
    // IEnumerator.MoveNext() advances and reports in one call. Advancing first
    // and reading Current gives the same sequence.
    public override E? Read() => iterator.MoveNext() ? iterator.Current : null;

    public override void Reset()
    {
        iterator.Dispose();
        iterator = collection.GetEnumerator();
    }

    public override void Close() => iterator.Dispose();
}
