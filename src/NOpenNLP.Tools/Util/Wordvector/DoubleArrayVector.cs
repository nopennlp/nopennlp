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

internal class DoubleArrayVector(double[] vector) : IWordVector
{
    public WordVectorType DataType => WordVectorType.Double;

    public float GetAsSingle(int index) => (float)GetAsDouble(index);

    public double GetAsDouble(int index) => vector[index];

    public ReadOnlyMemory<float> ToSingleBuffer()
    {
        float[] floatVector = new float[vector.Length];
        for (int i = 0; i < floatVector.Length; i++)
        {
            floatVector[i] = (float)vector[i];
        }

        return floatVector;
    }

    public ReadOnlyMemory<double> ToDoubleBuffer() => vector;

    public int Dimension => vector.Length;
}
