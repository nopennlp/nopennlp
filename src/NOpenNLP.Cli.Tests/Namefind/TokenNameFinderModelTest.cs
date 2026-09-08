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

using System.Collections.Generic;
using System.IO;
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Cmdline.Namefind;

/// <remarks>
/// NOpenNLP: upstream keeps this test in opennlp-tools, beside the model it builds,
/// because the cmdline package ships there too. This port splits cmdline out into
/// NOpenNLP.Cli, which NOpenNLP.Tools.Tests does not reference, so the test lives here
/// instead of at its upstream-mirrored path.
/// </remarks>
public class TokenNameFinderModelTest
{
    [Test]
    public void TestNERWithPOSModel()
    {
        // create a resources folder
        string resourcesFolder = Path.Combine(Path.GetTempPath(),
            $"resources-{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}");
        Directory.CreateDirectory(resourcesFolder);

        // save a POS model there
        POSModel posModel = TrainPOSModel(ModelType.MAXENT);
        FileInfo posModelFile = new(Path.Combine(resourcesFolder, "pos-model.bin"));

        posModel.Serialize(posModelFile);

        ClassicAssert.IsTrue(posModelFile.Exists);

        // load feature generator xml bytes
        using Stream fgInputStream = TestResources.OpenResource("/opennlp/tools/namefind/ner-pos-features.xml");
        using StreamReader buffers = new(fgInputStream);
        string featureGeneratorString = buffers.ReadToEnd();

        // create a featuregenerator file
        string featureGenerator = Path.Combine(Path.GetTempPath(),
            $"ner-featuregen-{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.xml");
        File.WriteAllBytes(featureGenerator, Encoding.UTF8.GetBytes(featureGeneratorString));

        IDictionary<string, object> resources;
        try
        {
            resources = TokenNameFinderTrainerTool.LoadResources(new DirectoryInfo(resourcesFolder),
                new FileInfo(featureGenerator));
        }
        catch (IOException e)
        {
            throw new TerminateToolException(-1, e.Message, e);
        }
        finally
        {
            File.Delete(featureGenerator);
        }

        // train a name finder
        // NOpenNLP: upstream resolves "opennlp/tools/namefind/voa1.train" as a relative
        // file path against the Maven test classpath directory. The corpora are embedded
        // resources here, so the training data is materialized to a temp file first.
        using TempResourceFile trainData = new("/opennlp/tools/namefind/voa1.train");
        // NOpenNLP: upstream leaves this stream open -- it reads the corpus straight off
        // the classpath, so nothing has to be released. Here the corpus is materialized
        // to a temp file that TempResourceFile deletes, and Windows will not delete a
        // file that is still open, so the stream is disposed first. Declared after
        // trainData so it is disposed before it, since `using` unwinds in reverse.
        using IObjectStream<NameSample?> sampleStream = new NameSampleDataStream(
            new PlainTextByLineStream(new MockInputStreamFactory(
                new FileInfo(trainData.Path)), Encoding.UTF8));

        TrainingParameters @params = new();
        @params.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        @params.Put(TrainingParameters.CUTOFF_PARAM, 1);

        TokenNameFinderModel nameFinderModel = NameFinderME.Train("en", null, sampleStream,
            @params, TokenNameFinderFactory.Create(null,
                Encoding.UTF8.GetBytes(featureGeneratorString), resources, new BioCodec()));

        string model = Path.Combine(Path.GetTempPath(),
            $"nermodel-{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.bin");
        try
        {
            using (FileStream modelOut = File.Create(model))
            {
                nameFinderModel.Serialize(modelOut);
            }

            FileAssert.Exists(model);
        }
        finally
        {
            File.Delete(model);

            // NOpenNLP: upstream calls opennlp.tools.util.FileUtil.deleteDirectory, which
            // this port has not ported. Directory.Delete does the same recursive removal.
            Directory.Delete(resourcesFolder, recursive: true);
        }
    }

    /// <summary>
    /// NOpenNLP: upstream calls <c>POSTaggerMETest.trainPOSModel</c>. That test class
    /// lives in NOpenNLP.Tools.Tests, and linking it here would re-run its own [Test]
    /// methods in this assembly, so the helper is repeated instead. It is a verbatim
    /// copy of the one there.
    /// </summary>
    private static POSModel TrainPOSModel(ModelType type)
    {
        TrainingParameters @params = new();
        @params.Put(TrainingParameters.ALGORITHM_PARAM, type.ToString());
        @params.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        @params.Put(TrainingParameters.CUTOFF_PARAM, 5);

        IInputStreamFactory @in = new ResourceAsStreamFactory("/opennlp/tools/postag/AnnotatedSentences.txt");
        IObjectStream<POSSample?> samples = new WordTagSampleStream(new PlainTextByLineStream(@in, Encoding.UTF8));

        return POSTaggerME.Train("eng", samples, @params, new POSTaggerFactory());
    }
}
