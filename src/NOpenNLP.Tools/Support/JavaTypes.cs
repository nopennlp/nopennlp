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
using System;
using System.IO;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// A stand-in for Java's <c>java.io.ByteArrayOutputStream</c>, backed by a
/// <see cref="MemoryStream"/>.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source.
/// </remarks>
internal sealed class ByteArrayOutputStream : MemoryStream
{
    /// <summary>
    /// Returns the buffer contents as a newly allocated byte array. Unlike
    /// <see cref="MemoryStream.ToArray()"/>, this remains callable after the
    /// stream has been disposed, matching Java's behavior.
    /// </summary>
    public new byte[] ToArray() => buffer ?? base.ToArray();

    private byte[] buffer;

    protected override void Dispose(bool disposing)
    {
        // Capture the contents before the underlying buffer is released so that
        // ToArray() still works post-dispose, as it does in Java.
        buffer ??= base.ToArray();
        base.Dispose(disposing);
    }
}

/// <summary>
/// A stand-in for Java's <c>java.lang.RuntimeException</c>.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source.
/// </remarks>
internal sealed class RuntimeException : Exception
{
    public RuntimeException()
    {
    }

    public RuntimeException(string message)
        : base(message)
    {
    }

    public RuntimeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public RuntimeException(Exception innerException)
        : base(innerException?.Message, innerException)
    {
    }

    /// <summary>
    /// Creates a <see cref="RuntimeException"/> wrapping <paramref name="cause"/>.
    /// </summary>
    public static RuntimeException Create(Exception cause) => new RuntimeException(cause);

    /// <summary>
    /// Creates a <see cref="RuntimeException"/> with the given message.
    /// </summary>
    public static RuntimeException Create(string message) => new RuntimeException(message);

    /// <summary>
    /// Creates a <see cref="RuntimeException"/> with the given message and cause.
    /// </summary>
    public static RuntimeException Create(string message, Exception cause) =>
        new RuntimeException(message, cause);
}

/// <summary>
/// A stand-in for Java's <c>java.lang.ClassNotFoundException</c>, thrown when a
/// type named in a serialized model cannot be resolved.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source.
/// </remarks>
internal sealed class ClassNotFoundException : Exception
{
    public ClassNotFoundException()
    {
    }

    public ClassNotFoundException(string message)
        : base(message)
    {
    }

    public ClassNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
