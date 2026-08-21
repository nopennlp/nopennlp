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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Ml;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Model;

namespace NOpenNLP.Tools.Cmdline;

/// <summary>
/// Util class for simple file checks, listings and error handling.
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public static class CmdLineUtil
{
    public const int IoBufferSize = 1024 * 1024;

    /// <summary>
    /// Check that the given input file is valid.
    /// <para/>
    /// To pass the test it must:<br/>
    /// - exist<br/>
    /// - not be a directory<br/>
    /// - accessibly<br/>
    /// </summary>
    /// <param name="name">
    /// the name which is used to refer to the file in an error message, it should start
    /// with a capital letter.
    /// </param>
    /// <param name="inFile">the particular file to check to qualify an input file</param>
    /// <exception cref="TerminateToolException">
    /// if the test does not pass this exception is thrown and an error message is printed
    /// to the console.
    /// </exception>
    public static void CheckInputFile(string name, FileInfo inFile)
    {
        string? isFailure = null;

        // NOpenNLP: Java's File models a path that may be either kind, so isDirectory()
        // and exists() are both meaningful on one object. A FileInfo pointing at a
        // directory reports Exists == false, so the directory case is tested separately
        // to keep upstream's more specific message.
        if (Directory.Exists(inFile.FullName))
        {
            isFailure = "The " + name + " file is a directory!";
        }
        else if (!inFile.Exists)
        {
            isFailure = "The " + name + " file does not exist!";
        }
        else if (!CanRead(inFile))
        {
            isFailure = "No permissions to read the " + name + " file!";
        }

        if (null != isFailure)
        {
            throw new TerminateToolException(-1, isFailure + " Path: " + inFile.FullName);
        }
    }

    /// <summary>
    /// Tries to ensure that it is possible to write to an output file.
    /// <para/>
    /// The method does nothing if it is possible to write, otherwise it prints an
    /// appropriate error message and a <see cref="TerminateToolException"/> is thrown.
    /// <para/>
    /// Computing the contents of an output file (e.g. ME model) can be very time
    /// consuming. Prior to this computation it should be checked once that writing this
    /// output file is possible to be able to fail fast if not.
    /// </summary>
    /// <param name="name">human-friendly file name, for example perceptron model</param>
    /// <param name="outFile">the file</param>
    public static void CheckOutputFile(string name, FileInfo outFile)
    {
        string? isFailure = null;

        if (Directory.Exists(outFile.FullName))
        {
            isFailure = "The " + name + " file is a directory!";
        }
        else if (outFile.Exists)
        {
            // The file already exists, ensure that it is possible to write into it.
            if (!CanWrite(outFile))
            {
                isFailure = "No permissions to write the " + name + " file!";
            }
        }
        else
        {
            // The file does not exist, ensure its parent directory exists and has write
            // permissions to create a new file in it.
            DirectoryInfo? parentDir = outFile.Directory;

            if (parentDir != null && parentDir.Exists)
            {
                if (!CanWriteDirectory(parentDir))
                {
                    isFailure = "No permissions to create the " + name + " file!";
                }
            }
            else
            {
                isFailure = "The parent directory of the " + name + " file does not exist, " +
                    "please create it first!";
            }
        }

        if (null != isFailure)
        {
            throw new TerminateToolException(-1, isFailure + " Path: " + outFile.FullName);
        }
    }

    // NOpenNLP: Java's File.canRead()/canWrite() ask the filesystem directly. .NET has no
    // equivalent that is meaningful across platforms, so these probe by opening, which is
    // what the caller is about to do anyway and is the only answer that does not race.
    private static bool CanRead(FileInfo file)
    {
        try
        {
            using (file.OpenRead())
            {
                return true;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool CanWrite(FileInfo file)
    {
        try
        {
            using (file.Open(FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                return true;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool CanWriteDirectory(DirectoryInfo directory)
    {
        string probe = Path.Combine(directory.FullName, Path.GetRandomFileName());

        try
        {
            using (File.Create(probe, 1, FileOptions.DeleteOnClose))
            {
                return true;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static Stream OpenInFile(FileInfo file)
    {
        try
        {
            return file.OpenRead();
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new TerminateToolException(-1, "File '" + file + "' cannot be found", e);
        }
    }

    public static IInputStreamFactory CreateInputStreamFactory(FileInfo file)
    {
        try
        {
            return new MarkableFileInputStreamFactory(file);
        }
        catch (FileNotFoundException e)
        {
            throw new TerminateToolException(-1, "File '" + file + "' cannot be found", e);
        }
    }

    /// <summary>
    /// Writes a <see cref="BaseModel"/> to disk. Occurring errors are printed to the
    /// console to inform the user.
    /// </summary>
    /// <param name="modelName">type of the model, name is used in error messages.</param>
    /// <param name="modelFile">output file of the model</param>
    /// <param name="model">the model itself which should be written to disk</param>
    public static void WriteModel(string modelName, FileInfo modelFile, BaseModel model)
    {
        CheckOutputFile(modelName + " model", modelFile);

        Console.Error.Write("Writing " + modelName + " model ... ");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using Stream modelOut = new BufferedStream(modelFile.Create(), IoBufferSize);
            model.Serialize(modelOut);
        }
        catch (IOException e)
        {
            Console.Error.WriteLine("failed");
            throw new TerminateToolException(-1,
                "Error during writing model file '" + modelFile + "'", e);
        }

        stopwatch.Stop();

        Console.Error.Write(string.Format(CultureInfo.InvariantCulture,
            "done ({0:F3}s)\n", stopwatch.Elapsed.TotalSeconds));

        Console.Error.WriteLine();

        Console.Error.WriteLine("Wrote " + modelName + " model to");
        Console.Error.WriteLine("path: " + modelFile.FullName);

        Console.Error.WriteLine();
    }

    /// <summary>
    /// Returns the index of the parameter in the arguments, or -1 if the parameter is
    /// not found.
    /// </summary>
    public static int GetParameterIndex(string param, string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("-", StringComparison.Ordinal)
                && args[i].Equals(param, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Retrieves the specified parameter from the given arguments.
    /// </summary>
    public static string? GetParameter(string param, string[] args)
    {
        int i = GetParameterIndex(param, args);
        if (-1 < i)
        {
            i++;
            if (i < args.Length)
            {
                return args[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Retrieves the specified parameter from the specified arguments.
    /// </summary>
    public static int? GetIntParameter(string param, string[] args)
    {
        string? value = GetParameter(param, args);

        // NOpenNLP: upstream catches NumberFormatException and returns null;
        // TryParse says the same thing without the exception. Invariant culture matches
        // Integer.parseInt, which is not locale sensitive.
        if (value != null && int.TryParse(value, NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Retrieves the specified parameter from the specified arguments.
    /// </summary>
    public static double? GetDoubleParameter(string param, string[] args)
    {
        string? value = GetParameter(param, args);

        if (value != null && double.TryParse(value, NumberStyles.Float,
            CultureInfo.InvariantCulture, out double result))
        {
            return result;
        }

        return null;
    }

    public static void CheckLanguageCode(string code)
    {
        // NOpenNLP: upstream uses Locale.getISOLanguages(). The .NET counterpart is the
        // two-letter ISO name of every specific culture; ICU supplies the same ISO 639-1
        // set. "x-unspecified" is added exactly as upstream does.
        var languageCodes = new HashSet<string>(
            System.Globalization.CultureInfo
                .GetCultures(CultureTypes.NeutralCultures)
                .Select(c => c.TwoLetterISOLanguageName)
                .Where(n => n.Length == 2),
            StringComparer.Ordinal)
        {
            "x-unspecified",
        };

        if (!languageCodes.Contains(code))
        {
            throw new TerminateToolException(1, "Unknown language code " + code + ", " +
                "must be an ISO 639 code!");
        }
    }

    public static bool ContainsParam(string param, string[] args) =>
        args.Any(arg => arg.Equals(param, StringComparison.Ordinal));

    public static void HandleStdinIoError(IOException e) =>
        throw new TerminateToolException(-1,
            "IO Error while reading from stdin: " + e.Message, e);

    public static TerminateToolException CreateObjectStreamError(IOException e) =>
        new TerminateToolException(-1,
            "IO Error while creating an Input Stream: " + e.Message, e);

    public static void HandleCreateObjectStreamError(IOException e) =>
        throw CreateObjectStreamError(e);

    /// <summary>
    /// Loads the training parameters from <paramref name="paramFile"/>, which is
    /// optional: passing <c>null</c> is allowed and yields <c>null</c>.
    /// </summary>
    public static TrainingParameters? LoadTrainingParameters(string? paramFile,
        bool supportSequenceTraining)
    {
        TrainingParameters? parameters = null;

        if (paramFile != null)
        {
            CheckInputFile("Training Parameter", new FileInfo(paramFile));

            try
            {
                using Stream paramsIn = new FileInfo(paramFile).OpenRead();
                parameters = new TrainingParameters(paramsIn);
            }
            catch (IOException e)
            {
                throw new TerminateToolException(-1,
                    "Error during parameters loading: " + e.Message, e);
            }

            if (!TrainerFactory.IsValid(parameters))
            {
                throw new TerminateToolException(1,
                    "Training parameters file '" + paramFile + "' is invalid!");
            }

            TrainerFactory.TrainerType? trainerType = TrainerFactory.GetTrainerType(parameters);

            if (!supportSequenceTraining
                && trainerType == TrainerFactory.TrainerType.EVENT_MODEL_SEQUENCE_TRAINER)
            {
                throw new TerminateToolException(1, "Sequence training is not supported!");
            }
        }

        return parameters;
    }
}
