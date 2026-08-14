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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Ext;
using System;

namespace NOpenNLP.Tools.Lemmatizer;

public class LemmatizerFactory : BaseToolFactory
{
    /// <summary>
    /// Creates a <see cref="LemmatizerFactory"/> that provides the default implementation
    /// of the resources.
    /// </summary>
    public LemmatizerFactory()
    {
    }

    public static LemmatizerFactory Create(string? subclassName)
    {
        if (subclassName == null)
        {
            // will create the default factory
            return new LemmatizerFactory();
        }

        try
        {
            return ExtensionLoader.InstantiateExtension<LemmatizerFactory>(subclassName);
        }
        catch (Exception e)
        {
            string msg = $"Could not instantiate the {subclassName}. The initialization throw an exception.";
            Console.Error.WriteLine(msg);
            e.PrintStackTrace();
            throw new InvalidFormatException(msg, e);
        }
    }

    public override void ValidateArtifactMap()
    {
    }

    public virtual ISequenceValidator<string> SequenceValidator => new DefaultLemmatizerSequenceValidator();

    public virtual ILemmatizerContextGenerator ContextGenerator => new DefaultLemmatizerContextGenerator();
}
