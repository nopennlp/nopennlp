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
using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;

namespace NOpenNLP.Tools.Formats.Brat;

public class BratAnnotationStreamTest
{
    private static IObjectStream<BratAnnotation?> CreatBratAnnotationStream(
        AnnotationConfiguration conf, string file)
    {
        Stream @in = TestResources.OpenResource(file);
        return new BratAnnotationStream(conf, "testing", @in);
    }

    internal static void AddEntityTypes(IDictionary<string, string> typeToClassMap)
    {
        typeToClassMap["Person"] = AnnotationConfiguration.ENTITY_TYPE;
        typeToClassMap["Location"] = AnnotationConfiguration.ENTITY_TYPE;
        typeToClassMap["Organization"] = AnnotationConfiguration.ENTITY_TYPE;
        typeToClassMap["Date"] = AnnotationConfiguration.ENTITY_TYPE;
    }

    [Test]
    public void TestParsingEntities()
    {
        IDictionary<string, string> typeToClassMap = new Dictionary<string, string>();
        AddEntityTypes(typeToClassMap);

        AnnotationConfiguration annConfig = new(typeToClassMap);

        IObjectStream<BratAnnotation?> annStream = CreatBratAnnotationStream(annConfig,
            "/opennlp/tools/formats/brat/voa-with-entities.ann");

        // TODO: Test if we get the entities ... we expect!

        BratAnnotation? ann;
        while ((ann = annStream.Read()) != null)
        {
            Console.WriteLine(ann);
        }
    }

    [Test]
    public void TestParsingRelations()
    {
        IDictionary<string, string> typeToClassMap = new Dictionary<string, string>();
        AddEntityTypes(typeToClassMap);
        typeToClassMap["Related"] = AnnotationConfiguration.RELATION_TYPE;

        AnnotationConfiguration annConfig = new(typeToClassMap);

        IObjectStream<BratAnnotation?> annStream = CreatBratAnnotationStream(annConfig,
            "/opennlp/tools/formats/brat/voa-with-relations.ann");

        // TODO: Test if we get the entities ... we expect!

        BratAnnotation? ann;
        while ((ann = annStream.Read()) != null)
        {
            Console.WriteLine(ann);
        }
    }
}
