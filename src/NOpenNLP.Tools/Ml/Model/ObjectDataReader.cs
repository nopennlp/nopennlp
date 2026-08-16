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

using J2N.IO;

namespace NOpenNLP.Tools.Ml.Model;

// NOpenNLP: upstream wraps a java.io.ObjectInputStream, which has no .NET
// counterpart. Only the three DataInput methods below are used, so this wraps
// J2N's IDataInput — the interface ObjectInputStream implements for them.
public class ObjectDataReader : IDataReader
{
    protected IDataInput ois;

    public ObjectDataReader(IDataInput ois)
    {
        this.ois = ois;
    }

    public virtual double ReadDouble()
    {
        return ois.ReadDouble();
    }

    public virtual int ReadInt32()
    {
        return ois.ReadInt32();
    }

    public virtual string ReadUTF()
    {
        return ois.ReadUTF();
    }
}
