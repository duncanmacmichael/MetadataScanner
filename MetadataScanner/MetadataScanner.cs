using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MetadataScanner
{
    class MetadataScanner
    {
        /// <summary>
        /// This program scans the YAML block of each Markdown file in a desired directory
        /// for a specified metadata token. If it is not found, the program will
        /// ask for a value to insert for that metadata, then either apply it
        /// to all files in the directory or go on a file-by-file basis depending
        /// on what the user wants to do.
        /// 
        /// The program will run forever unless the user closes the window or types "exit."
        /// 
        /// Acceptable metadata tokens are currently hard-coded and are based
        /// on valid metadata from existing ref and conceptual pages.
        /// </summary>
        /// <param name="args"></param>
        /// 
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the YAML metadata scanning tool. This tool searches for a metadata token in the YAML block of Markdown files and lets you do one of the following: \n" +
                              "\t1) Insert that token and a value at the end of the metadata block if the token does not exist in the file.\n" +
                              "\t2) Insert a new value for that token if the token exists in the file, but has no value.\n" +
                              "\t3) Update the value of the token if the token exists in the file and has a value.\n" +
                              "This tool scans a given directory and not its subdirectories, so the desired directory must be a bottom-level one.");
            Console.WriteLine();
            Console.WriteLine("Enter \"exit\" at any time to close the tool. You can also enter \"start over\" at any time to restart the tool.\n");
            Console.WriteLine();

            while (true)
            {
                //
                // Variables for each step in the algorithm
                //
                ActionType actionType = ActionType.None;
                bool isExiting = false, 
                     isStartingOver = false, 
                     isForReference = false,
                     sameMetadataForAllFiles = false,
                     skipThisFile = false;
                string directoryPath = "", 
                       desiredMetadataToken = "",
                       desiredMetadataValue = "",
                       metadataToInsert = "";
                List<string> validMetadataTokens = null;
                int numFilesModified = 0;

                //
                // Step 1
                // Ask the user what action they are doing for this pass:
                // update existing metadata, find empty metadata, or find
                // missing metadata.
                //
                AskUserForActionType(ref actionType,
                                     ref isExiting,
                                     ref isStartingOver);
                if (isExiting)
                {
                    return;
                }
                if (isStartingOver)
                {
                    Console.WriteLine("Starting over.");
                    continue;
                }
                if(ActionType.None == actionType)
                {
                    Console.WriteLine("Action type must be \"update,\" \"find empty,\" or \"find missing.\" Starting over.");
                }

                //
                // Step 2
                // Ask the user the directory that they'd like to scan
                //
                AskUserForDirectoryPath(ref directoryPath,
                                        ref isExiting,
                                        ref isStartingOver);
                if (isExiting)
                {
                    return;
                }
                if(isStartingOver)
                {
                    Console.WriteLine("Starting over.");
                    continue;
                }

                //
                // Step 3
                // Ask the user if they'd like to apply the same metadata to all
                // files in this directory. This only applies if the action type
                // is to find missing or empty metadata. If the user wants to
                // update metadata tokens with existing values then the tool
                // will apply to the change to all files automatically. The tool
                // is not designed to slog through files one by one and ask a 
                // user what they want to put for each one. If they want to 
                // update a single or only a few files, it would be faster to 
                // manually update the files separately.
                //
                if(actionType == ActionType.FindEmpty ||
                   actionType == ActionType.FindMissing)
                {
                    AskUserIfMetadataSameForAllFiles(ref sameMetadataForAllFiles,
                                                     ref isExiting,
                                                     ref isStartingOver);
                    if (isExiting)
                    {
                        return;
                    }
                    if (isStartingOver)
                    {
                        Console.WriteLine("Starting over.");
                        continue;
                    }
                }
                else
                {
                    sameMetadataForAllFiles = true;
                }
                

                //
                // Step 4
                // Ask the user if they are doing this for reference topics or
                // conceptual topics
                //
                AskUserIfReferenceOrConceptual(ref isForReference,
                                               ref isExiting,
                                               ref isStartingOver);
                if (isExiting)
                {
                    return;
                }
                if (isStartingOver)
                {
                    Console.WriteLine("Starting over.");
                    continue;
                }

                //
                // Step 5
                // Pre-generate a list of acceptable metadata tokens based on
                // whether this is for reference or conceptual
                //
                validMetadataTokens = HardCodeValidMetadataTokens(isForReference);
                if(null == validMetadataTokens)
                {
                    Console.WriteLine("Could not open the tokens file. " +
                                      "Please ensure that " +
                                      "MetadataTokensForConceptual.txt and " +
                                      "MetadataTokensForRef.txt are in the " +
                                      "same directory as this program. " +
                                      "Starting over.");
                    continue;
                }

                //
                // Step 6
                // Ask the user for the token they'd like to search for.
                //
                AskUserForMetadataToken(ref desiredMetadataToken,
                                         validMetadataTokens,
                                         ref isExiting,
                                         ref isStartingOver);
                if (isExiting)
                {
                    return;
                }
                if (isStartingOver)
                {
                    Console.WriteLine("Starting over.");
                    continue;
                }

                //
                // Step 7
                // If the metadata is to be applied for all files in this
                // directory, ask the user for the value now. Otherwise,
                // we will ask this on a per-file basis.
                //
                if (sameMetadataForAllFiles)
                {
                    AskUserForMetadataValue(desiredMetadataToken,
                                            ref desiredMetadataValue,
                                            ref isExiting,
                                            ref isStartingOver,
                                            false,
                                            ref skipThisFile);
                    if (isExiting)
                    {
                        return;
                    }
                    if (isStartingOver)
                    {
                        Console.WriteLine("Starting over.");
                        continue;
                    }

                    // Construct the full metadata line from the token and value
                    metadataToInsert = ConstructMetadataFromTokenAndValue(desiredMetadataToken,
                                                                          desiredMetadataValue);
                }

                //
                // Step 8
                // Iterate through each file to scan the YAML block and do the
                // search/insert/updates.
                //

                // Filter for only Markdown files and do not search subdirectories.
                foreach (string filePath in Directory.EnumerateFiles(directoryPath,
                                                                     "*.md",
                                                                     SearchOption.TopDirectoryOnly))
                {
                    // Ignore TOC and index files
                    string fileName = "";
                    if ((fileName = Path.GetFileName(filePath)) == "index.md" ||
                        fileName == "TOC.md")
                    {
                        Console.WriteLine($"Ignoring {fileName}.");
                        continue;
                    }

                    // Open a stream reader
                    StreamReader reader = null;
                    try
                    {
                        reader = File.OpenText(filePath);
                    }
                    catch
                    {
                        Console.WriteLine($"Could not open {filePath}.");
                    }
                    if (reader == null)
                    {
                        continue;
                    }

                    // Scan the YAML for the desired string
                    bool foundMetadata = false, metadataValueAlreadyPopulated = false;

                    string line = "", existingMetadataValue = "";
                    int numYamlDividingLinesSeen = 0, existingLineNumber = 0;

                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine();
                        existingLineNumber++;
                        if (line == "---")
                        {
                            numYamlDividingLinesSeen++;
                        }
                        if (numYamlDividingLinesSeen == 2)
                        {
                            break;
                        }
                        string[] words = line.Split(':');   // find token
                        if (words[0] == desiredMetadataToken)
                        {
                            foundMetadata = true;
                            if(words.Length > 1)
                            {
                                if(words[1].Trim().Length > 0)
                                {
                                    metadataValueAlreadyPopulated = true;
                                    existingMetadataValue = words[1].Trim();
                                }                                
                            }
                            break;
                        }
                    }
                    reader.Close();

                    // If action is "update", then proceed as long as the token is found
                    // and is already populated.
                    if((actionType == ActionType.Update && !foundMetadata) ||
                       (actionType == ActionType.Update && !metadataValueAlreadyPopulated))
                    {
                        Console.WriteLine($"Cannot update \"{desiredMetadataToken}\" in {fileName} because the file did not contain the token.");
                        continue;
                    }

                    // If action is "find empty", proceed only if token is found and it is
                    // not already populated.
                    if((actionType == ActionType.FindEmpty && !foundMetadata) ||
                       (actionType == ActionType.FindEmpty && metadataValueAlreadyPopulated))
                    {
                        Console.WriteLine($"\"{desiredMetadataToken}\" already existed in {fileName} or already had a value.");
                        continue;
                    }

                    // If action is "find missing", proceed only if token is not found at all.
                    if(actionType == ActionType.FindMissing && foundMetadata)
                    {
                        Console.WriteLine($"Found \"{desiredMetadataToken}\" in {fileName} with this value: {existingMetadataValue}.");
                        continue;
                    }


                    //
                    // Insert the desired metadata if it were not found. Not found
                    // means that either the metadata was missing entirely or it
                    // was blank. 
                    //
                    // If the metadata was not found, this code inserts the line
                    // at the end of the YAML block. If it were found but not
                    // populated, it replaces the line where it was originally
                    // found.
                    //

                    // If the user specified to perform inserting on
                    // individual files, ask now for this file.
                    if (!sameMetadataForAllFiles)
                    {
                        AskUserForMetadataValue(desiredMetadataToken,
                                                ref desiredMetadataValue,
                                                ref isExiting,
                                                ref isStartingOver,
                                                true,
                                                ref skipThisFile);
                        if (isExiting)
                        {
                            return;
                        }
                        if (isStartingOver)
                        {
                            Console.WriteLine("Starting over.");
                            break;
                        }
                        if(skipThisFile)
                        {
                            Console.WriteLine($"Skipping {fileName}.");
                            skipThisFile = false;   // Reset for next file
                            continue;
                        }

                        // Construct the full metadata line from the token and value
                        metadataToInsert = ConstructMetadataFromTokenAndValue(desiredMetadataToken,
                                                                                desiredMetadataValue);
                    }

                    //
                    // If the metadata were not found, insert at the end
                    // of the metadata block right before the second line.
                    // If it were found, insert it at the line where it
                    // occurred.
                    //
                    int indexAtWhichToInsert = 0;
                    if (!foundMetadata)
                    {
                        try
                        {
                            reader = File.OpenText(filePath);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine($"Could not open {filePath}. Exception: " + e.Message);
                            continue;
                        }
                        if (reader == null)
                        {
                            continue;
                        }


                        numYamlDividingLinesSeen = 0;   // reset for this file
                        while (!reader.EndOfStream)
                        {
                            line = reader.ReadLine();
                            if (line == "---")
                            {
                                numYamlDividingLinesSeen++;
                            }
                            if (numYamlDividingLinesSeen == 2)
                            {
                                // The end of the YAML block
                                break;
                            }
                            indexAtWhichToInsert++;
                        }
                        reader.Close();
                    }
                    else
                    {
                        indexAtWhichToInsert = existingLineNumber - 1;
                    }                        

                    // Insert the metadata. 
                    string[] lines = File.ReadAllLines(filePath);
                    using (StreamWriter streamWriter = new StreamWriter(filePath))
                    {
                        // Write original file up to line to replace
                        int i;
                        for (i = 0; i < indexAtWhichToInsert; i++)
                        {
                            streamWriter.WriteLine(lines[i]);
                        }
                        streamWriter.WriteLine(metadataToInsert);

                        // Skip the original line if replacing existing metadata value (empty or otherwise).
                        if (foundMetadata)
                        {
                            i = indexAtWhichToInsert + 1;
                        }
                        else
                        {
                            i = indexAtWhichToInsert;
                        }

                        // Write the rest of the file
                        for (; i < lines.Length; i++)
                        {
                            streamWriter.WriteLine(lines[i]);
                        }
                    }

                    Console.WriteLine($"Successfully inserted \"{metadataToInsert}\" " +
                                        $"into {fileName}.");
                    numFilesModified++;
                }

                if (numFilesModified > 0)
                {
                    Console.WriteLine($"Successfully modified {numFilesModified} files.\n");
                    numFilesModified = 0;
                }
                else
                {
                    Console.WriteLine("Didn't find any files to modify for " +
                                      $"\"{desiredMetadataToken}\".\n");
                }
            }
        }

        /// <summary>
        /// Generates a list of approved metadata tokens to use for reference
        /// topics. Hard-coded this way instead of loading from a file to avoid
        /// accidental modification of this list by a user.
        /// </summary>
        /// /// <param name="DirectoryPath">The path to the directory. Must be a bottom-level directory that contains the Markdown files to scan.</param>
        /// <returns>A list of valid metadata tokens for the given type of
        /// topic.</returns>
        private static List<string> HardCodeValidMetadataTokens(
            bool IsForReference
        )
        {
            List<string> tokens = new List<string>();

            StreamReader reader = null;
            string fileName = IsForReference ? "MetadataTokensForRef.txt" : "MetadataTokensForConceptual.txt";
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            try
            {
                reader = File.OpenText(filePath);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Could not open {filePath}. Exception: " + e.Message);
            }
            if(reader == null)
            {
                return null;
            }
            
            string line = "";
            while(!reader.EndOfStream)
            {
                line = reader.ReadLine();
                tokens.Add(line);
            }
            reader.Close();
            return tokens;
        }

        /// <summary>
        /// Asks the user for a directory path that contains Markdown files to
        /// scan for metadata in the YAML block.
        /// </summary>
        /// <param name="DirectoryPath">The path to the directory. Must be a bottom-level directory that contains the Markdown files to scan.</param>
        /// <param name="IsExiting">Indicates to the calling function that the user would like to exit the program.</param>
        /// <param name="IsStartingOver">Indicates to the calling function that the user would like to start the program over, but not exit.</param>
        private static void AskUserForActionType(
            ref ActionType DesiredAction,
            ref bool IsExiting,
            ref bool IsStartingOver
        )
        {
            Console.WriteLine("Would you like to update existing metadata, search for empty metadata, or search for missing metadata? " +
                              "Enter \"update,\" \"find empty,\" or \"find missing.\"");
            string action = Console.ReadLine();
            while (true)
            {
                if (action.ToLower() == "exit")
                {
                    IsExiting = true;
                    return;
                }
                if (action.ToLower() == "start over")
                {
                    IsStartingOver = true;
                    return;
                }
                if (action.ToLower() != "update" && 
                    action.ToLower() != "find empty" &&
                    action.ToLower() != "find missing")
                {
                    Console.WriteLine($"Please enter \"update,\" \"find empty,\" or \"find missing,\" or enter \"exit\" to quit.");
                    action = Console.ReadLine();
                    continue;
                }
                break;
            }
            if(action.ToLower() == "update")
            {
                DesiredAction = ActionType.Update;
            }
            else if(action.ToLower() == "find empty")
            {
                DesiredAction = ActionType.FindEmpty;
            }
            else if(action.ToLower() == "find missing")
            {
                DesiredAction = ActionType.FindMissing;
            }
            else
            {
                // Shouldn't ever get here based on constrained input above
                DesiredAction = ActionType.None;
            }
        }

        /// <summary>
        /// Asks the user for a directory path that contains Markdown files to
        /// scan for metadata in the YAML block.
        /// </summary>
        /// <param name="DirectoryPath">The path to the directory. Must be a bottom-level directory that contains the Markdown files to scan.</param>
        /// <param name="IsExiting">Indicates to the calling function that the user would like to exit the program.</param>
        /// <param name="IsStartingOver">Indicates to the calling function that the user would like to start the program over, but not exit.</param>
        private static void AskUserForDirectoryPath(
            ref string DirectoryPath,
            ref bool IsExiting,
            ref bool IsStartingOver
        )
        {
            Console.WriteLine("Please enter a directory you'd like to scan or " +
                                  "enter \"exit\" to quit.");
            DirectoryPath = Console.ReadLine();
            while (true)
            {
                if (DirectoryPath.ToLower() == "exit")
                {
                    IsExiting = true;
                    return;
                }
                if(DirectoryPath.ToLower() == "start over")
                {
                    IsStartingOver = true;
                    return;
                }
                if (!Directory.Exists(DirectoryPath))
                {
                    Console.WriteLine($"{DirectoryPath} does not exist on disk." +
                                " Please enter a valid directory or enter" +
                                " \"exit\" to quit.");
                    DirectoryPath = Console.ReadLine();
                    continue;
                }
                break;
            }
        }

        /// <summary>
        /// Asks the user if they would like to apply the same metadata to all files in this directory or not.
        /// </summary>
        /// <param name="SameMetadataForAllFiles">Indicates that the desired metadata should be applied to all files in a directory.</param>
        /// <param name="IsExiting">Indicates to the calling function that the user would like to exit the program.</param>
        /// /// <param name="IsStartingOver">Indicates to the calling function that the user would like to start the program over, but not exit.</param>
        private static void AskUserIfMetadataSameForAllFiles(
            ref bool SameMetadataForAllFiles,
            ref bool IsExiting,
            ref bool IsStartingOver
        )
        {
            Console.WriteLine("Would you like to search for/insert the same" +
                                  " metadata for all files in this directory? " +
                                  "Y/N");
            string yesOrNo = Console.ReadLine();
            while (true)
            {
                if (yesOrNo.ToLower() == "exit")
                {
                    IsExiting = true;
                    return;
                }
                if(yesOrNo.ToLower() == "start over")
                {
                    IsStartingOver = true;
                    return;
                }
                if (yesOrNo.ToLower() != "y" && 
                    yesOrNo.ToLower() != "n" &&
                    yesOrNo.ToLower() != "yes" &&
                    yesOrNo.ToLower() != "no")
                {
                    Console.WriteLine("Please enter yes (Y) or no (N) for " +
                                      "whether you would like to scan/insert" +
                                      " the same metadata for all files in " +
                                      "this directory.");
                    yesOrNo = Console.ReadLine();
                    continue;
                }
                break;
            }
            SameMetadataForAllFiles = yesOrNo.ToLower() == "y" || yesOrNo.ToLower() == "yes" ? true : false;
        }

        /// <summary>
        /// Asks the user if they are applying this metadata to reference or
        /// conceptual topics.
        /// </summary>
        /// <param name="IsForReference">Indicates that this is for reference topics.</param>
        /// <param name="IsExiting">Indicates to the calling function that the user would like to exit the program.</param>
        /// /// <param name="IsStartingOver">Indicates to the calling function that the user would like to start the program over, but not exit.</param>
        private static void AskUserIfReferenceOrConceptual(
            ref bool IsForReference,
            ref bool IsExiting,
            ref bool IsStartingOver
        )
        {
            Console.WriteLine("Is this for reference or conceptual topics? " +
                                  "This determines what metadata is valid for " +
                                  "input. Enter \"reference\" or \"conceptual.\"");
            string refOrConceptual = Console.ReadLine();
            while (true)
            {
                if (refOrConceptual.ToLower() == "exit")
                {
                    IsExiting = true;
                    return;
                }
                if(refOrConceptual.ToLower() == "start over")
                {
                    IsStartingOver = true;
                    return;
                }
                if (refOrConceptual.ToLower() != "reference" &&
                    refOrConceptual.ToLower() != "ref" &&
                    refOrConceptual.ToLower() != "conceptual")
                {
                    Console.WriteLine("Please enter \"reference\" or " +
                                      "\"conceptual\" for the type of " +
                                      "content you're scanning.");
                    refOrConceptual = Console.ReadLine();
                    continue;
                }
                break;
            }
            IsForReference = refOrConceptual.ToLower() == "reference"  || refOrConceptual.ToLower() == "ref" ? true : false;
        }

        /// <summary>
        /// Asks the user for a valid metadata token for which to scan in the 
        /// Markdown YAML blocks. Sees if a file has that field at all regardless
        /// of value following the metadata token.
        /// </summary>
        /// <param name="DesiredMetadataToken">The desired metadata token. For example, ms.author</param>
        /// <param name="ValidMetadataTypes">A list of valid metadata tokens.</param>
        /// <param name="IsExiting">Indicates to the calling function that the user would like to exit the program.</param>
        /// /// <param name="IsStartingOver">Indicates to the calling function that the user would like to start the program over, but not exit.</param>
        private static void AskUserForMetadataToken(
            ref string DesiredMetadataToken,
            List<string> ValidMetadataTokens,
            ref bool IsExiting,
            ref bool IsStartingOver
        )
        {
            Console.WriteLine("Enter the metadata token for which you'd like " +
                              "to scan, without the colon. For example, " +
                              "\"description.\"");
            DesiredMetadataToken = Console.ReadLine();
            while (true)
            {
                // User wants to exit
                if (DesiredMetadataToken.ToLower() == "exit")
                {
                    IsExiting = true;
                    return;
                }
                if(DesiredMetadataToken.ToLower() == "start over")
                {
                    IsStartingOver = true;
                    return;
                }

                // Invalid input: empty string or null
                if (DesiredMetadataToken == "")
                {
                    Console.WriteLine("Invalid metadata token. Please enter " +
                                      "a valid metadata token.");
                    DesiredMetadataToken = Console.ReadLine();
                    continue;
                }

                // Invalid input: token is not a supported one
                if (!ValidMetadataTokens.Contains(DesiredMetadataToken))
                {
                    Console.WriteLine("Invalid metadata token. Please enter " +
                                      "a valid metadata token.");
                    DesiredMetadataToken = Console.ReadLine();
                    continue;
                }
                break;
            }
        }

        /// <summary>
        /// Asks the user for a value to put for the metadata now that a file
        /// has been found that lacks it.
        /// </summary>
        /// <param name="DesiredMetadataToken">The desired metadata token. Previously acquired from a call to AskUserForMetadataToken(). For example, ms.author</param>
        /// <param name="DesiredMetadataValue">The value to insert for the given metadata token.</param>
        /// <param name="IsExiting">Indicates to the calling function that the user would like to exit the program.</param>
        /// <param name="IsStartingOver">Indicates to the calling function that the user would like to start the program over, but not exit.</param>
        /// <param name="AskForSkip">Specifies if this function should additionally ask the user if they would like to skip this file. Only used when scanning/inserting metadata on a file-by-file basis.</param>
        /// <param name="SkipThisFile">Indicates to the calling function that it should skip inserting metadata for this file. This parameter is only filled in by this function if AskForSkip is set to TRUE by the caller.</param>
        private static void AskUserForMetadataValue(
            string DesiredMetadataToken,
            ref string DesiredMetadataValue,
            ref bool IsExiting,
            ref bool IsStartingOver,
            bool AskForSkip,
            ref bool SkipThisFile
        )
        {
            Console.WriteLine($"Enter the value for {DesiredMetadataToken}. " +
                              "For lists, use commas to separate the values.");
            if(AskForSkip)
            {
                Console.WriteLine("To skip this file, enter \"skip\".");
            }
            DesiredMetadataValue = Console.ReadLine();
            while (true)
            {
                if (DesiredMetadataValue.ToLower() == "exit")
                {
                    IsExiting = true;
                    return;
                }
                if(DesiredMetadataValue.ToLower() == "start over")
                {
                    IsStartingOver = true;
                    return;
                }
                if(AskForSkip && DesiredMetadataValue.ToLower() == "skip")
                {
                    SkipThisFile = true;
                    return;
                }
                if (DesiredMetadataValue == "")
                {
                    Console.WriteLine("Invalid metadata value. Please enter a" +
                                      " valid metadata value.");
                    DesiredMetadataValue = Console.ReadLine();
                    continue;
                }

                // Perform other validation here if necessary; otherwise, take
                // whatever the user provides and break. No way to validate all
                // possible valid values for all metadata tokens.

                break;
            }
        }

        /// <summary>
        /// Constructs a proper metadata line from a given token and value.
        /// Checks if a token is a type that requires a list following it or
        /// not.
        /// </summary>
        /// <param name="Token">The metadata token.</param>
        /// <param name="Value">The metadata value.</param>
        /// <returns></returns>
        private static string ConstructMetadataFromTokenAndValue(
            string Token,
            string Value
        )
        {
            string metadata = "";

            // Construct a Markdown list of values for certain metadata tokens
            if (Token == "topic_type" || Token == "api_type" ||
                Token == "api_location" || Token == "api_name" ||
                Token == "product")
            {
                StringBuilder builder = new StringBuilder(Token);
                builder.Append(":\n");
                string[] values = Value.Split(',');

                foreach (string value in values)
                {
                    builder.Append("- ");
                    builder.Append(value);
                    if (value != values[values.Length - 1])
                    {
                        builder.Append("\n");
                    }
                }

                string metadataList = builder.ToString();
                metadata = metadataList;
            }
            else
            {
                metadata = Token + ": " + Value;
            }

            return metadata;
        }

        private enum ActionType
        {
            Update,
            FindEmpty,
            FindMissing,
            None
        }
    }
}
