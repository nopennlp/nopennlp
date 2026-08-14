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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// This is the test class for <see cref="NameFinderSequenceValidator"/>.
/// </summary>
public class NameFinderSequenceValidatorTest
{
    private static readonly NameFinderSequenceValidator validator = new NameFinderSequenceValidator();
    private const string START_A = "TypeA-" + NameFinderME.START;
    private const string CONTINUE_A = "TypeA-" + NameFinderME.CONTINUE;
    private const string START_B = "TypeB-" + NameFinderME.START;
    private const string CONTINUE_B = "TypeB-" + NameFinderME.CONTINUE;
    private const string OTHER = NameFinderME.OTHER;

    [Test]
    public void TestContinueCannotBeFirstOutcome()
    {
        const string outcome = CONTINUE_A;

        string[] inputSequence = ["PersonA", "is", "here"];
        string[] outcomesSequence = [];
        ClassicAssert.IsFalse(validator.ValidSequence(0, inputSequence, outcomesSequence, outcome));
    }

    [Test]
    public void TestContinueAfterStartAndSameType()
    {
        const string outcome = CONTINUE_A;

        // previous start, same name type
        string[] inputSequence = ["Stefanie", "Schmidt", "is", "German"];
        string[] outcomesSequence = [START_A];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    [Test]
    public void TestContinueAfterStartAndNotSameType()
    {
        const string outcome = CONTINUE_B;

        // previous start, not same name type
        string[] inputSequence = ["PersonA", "LocationA", "something"];
        string[] outcomesSequence = [START_A];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    [Test]
    public void TestContinueAfterContinueAndSameType()
    {
        const string outcome = CONTINUE_A;

        // previous continue, same name type
        string[] inputSequence = ["FirstName", "MidleName", "LastName", "is", "a", "long", "name"];
        string[] outcomesSequence = [START_A, CONTINUE_A];
        ClassicAssert.IsTrue(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));
    }

    [Test]
    public void TestContinueAfterContinueAndNotSameType()
    {
        const string outcome = CONTINUE_B;

        // previous continue, not same name type
        string[] inputSequence = ["FirstName", "LastName", "LocationA", "something"];
        string[] outcomesSequence = [START_A, CONTINUE_A];
        ClassicAssert.IsFalse(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));
    }

    [Test]
    public void TestContinueAfterOther()
    {
        const string outcome = CONTINUE_A;

        // previous other
        string[] inputSequence = ["something", "is", "wrong", "here"];
        string[] outcomesSequence = [OTHER];
        ClassicAssert.IsFalse(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));
    }

    [Test]
    public void TestStartIsAlwaysAValidOutcome()
    {
        const string outcome = START_A;

        // pos zero
        string[] inputSequence = ["PersonA", "is", "here"];
        string[] outcomesSequence = [];
        ClassicAssert.IsTrue(validator.ValidSequence(0, inputSequence, outcomesSequence, outcome));

        // pos one, previous other
        inputSequence = ["it's", "PersonA", "again"];
        outcomesSequence = [OTHER];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));

        // pos one, previous start
        inputSequence = ["PersonA", "PersonB", "something"];
        outcomesSequence = [START_A];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));

        // pos two, previous other
        inputSequence = ["here", "is", "PersonA"];
        outcomesSequence = [OTHER, OTHER];
        ClassicAssert.IsTrue(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));

        // pos two, previous start, same name type
        inputSequence = ["is", "PersonA", "PersoneB"];
        outcomesSequence = [OTHER, START_A];
        ClassicAssert.IsTrue(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));

        // pos two, previous start, different name type
        inputSequence = ["something", "PersonA", "OrganizationA"];
        outcomesSequence = [OTHER, START_B];
        ClassicAssert.IsTrue(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));

        // pos two, previous continue, same name type
        inputSequence = ["Stefanie", "Schmidt", "PersonB", "something"];
        outcomesSequence = [START_A, CONTINUE_A];
        ClassicAssert.IsTrue(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));

        // pos two, previous continue, not same name type
        inputSequence = ["Stefanie", "Schmidt", "OrganizationA", "something"];
        outcomesSequence = [START_B, CONTINUE_B];
        ClassicAssert.IsTrue(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));
    }

    [Test]
    public void TestOtherIsAlwaysAValidOutcome()
    {
        const string outcome = OTHER;

        // pos zero
        string[] inputSequence = ["it's", "a", "test"];
        string[] outcomesSequence = [];
        ClassicAssert.IsTrue(validator.ValidSequence(0, inputSequence, outcomesSequence, outcome));

        // pos one, previous other
        inputSequence = ["it's", "a", "test"];
        outcomesSequence = [OTHER];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));

        // pos one, previous start
        inputSequence = ["Mike", "is", "here"];
        outcomesSequence = [START_A];
        ClassicAssert.IsTrue(validator.ValidSequence(1, inputSequence, outcomesSequence, outcome));

        // pos two, previous other
        inputSequence = ["it's", "a", "test"];
        outcomesSequence = [OTHER, OTHER];
        ClassicAssert.IsTrue(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));

        // pos two, previous start
        inputSequence = ["is", "Mike", "here"];
        outcomesSequence = [OTHER, START_A];
        ClassicAssert.IsTrue(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));

        // pos two, previous continue
        inputSequence = ["Stefanie", "Schmidt", "lives", "at", "home"];
        outcomesSequence = [START_A, CONTINUE_A];
        ClassicAssert.IsTrue(validator.ValidSequence(2, inputSequence, outcomesSequence, outcome));
    }
}
