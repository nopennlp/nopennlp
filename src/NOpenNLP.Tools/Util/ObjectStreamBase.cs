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

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Supplies the <see cref="IObjectStream{T}"/> members that upstream declares as Java 8
/// default methods.
/// </summary>
// NOpenNLP-specific: opennlp.tools.util.ObjectStream declares reset() and close()
// as default methods, which C# cannot express on netstandard2.0. This base class
// carries those defaults -- reset() throws UnsupportedOperationException, close()
// does nothing -- so ported implementations that do not override them read the
// same as upstream. It also maps Close() onto the IDisposable pattern for `using`
// support. It adds no state and no behavior beyond those defaults.
public abstract class ObjectStreamBase<T> : IObjectStream<T>
{
    /// <inheritdoc/>
    public abstract T Read();

    /// <inheritdoc/>
    public virtual void Reset() =>
        throw new NotSupportedException("reset is not supported on this stream");

    /// <summary>
    /// Releases the resources from this object stream.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
