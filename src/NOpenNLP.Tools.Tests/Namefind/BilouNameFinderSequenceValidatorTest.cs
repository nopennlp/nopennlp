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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// This is the test class for <see cref="BilouNameFinderSequenceValidator"/>.
/// inputSequence is actually not used, but provided in the test to describe the cases.
/// </summary>
public class BilouNameFinderSequenceValidatorTest
{
    private static readonly BilouNameFinderSequenceValidator validator = new BilouNameFinderSequenceValidator();

    private const string START_A = "TypeA-" + BilouCodec.START;
    private const string CONTINUE_A = "TypeA-" + BilouCodec.CONTINUE;
    private const string LAST_A = "TypeA-" + BilouCodec.LAST;
    private const string UNIT_A = "TypeA-" + BilouCodec.UNIT;

    private const string START_B = "TypeB-" + BilouCodec.START;
    private const string CONTINUE_B = "TypeB-" + BilouCodec.CONTINUE;
    private const string LAST_B = "TypeB-" + BilouCodec.LAST;

    private const string OTHER = BilouCodec.OTHER;

    [Test]
    public void TestStartAsFirstLabel()
    {
        string outcome = START_A;
        string[] inputSequence = ["TypeA", "TypeA", "something"];
        string[] outcomesSequence = [];
        ClassicAssert.IsTrue(validator.ValidSequence(0, inputSequence, outcomesSequence, outcome));
    }

    [Test]
    public void TestContinueAsFirstLabel()
    {
        string outcome = CONTINUE_A;
        string[] inputSequence = ["TypeA", "something", "something"];
        string[] outcomesSequence = [];
        ClassicAssert.IsFalse(validator.ValidSequence(0, inputSequence, outcomesSequence, outcome));
    }

    [Test]
    public void TestLastAsFirstLabel()
    {
        string outcome = LAST_A;
        string[] inputSequence = ["TypeA", "something", "something"];
        string[] outcomesSequence = [];
        ClassicAssert.IsFalse(validator.ValidSequence(0, inputSequence, outcomesSequence, outcome));
    }

    [Test]
    public void TestUnitAsFirstLabel()
    {
        string outcome = UNIT_A;
        string[] inputSequence = ["TypeA", "something", "something"];
        string[] outcomesSequence = [];
        ClassicAssert.IsTrue(validator.ValidSequence(0, inputSequence, outcomesSequence, outcome));
    }

    [Test]
    public void TestOtherAsFirstLabel()
    {
        string outcome = OTHER;
        string[] inputSequence = ["something", "TypeA", "something"];
        string[] outcomesSequence = [];
        ClassicAssert.IsTrue(validator.ValidSequence(0, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Start, Any Start =&gt; Invalid
    /// </summary>
    [Test]
    public void TestBeginFollowedByBegin()
    {
        string[] outcomesSequence = [START_A];

        // Same Types
        string outcome = START_A;
        string[] inputSequence = ["TypeA", "TypeA", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));

        // Diff. Types
        outcome = START_B;
        inputSequence = ["TypeA", "TypeB", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Start, Continue, Same type =&gt; Valid
    /// <para/>
    /// Start, Continue, Diff. Type =&gt; Invalid
    /// </summary>
    [Test]
    public void TestBeginFollowedByContinue()
    {
        string[] outcomesSequence = [START_A];

        // Same Types
        string outcome = CONTINUE_A;
        string[] inputSequence = ["TypeA", "TypeA", "TypeA", "something"];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));

        // Different Types
        outcome = CONTINUE_B;
        inputSequence = ["TypeA", "TypeB", "TypeB", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Start, Last, Same Type =&gt; Valid
    /// <para/>
    /// Start, Last, Diff. Type =&gt; Invalid
    /// </summary>
    [Test]
    public void TestStartFollowedByLast()
    {
        string[] outcomesSequence = [START_A];

        // Same Type
        string outcome = LAST_A;
        string[] inputSequence = ["TypeA", "TypeA", "something"];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));

        // Diff. Types
        outcome = LAST_B;
        inputSequence = ["TypeA", "TypeB", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Start, Other =&gt; Invalid
    /// </summary>
    [Test]
    public void TestStartFollowedByOther()
    {
        string outcome = OTHER;
        string[] inputSequence = ["TypeA", "something", "something"];
        string[] outcomesSequence = [START_A];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Start, Unit =&gt; Invalid
    /// </summary>
    [Test]
    public void TestStartFollowedByUnit()
    {
        string outcome = UNIT_A;
        string[] inputSequence = ["TypeA", "AnyType", "something"];
        string[] outcomesSequence = [START_A];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Continue, Any Begin =&gt; Invalid
    /// </summary>
    [Test]
    public void TestContinueFollowedByStart()
    {
        string[] outcomesSequence = [START_A, CONTINUE_A];

        // Same Types
        string outcome = START_A;
        string[] inputSequence = ["TypeA", "TypeA", "TypeA", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));

        // Diff. Types
        outcome = START_B;
        inputSequence = ["TypeA", "TypeA", "TypeB", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Continue, Continue, Same type =&gt; Valid
    /// <para/>
    /// Continue, Continue, Diff. Type =&gt; Invalid
    /// </summary>
    [Test]
    public void TestContinueFollowedByContinue()
    {
        string[] outcomesSequence = [START_A, CONTINUE_A, CONTINUE_A];

        // Same Types
        string outcome = CONTINUE_A;
        string[] inputSequence = ["TypeA", "TypeA", "TypeA", "TypeA", "something"];
        ClassicAssert.IsTrue(validator.ValidSequence(3, inputSequence, outcomesSequence, outcome));

        // Different Types
        outcome = CONTINUE_B;
        inputSequence = ["TypeA", "TypeA", "TypeA", "TypeB", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(3, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Continue, Last, Same Type =&gt; Valid
    /// <para/>
    /// Continue, Last, Diff. Type =&gt; Invalid
    /// </summary>
    [Test]
    public void TestContinueFollowedByLast()
    {
        string[] outcomesSequence = [OTHER, START_A, CONTINUE_A];

        // Same Types
        string outcome = LAST_A;
        string[] inputSequence = ["something", "TypeA", "TypeA", "TypeA", "something"];
        ClassicAssert.IsTrue(validator.ValidSequence(3, inputSequence, outcomesSequence, outcome));

        // Different Types
        outcome = LAST_B;
        inputSequence = ["something", "TypeA", "TypeA", "TypeB", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(3, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Continue, Other =&gt; Invalid
    /// </summary>
    [Test]
    public void TestContinueFollowedByOther()
    {
        string outcome = OTHER;
        string[] inputSequence = ["TypeA", "TypeA", "something", "something"];
        string[] outcomesSequence = [START_A, CONTINUE_A];
        ClassicAssert.IsFalse(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Continue, Unit =&gt; Invalid
    /// </summary>
    [Test]
    public void TestContinueFollowedByUnit()
    {
        string outcome = UNIT_A;
        string[] inputSequence = ["TypeA", "TypeA", "AnyType", "something"];
        string[] outcomesSequence = [START_A, CONTINUE_A];
        ClassicAssert.IsFalse(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Last, Any Start =&gt; Valid
    /// </summary>
    [Test]
    public void TestLastFollowedByStart()
    {
        string[] outcomesSequence = [START_A, CONTINUE_A, LAST_A];

        // Same Types
        string outcome = START_A;
        string[] inputSequence = ["TypeA", "TypeA", "TypeA", "TypeA", "TypeA"];
        ClassicAssert.IsTrue(validator.ValidSequence(3, inputSequence, outcomesSequence, outcome));

        // Same Types
        outcome = START_B;
        inputSequence = ["TypeA", "TypeA", "TypeA", "TypeB", "TypeB"];
        ClassicAssert.IsTrue(validator.ValidSequence(3, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Last, Any Continue =&gt; Invalid
    /// </summary>
    [Test]
    public void TestLastFollowedByContinue()
    {
        string[] outcomesSequence = [START_A, CONTINUE_A, LAST_A];

        string outcome = CONTINUE_A;
        string[] inputSequence = ["TypeA", "TypeA", "TypeA", "TypeA", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(3, inputSequence, outcomesSequence, outcome));

        // Diff. Types
        outcome = CONTINUE_B;
        inputSequence = ["TypeA", "TypeA", "TypeA", "TypeB", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(3, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Last, Any Last =&gt; Invalid
    /// </summary>
    [Test]
    public void TestLastFollowedByLast()
    {
        string[] outcomesSequence = [OTHER, OTHER, START_A, CONTINUE_A, LAST_A];

        // Same Types
        string outcome = LAST_A;
        string[] inputSequence = ["something", "something", "TypeA", "TypeA", "TypeA", "TypeA", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(5, inputSequence, outcomesSequence, outcome));

        // Diff. Types
        outcome = LAST_B;
        inputSequence = ["something", "something", "TypeA", "TypeA", "TypeA", "TypeB", "something"];
        ClassicAssert.IsFalse(validator.ValidSequence(5, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Last, Other =&gt; Valid
    /// </summary>
    [Test]
    public void TestLastFollowedByOther()
    {
        string outcome = OTHER;
        string[] inputSequence = ["TypeA", "TypeA", "TypeA", "something", "something"];
        string[] outcomesSequence = [START_A, CONTINUE_A, LAST_A];
        ClassicAssert.IsTrue(validator.ValidSequence(3, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Last, Unit =&gt; Valid
    /// </summary>
    [Test]
    public void TestLastFollowedByUnit()
    {
        string outcome = UNIT_A;
        string[] inputSequence = ["TypeA", "TypeA", "TypeA", "AnyType", "something"];
        string[] outcomesSequence = [START_A, CONTINUE_A, LAST_A];
        ClassicAssert.IsTrue(validator.ValidSequence(3, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Other, Any Start =&gt; Valid
    /// </summary>
    [Test]
    public void TestOtherFollowedByBegin()
    {
        string outcome = START_A;
        string[] inputSequence = ["something", "TypeA", "TypeA"];
        string[] outcomesSequence = [OTHER];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Other, Any Continue =&gt; Invalid
    /// </summary>
    [Test]
    public void TestOtherFollowedByContinue()
    {
        string outcome = CONTINUE_A;
        string[] inputSequence = ["something", "TypeA", "TypeA"];
        string[] outcomesSequence = [OTHER];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Other, Any Last =&gt; Invalid
    /// </summary>
    [Test]
    public void TestOtherFollowedByLast()
    {
        string outcome = LAST_A;
        string[] inputSequence = ["something", "TypeA", "TypeA"];
        string[] outcomesSequence = [OTHER];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Outside, Unit =&gt; Valid
    /// </summary>
    [Test]
    public void TestOtherFollowedByUnit()
    {
        string outcome = UNIT_A;
        string[] inputSequence = ["something", "AnyType", "something"];
        string[] outcomesSequence = [OTHER];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Other, Other =&gt; Valid
    /// </summary>
    [Test]
    public void TestOutsideFollowedByOutside()
    {
        string outcome = OTHER;
        string[] inputSequence = ["something", "something", "something"];
        string[] outcomesSequence = [OTHER];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Unit, Any Start =&gt; Valid
    /// </summary>
    [Test]
    public void TestUnitFollowedByBegin()
    {
        string outcome = START_A;
        string[] inputSequence = ["AnyType", "TypeA", "something"];
        string[] outcomesSequence = [UNIT_A];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Unit, Any Continue =&gt; Invalid
    /// </summary>
    [Test]
    public void TestUnitFollowedByInside()
    {
        string outcome = CONTINUE_A;
        string[] inputSequence = ["TypeA", "TypeA", "something"];
        string[] outcomesSequence = [UNIT_A];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Unit, Any Last =&gt; Invalid
    /// </summary>
    [Test]
    public void TestUnitFollowedByLast()
    {
        string outcome = LAST_A;
        string[] inputSequence = ["AnyType", "TypeA", "something"];
        string[] outcomesSequence = [UNIT_A];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Unit, Other =&gt; Valid
    /// </summary>
    [Test]
    public void TestUnitFollowedByOutside()
    {
        string outcome = OTHER;
        string[] inputSequence = ["TypeA", "something", "something"];
        string[] outcomesSequence = [UNIT_A];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    /// <summary>
    /// Unit, Unit =&gt; Valid
    /// </summary>
    [Test]
    public void TestUnitFollowedByUnit()
    {
        string outcome = UNIT_A;
        string[] inputSequence = ["AnyType", "AnyType", "something"];
        string[] outcomesSequence = [UNIT_A];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }
}
