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

using System.IO;
using NOpenNLP.Tools.Support;

namespace NOpenNLP.Tools.Ml.Model;

// NOpenNLP: upstream wraps a java.io.ObjectInputStream, which has no .NET
// counterpart. Only the three DataInput methods below are used, and those read
// the same big-endian layout java.io.DataInputStream does, so this wraps a
// plain Stream.
public class ObjectDataReader(Stream ois) : IDataReader
{
    protected readonly Stream ois = ois;

    public virtual double ReadDouble() => ois.ReadJavaDouble();

    public virtual int ReadInt32() => ois.ReadJavaInt32();

    public virtual string ReadUTF() => ois.ReadJavaUTF();
}
