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

namespace NOpenNLP.Tools.Ml;

// NOpenNLP: only the data-indexer constants are ported here. The rest of
// AbstractEventTrainer belongs to the trainer API, which is ported separately.
// The constants are declared in their upstream home because DataIndexerFactory
// references them.
public abstract class AbstractEventTrainer : AbstractTrainer
{
    public const string DATA_INDEXER_PARAM = "DataIndexer";
    public const string DATA_INDEXER_ONE_PASS_VALUE = "OnePass";
    public const string DATA_INDEXER_TWO_PASS_VALUE = "TwoPass";
    public const string DATA_INDEXER_ONE_PASS_REAL_VALUE = "OnePassRealValue";
}
