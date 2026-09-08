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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using J2N.Text;

namespace NOpenNLP.Tools.Ml.Model;

public abstract class AbstractModelReader
{
    /// <summary>
    /// Setting for overriding the maximum number of entries (outcomes, predicates,
    /// outcome patterns, chunk counts) that may be read from a model file or training data.
    /// <para/>
    /// Falls back to <c>10_000_000</c> if absent or invalid.
    /// </summary>
    // NOpenNLP: upstream reads this from a JVM system property, set with
    // -DOPENNLP_MAX_ENTRIES=5000000. .NET has no such thing, so the value is read from
    // AppContext data (settable in runtimeconfig.json or via AppContext.SetSwitch's data
    // counterpart) and falls back to the environment variable of the same name.
    public const string MAX_ENTRIES_PROPERTY = "OPENNLP_MAX_ENTRIES";

    /// <summary>
    /// Upper bound on count fields read from a model file.
    /// Prevents OOM on crafted inputs with oversized array size declarations.
    /// Configurable via the <see cref="MAX_ENTRIES_PROPERTY"/> setting.
    /// </summary>
    internal static readonly int MAX_ENTRIES = InitMaxEntries();

    private static int InitMaxEntries()
    {
        string prop = (AppContext.GetData(MAX_ENTRIES_PROPERTY) as string
                       ?? Environment.GetEnvironmentVariable(MAX_ENTRIES_PROPERTY)
                       ?? string.Empty).Trim();

        if (prop.Length > 0
            && int.TryParse(prop, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int val)
            && val > 0)
        {
            return val;
        }

        return 10_000_000;
    }

    /// <summary>
    /// The number of predicates contained in the model.
    /// </summary>
    protected int NUM_PREDS;
    protected readonly IDataReader dataReader; // NOpenNLP: made readonly

    protected AbstractModelReader(FileInfo f)
    {
        string filename = f.Name;
        Stream input;

        // handle the zipped/not zipped distinction
        if (filename.EndsWith(".gz", StringComparison.Ordinal))
        {
            input = new GZipStream(f.OpenRead(), CompressionMode.Decompress);
            filename = filename[..^3];
        }
        else
        {
            input = f.OpenRead();
        }


        // handle the different formats
        if (filename.EndsWith(".bin", StringComparison.Ordinal))
        {
            this.dataReader = new BinaryFileDataReader(input);
        }
        else
        {

            // filename ends with ".txt"
            this.dataReader = new PlainTextFileDataReader(input);
        }
    }

    protected AbstractModelReader(IDataReader dataReader)
        /* : base() */
    {
        this.dataReader = dataReader;
    }

    /// <summary>
    /// Implement as needed for the format the model is stored in.
    /// </summary>
    public virtual int ReadInt32()
    {
        return dataReader.ReadInt32();
    }

    /// <summary>
    /// Implement as needed for the format the model is stored in.
    /// </summary>
    public virtual double ReadDouble()
    {
        return dataReader.ReadDouble();
    }

    /// <summary>
    /// Implement as needed for the format the model is stored in.
    /// </summary>
    public virtual string ReadUTF()
    {
        return dataReader.ReadUTF();
    }

    public virtual AbstractModel Model
    {
        get
        {
            CheckModelType();
            return ConstructModel();
        }
    }

    public abstract void CheckModelType();

    public abstract AbstractModel ConstructModel();

    protected virtual string[] Outcomes
    {
        get
        {
            int numOutcomes = ReadInt32();
            if (numOutcomes < 0 || numOutcomes > MAX_ENTRIES)
            {
                throw new ArgumentException(
                    $"Outcome count {numOutcomes} exceeds safe limit of {MAX_ENTRIES}");
            }

            string[] outcomeLabels = new string[numOutcomes];
            for (int i = 0; i < numOutcomes; i++)
                outcomeLabels[i] = ReadUTF();
            return outcomeLabels;
        }
    }

    protected virtual int[][] OutcomePatterns
    {
        get
        {
            int numOCTypes = ReadInt32();
            if (numOCTypes < 0 || numOCTypes > MAX_ENTRIES)
            {
                throw new ArgumentException(
                    $"Outcome pattern count {numOCTypes} exceeds safe limit of {MAX_ENTRIES}");
            }

            int[][] outcomePatterns = new int[numOCTypes][];
            for (int i = 0; i < numOCTypes; i++)
            {
                StringTokenizer tok = new StringTokenizer(ReadUTF(), " ");
                int[] infoInts = new int[tok.RemainingTokens];
                int j = 0;
                while (tok.MoveNext())
                {
                    infoInts[j] = int.Parse(tok.Current);
                    j++;
                }

                outcomePatterns[i] = infoInts;
            }

            return outcomePatterns;
        }
    }

    protected virtual string[] Predicates
    {
        get
        {
            NUM_PREDS = ReadInt32();
            if (NUM_PREDS < 0 || NUM_PREDS > MAX_ENTRIES)
            {
                throw new ArgumentException(
                    $"Predicate count {NUM_PREDS} exceeds safe limit of {MAX_ENTRIES}");
            }

            string[] predLabels = new string[NUM_PREDS];
            for (int i = 0; i < NUM_PREDS; i++)
                predLabels[i] = ReadUTF();
            return predLabels;
        }
    }

    /// <summary>
    /// Reads the parameters from a file and populates an array of context objects.
    /// </summary>
    /// <param name="outcomePatterns">The outcomes patterns for the model.  The first index refers to which
    ///     outcome pattern (a set of outcomes that occurs with a context) is being specified.  The
    ///     second index specifies the number of contexts which use this pattern at index 0, and the
    ///     index of each outcomes which make up this pattern in indicies 1-n.</param>
    /// <returns>An array of context objects.</returns>
    /// <exception cref="IOException">when the model file does not match the outcome patterns or can not be read.</exception>
    protected virtual Context[] GetParameters(int[][] outcomePatterns)
    {
        Context[] @params = new Context[NUM_PREDS];
        int pid = 0;
        foreach (int[] pattern in outcomePatterns)
        {
            //construct outcome pattern
            int[] outcomePattern = new int[pattern.Length - 1];
            Array.Copy(pattern, 1, outcomePattern, 0, pattern.Length - 1);

            //populate parameters for each context which uses this outcome pattern.
            for (int j = 0; j < pattern[0]; j++)
            {
                double[] contextParameters = new double[pattern.Length - 1];
                for (int k = 1; k < pattern.Length; k++)
                {
                    contextParameters[k - 1] = ReadDouble();
                }

                @params[pid] = new Context(outcomePattern, contextParameters);
                pid++;
            }
        }

        return @params;
    }
}
