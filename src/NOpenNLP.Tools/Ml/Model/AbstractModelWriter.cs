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
using System.IO;

namespace NOpenNLP.Tools.Ml.Model;

// NOpenNLP: upstream declares close(); it maps onto IDisposable so C# `using`
// works, matching how IObjectStream handles AutoCloseable.
public abstract class AbstractModelWriter : IDisposable
{
    private bool closed;

    protected AbstractModelWriter()
    {
    }

    /// <exception cref="IOException">if there is an error during writing</exception>
    public abstract void WriteUTF(string s);

    /// <exception cref="IOException">if there is an error during writing</exception>
    public abstract void WriteInt32(int i);

    /// <exception cref="IOException">if there is an error during writing</exception>
    public abstract void WriteDouble(double d);

    /// <summary>
    /// Closes the underlying stream. Closing an already-closed writer does nothing.
    /// </summary>
    /// <exception cref="IOException">if there is an error during writing</exception>
    // NOpenNLP: Persist() ends by calling Close(), so a writer used in a `using`
    // block would close its stream a second time on the way out. Java's
    // OutputStream.close() is specified to be a no-op when already closed, but
    // .NET throws ObjectDisposedException, so the repeat is absorbed here rather
    // than in each concrete writer. Subclasses override CloseCore.
    public void Close()
    {
        if (closed)
        {
            return;
        }

        closed = true;
        CloseCore();
    }

    /// <summary>
    /// Performs the actual close. Called at most once.
    /// </summary>
    /// <exception cref="IOException">if there is an error during writing</exception>
    protected abstract void CloseCore();

    /// <exception cref="IOException">if there is an error during writing</exception>
    public abstract void Persist();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }
    }
}
