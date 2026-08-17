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

namespace NOpenNLP.Tools.Util.Wordvector;

/// <summary>
/// A word vector.
/// <para/>
/// Warning: Experimental new feature, see OPENNLP-1144 for details, the API might be changed anytime.
/// </summary>
public interface IWordVector
{
    /// <summary>
    /// Gets the element type of this vector.
    /// </summary>
    WordVectorType DataType { get; }

    /// <summary>
    /// Gets the element at <paramref name="index"/> as a <see cref="float"/>.
    /// </summary>
    float GetAsSingle(int index);

    /// <summary>
    /// Gets the element at <paramref name="index"/> as a <see cref="double"/>.
    /// </summary>
    double GetAsDouble(int index);

    // NOpenNLP-specific: Java returns a read-only FloatBuffer/DoubleBuffer, which has no .NET
    // counterpart. ReadOnlyMemory<T> is the idiomatic read-only view over a contiguous buffer.
    /// <summary>
    /// Returns a read-only view of this vector as <see cref="float"/> values.
    /// </summary>
    ReadOnlyMemory<float> ToSingleBuffer();

    /// <summary>
    /// Returns a read-only view of this vector as <see cref="double"/> values.
    /// </summary>
    ReadOnlyMemory<double> ToDoubleBuffer();

    /// <summary>
    /// Gets the number of elements in this vector.
    /// </summary>
    int Dimension { get; }
}
