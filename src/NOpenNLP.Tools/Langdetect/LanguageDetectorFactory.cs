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
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Ext;
using NOpenNLP.Tools.Util.Normalizer;

namespace NOpenNLP.Tools.Langdetect;

/// <summary>
/// Default factory used by Language Detector. Extend this class to change the Language Detector
/// behaviour, such as the <see cref="ILanguageDetectorContextGenerator"/>.
/// The default <see cref="DefaultLanguageDetectorContextGenerator"/> will use char n-grams of
/// size 1 to 3 and the following normalizers:
/// <list type="bullet">
///  <item><description><see cref="EmojiCharSequenceNormalizer"/></description></item>
///  <item><description><see cref="UrlCharSequenceNormalizer"/></description></item>
///  <item><description><see cref="TwitterCharSequenceNormalizer"/></description></item>
///  <item><description><see cref="NumberCharSequenceNormalizer"/></description></item>
///  <item><description><see cref="ShrinkCharSequenceNormalizer"/></description></item>
/// </list>
/// </summary>
public class LanguageDetectorFactory : BaseToolFactory
{
    public virtual ILanguageDetectorContextGenerator GetContextGenerator() =>
        new DefaultLanguageDetectorContextGenerator(1, 3,
            EmojiCharSequenceNormalizer.GetInstance(),
            UrlCharSequenceNormalizer.GetInstance(),
            TwitterCharSequenceNormalizer.GetInstance(),
            NumberCharSequenceNormalizer.GetInstance(),
            ShrinkCharSequenceNormalizer.GetInstance());

    public static LanguageDetectorFactory Create(string? subclassName)
    {
        if (subclassName == null)
        {
            // will create the default factory
            return new LanguageDetectorFactory();
        }

        try
        {
            LanguageDetectorFactory theFactory =
                ExtensionLoader.InstantiateExtension<LanguageDetectorFactory>(subclassName);
            theFactory.Init();
            return theFactory;
        }
        catch (Exception e)
        {
            string msg = "Could not instantiate the " + subclassName
                + ". The initialization throw an exception.";
            throw new InvalidFormatException(msg, e);
        }
    }

    public virtual void Init()
    {
        // nothing to do
    }

    public override void ValidateArtifactMap()
    {
        // nothing to validate
    }
}
