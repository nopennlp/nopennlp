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

namespace NOpenNLP.Tools.Cmdline.Tokenizer;

public sealed class SimpleTokenizerTool : BasicCmdLineTool
{
    /// <inheritdoc/>
    public override string ShortDescription => "character class tokenizer";

    /// <inheritdoc/>
    public override string GetHelp() => "Usage: " + CLI.Cmd + " " + Name + " < sentences";

    /// <inheritdoc/>
    public override bool HasParams => false;

    /// <inheritdoc/>
    public override void Run(string[] args)
    {
        if (args.Length != 0)
        {
            Console.WriteLine(GetHelp());
        }
        else
        {
            var tokenizer = new CommandLineTokenizer(NOpenNLP.Tools.Tokenize.SimpleTokenizer.INSTANCE);

            tokenizer.Process();
        }
    }
}
