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
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats;

// NOpenNLP: upstream takes (Class<?> clazz, String name) and resolves the resource
// relative to that class with Class.getResourceAsStream. .NET embedded resources are
// addressed by their manifest name rather than relative to a type, so this takes the
// upstream classpath path directly and resolves it through TestResources, which does
// that translation for every ported test.
public class ResourceAsStreamFactory : IInputStreamFactory
{
    private readonly string name;

    public ResourceAsStreamFactory(string name)
    {
        this.name = name ?? throw new ArgumentNullException(nameof(name), "name must not be null");
    }

    /// <exception cref="IOException">if the stream cannot be created</exception>
    public Stream CreateInputStream() => TestResources.OpenResource(name);
}
