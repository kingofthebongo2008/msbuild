﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

using NUnit.Framework;

using Microsoft.Build.Shared;

namespace Microsoft.Build.UnitTests
{
    [TestFixture]
    public class FileMatcherTest
    {
        /*
         * Method:  GetFileSystemEntries
         * 
         * Simulate Directories.GetFileSystemEntries where file names are short.
         * 
         */
        private static string[] GetFileSystemEntries(FileMatcher.FileSystemEntity entityType, string path, string pattern, string projectDirectory, bool stripProjectDirectory)
        {
            if
            (
                pattern == @"LONGDI~1"
                && (@"D:\" == path || @"\\server\share\" == path || path.Length == 0)
            )
            {
                return new string[] { Path.Combine(path, "LongDirectoryName") };
            }
            else if
            (
                pattern == @"LONGSU~1"
                && (@"D:\LongDirectoryName" == path || @"\\server\share\LongDirectoryName" == path || @"LongDirectoryName" == path)
            )
            {
                return new string[] { Path.Combine(path, "LongSubDirectory") };
            }
            else if
            (
                pattern == @"LONGFI~1.TXT"
                && (@"D:\LongDirectoryName\LongSubDirectory" == path || @"\\server\share\LongDirectoryName\LongSubDirectory" == path || @"LongDirectoryName\LongSubDirectory" == path)
            )
            {
                return new string[] { Path.Combine(path, "LongFileName.txt") };
            }
            else if
            (
                pattern == @"pomegr~1"
                && @"c:\apple\banana\tomato" == path
            )
            {
                return new string[] { Path.Combine(path, "pomegranate") };
            }
            else if
            (
                @"c:\apple\banana\tomato\pomegranate\orange" == path
            )
            {
                // No files exist here. This is an empty directory.
                return new string[0];
            }
            else
            {
                Console.WriteLine("GetFileSystemEntries('{0}', '{1}')", path, pattern);
                Assert.Fail("Unexpected input into GetFileSystemEntries");
            }
            return new string[] { "<undefined>" };
        }

        /// <summary>
        /// Simple test of the MatchDriver code.
        /// </summary>
        [Test]
        public void BasicMatchDriver()
        {
            MatchDriver
            (
                "Source" + Path.DirectorySeparatorChar + "**",
                new string[]  // Files that exist and should match.
                {
                    "Source" + Path.DirectorySeparatorChar + "Bart.txt",
                    "Source" + Path.DirectorySeparatorChar + "Sub" + Path.DirectorySeparatorChar + "Homer.txt",
                },
                new string[]  // Files that exist and should not match.
                {
                    "Destination" + Path.DirectorySeparatorChar + "Bart.txt",
                    "Destination" + Path.DirectorySeparatorChar + "Sub" + Path.DirectorySeparatorChar + "Homer.txt",
                },
                null
            );
        }

        /// <summary>
        /// This pattern should *not* recurse indefinitely since there is no '**' in the pattern:
        /// 
        ///        c:\?emp\foo
        /// 
        /// </summary>
        [Test]
        public void Regress162390()
        {
            MatchDriver
            (
                @"c:\?emp\foo.txt",
                new string[] { @"c:\temp\foo.txt" },    // Should match
                new string[] { @"c:\timp\foo.txt" },    // Shouldn't match
                new string[]                            // Should not even consider.
                {
                    @"c:\temp\sub\foo.txt"
                }
            );
        }


        /*
        * Method:  GetLongFileNameForShortLocalPath
        * 
        * Convert a short local path to a long path.
        * 
        */
        [Test]
        public void GetLongFileNameForShortLocalPath()
        {
            if (!NativeMethodsShared.IsWindows)
            {
                Assert.Ignore("Short names are for Windows only");
            }

            string longPath = FileMatcher.GetLongPathName
            (
                @"D:\LONGDI~1\LONGSU~1\LONGFI~1.TXT",
                new FileMatcher.GetFileSystemEntries(FileMatcherTest.GetFileSystemEntries)
            );

            Assert.AreEqual(longPath, @"D:\LongDirectoryName\LongSubDirectory\LongFileName.txt");
        }

        /*
        * Method:  GetLongFileNameForLongLocalPath
        * 
        * Convert a long local path to a long path (nop).
        * 
        */
        [Test]
        public void GetLongFileNameForLongLocalPath()
        {
            string longPath = FileMatcher.GetLongPathName
            (
                @"D:\LongDirectoryName\LongSubDirectory\LongFileName.txt",
                new FileMatcher.GetFileSystemEntries(FileMatcherTest.GetFileSystemEntries)
            );

            Assert.AreEqual(longPath, @"D:\LongDirectoryName\LongSubDirectory\LongFileName.txt");
        }

        /*
        * Method:  GetLongFileNameForShortUncPath
        * 
        * Convert a short UNC path to a long path.
        * 
        */
        [Test]
        public void GetLongFileNameForShortUncPath()
        {
            if (!NativeMethodsShared.IsWindows)
            {
                Assert.Ignore("Short names are for Windows only");
            }

            string longPath = FileMatcher.GetLongPathName
            (
                @"\\server\share\LONGDI~1\LONGSU~1\LONGFI~1.TXT",
                new FileMatcher.GetFileSystemEntries(FileMatcherTest.GetFileSystemEntries)
            );

            Assert.AreEqual(longPath, @"\\server\share\LongDirectoryName\LongSubDirectory\LongFileName.txt");
        }

        /*
        * Method:  GetLongFileNameForLongUncPath
        * 
        * Convert a long UNC path to a long path (nop)
        * 
        */
        [Test]
        public void GetLongFileNameForLongUncPath()
        {
            string longPath = FileMatcher.GetLongPathName
            (
                @"\\server\share\LongDirectoryName\LongSubDirectory\LongFileName.txt",
                new FileMatcher.GetFileSystemEntries(FileMatcherTest.GetFileSystemEntries)
            );

            Assert.AreEqual(longPath, @"\\server\share\LongDirectoryName\LongSubDirectory\LongFileName.txt");
        }

        /*
        * Method:  GetLongFileNameForRelativePath
        * 
        * Convert a short relative path to a long path
        * 
        */
        [Test]
        public void GetLongFileNameForRelativePath()
        {
            if (!NativeMethodsShared.IsWindows)
            {
                Assert.Ignore("Short names are for Windows only");
            }

            string longPath = FileMatcher.GetLongPathName
            (
                @"LONGDI~1\LONGSU~1\LONGFI~1.TXT",
                new FileMatcher.GetFileSystemEntries(FileMatcherTest.GetFileSystemEntries)
            );

            Assert.AreEqual(longPath, @"LongDirectoryName\LongSubDirectory\LongFileName.txt");
        }

        /*
        * Method:  GetLongFileNameForRelativePathPreservesTrailingSlash
        * 
        * Convert a short relative path with a trailing backslash to a long path
        * 
        */
        [Test]
        public void GetLongFileNameForRelativePathPreservesTrailingSlash()
        {
            if (!NativeMethodsShared.IsWindows)
            {
                Assert.Ignore("Short names are for Windows only");
            }

            string longPath = FileMatcher.GetLongPathName
            (
                @"LONGDI~1\LONGSU~1\",
                new FileMatcher.GetFileSystemEntries(FileMatcherTest.GetFileSystemEntries)
            );

            Assert.AreEqual(@"LongDirectoryName\LongSubDirectory\", longPath);
        }

        /*
        * Method:  GetLongFileNameForRelativePathPreservesExtraSlashes
        * 
        * Convert a short relative path with doubled embedded backslashes to a long path
        * 
        */
        [Test]
        public void GetLongFileNameForRelativePathPreservesExtraSlashes()
        {
            if (!NativeMethodsShared.IsWindows)
            {
                Assert.Ignore("Short names are for Windows only");
            }

            string longPath = FileMatcher.GetLongPathName
            (
                @"LONGDI~1\\LONGSU~1\\",
                new FileMatcher.GetFileSystemEntries(FileMatcherTest.GetFileSystemEntries)
            );

            Assert.AreEqual(@"LongDirectoryName\\LongSubDirectory\\", longPath);
        }

        /*
        * Method:  GetLongFileNameForMixedLongAndShort
        * 
        * Only part of the path might be short.
        * 
        */
        [Test]
        public void GetLongFileNameForMixedLongAndShort()
        {
            if (!NativeMethodsShared.IsWindows)
            {
                Assert.Ignore("Short names are for Windows only");
            }

            string longPath = FileMatcher.GetLongPathName
            (
                @"c:\apple\banana\tomato\pomegr~1\orange\",
                new FileMatcher.GetFileSystemEntries(FileMatcherTest.GetFileSystemEntries)
            );

            Assert.AreEqual(@"c:\apple\banana\tomato\pomegranate\orange\", longPath);
        }

        /*
        * Method:  GetLongFileNameWherePartOfThePathDoesntExist
        * 
        * Part of the path may not exist. In this case, we treat the non-existent parts
        * as if they were already a long file name.
        * 
        */
        [Test]
        public void GetLongFileNameWherePartOfThePathDoesntExist()
        {
            if (!NativeMethodsShared.IsWindows)
            {
                Assert.Ignore("Short names are for Windows only");
            }

            string longPath = FileMatcher.GetLongPathName
            (
                @"c:\apple\banana\tomato\pomegr~1\orange\chocol~1\vanila~1",
                new FileMatcher.GetFileSystemEntries(FileMatcherTest.GetFileSystemEntries)
            );

            Assert.AreEqual(@"c:\apple\banana\tomato\pomegranate\orange\chocol~1\vanila~1", longPath);
        }

        [Test]
        public void BasicMatch()
        {
            ValidateFileMatch("file.txt", "File.txt", false);
            ValidateNoFileMatch("file.txt", "File.bin", false);
        }

        [Test]
        public void MatchSingleCharacter()
        {
            ValidateFileMatch("file.?xt", "File.txt", false);
            ValidateNoFileMatch("file.?xt", "File.bin", false);
        }

        [Test]
        public void MatchMultipleCharacters()
        {
            ValidateFileMatch("*.txt", "*.txt", false);
            ValidateNoFileMatch("*.txt", "*.bin", false);
        }

        [Test]
        public void SimpleRecursive()
        {
            ValidateFileMatch("**", ".\\File.txt", true);
        }

        [Test]
        public void DotForCurrentDirectory()
        {
            ValidateFileMatch(".\\file.txt", ".\\File.txt", false);
            ValidateNoFileMatch(".\\file.txt", ".\\File.bin", false);
        }

        [Test]
        public void DotDotForParentDirectory()
        {
            ValidateFileMatch("..\\..\\*.*", "..\\..\\File.txt", false);
            ValidateFileMatch("..\\..\\*.*", "..\\..\\File", false);
            ValidateNoFileMatch("..\\..\\*.*", "..\\..\\dir1\\dir2\\File.txt", false);
            ValidateNoFileMatch("..\\..\\*.*", "..\\..\\dir1\\dir2\\File", false);
        }

        [Test]
        public void ReduceDoubleSlashesBaseline()
        {
            // Baseline
            ValidateFileMatch("f:\\dir1\\dir2\\file.txt", "f:\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch("**\\*.cs", "dir1\\dir2\\file.cs", true);
            ValidateFileMatch("**\\*.cs", "file.cs", true);
        }


        [Test]
        public void ReduceDoubleSlashes()
        {
            ValidateFileMatch("f:\\\\dir1\\dir2\\file.txt", "f:\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch("f:\\\\dir1\\\\\\dir2\\file.txt", "f:\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch("f:\\\\dir1\\\\\\dir2\\\\\\\\\\file.txt", "f:\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch("..\\**/\\*.cs", "..\\dir1\\dir2\\file.cs", true);
            ValidateFileMatch("..\\**/.\\*.cs", "..\\dir1\\dir2\\file.cs", true);
            ValidateFileMatch("..\\**\\./.\\*.cs", "..\\dir1\\dir2\\file.cs", true);
        }

        [Test]
        public void DoubleSlashesOnBothSidesOfComparison()
        {
            ValidateFileMatch("f:\\\\dir1\\dir2\\file.txt", "f:\\\\dir1\\dir2\\file.txt", false, false);
            ValidateFileMatch("f:\\\\dir1\\\\\\dir2\\file.txt", "f:\\\\dir1\\\\\\dir2\\file.txt", false, false);
            ValidateFileMatch("f:\\\\dir1\\\\\\dir2\\\\\\\\\\file.txt", "f:\\\\dir1\\\\\\dir2\\\\\\\\\\file.txt", false, false);
            ValidateFileMatch("..\\**/\\*.cs", "..\\dir1\\dir2\\\\file.cs", true, false);
            ValidateFileMatch("..\\**/.\\*.cs", "..\\dir1\\dir2//\\file.cs", true, false);
            ValidateFileMatch("..\\**\\./.\\*.cs", "..\\dir1/\\/\\/dir2\\file.cs", true, false);
        }

        [Test]
        public void DecomposeDotSlash()
        {
            ValidateFileMatch("f:\\.\\dir1\\dir2\\file.txt", "f:\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch("f:\\dir1\\.\\dir2\\file.txt", "f:\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch("f:\\dir1\\dir2\\.\\file.txt", "f:\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch("f:\\.//dir1\\dir2\\file.txt", "f:\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch("f:\\dir1\\.//dir2\\file.txt", "f:\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch("f:\\dir1\\dir2\\.//file.txt", "f:\\dir1\\dir2\\file.txt", false);

            ValidateFileMatch(".\\dir1\\dir2\\file.txt", ".\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch(".\\.\\dir1\\dir2\\file.txt", ".\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch(".//dir1\\dir2\\file.txt", ".\\dir1\\dir2\\file.txt", false);
            ValidateFileMatch(".//.//dir1\\dir2\\file.txt", ".\\dir1\\dir2\\file.txt", false);
        }

        [Test]
        public void RecursiveDirRecursive()
        {
            // Check that a wildcardpath of **\x\**\ matches correctly since, \**\ is a 
            // separate code path.
            ValidateFileMatch(@"c:\foo\**\x\**\*.*", @"c:\foo\x\file.txt", true);
            ValidateFileMatch(@"c:\foo\**\x\**\*.*", @"c:\foo\y\x\file.txt", true);
            ValidateFileMatch(@"c:\foo\**\x\**\*.*", @"c:\foo\x\y\file.txt", true);
            ValidateFileMatch(@"c:\foo\**\x\**\*.*", @"c:\foo\y\x\y\file.txt", true);
            ValidateFileMatch(@"c:\foo\**\x\**\*.*", @"c:\foo\x\x\file.txt", true);
            ValidateFileMatch(@"c:\foo\**\x\**\*.*", @"c:\foo\x\x\file.txt", true);
            ValidateFileMatch(@"c:\foo\**\x\**\*.*", @"c:\foo\x\x\x\file.txt", true);
        }

        [Test]
        public void Regress155731()
        {
            ValidateFileMatch(@"a\b\**\**\**\**\**\e\*", @"a\b\c\d\e\f.txt", true);
            ValidateFileMatch(@"a\b\**\e\*", @"a\b\c\d\e\f.txt", true);
            ValidateFileMatch(@"a\b\**\**\e\*", @"a\b\c\d\e\f.txt", true);
            ValidateFileMatch(@"a\b\**\**\**\e\*", @"a\b\c\d\e\f.txt", true);
            ValidateFileMatch(@"a\b\**\**\**\**\e\*", @"a\b\c\d\e\f.txt", true);
        }

        [Test]
        public void ParentWithoutSlash()
        {
            // However, we don't wtool this to match,
            ValidateNoFileMatch(@"C:\foo\**", @"C:\foo", true);
            // becase we don't know whether foo is a file or folder.

            // Same for UNC
            ValidateNoFileMatch
                (
                "\\\\server\\c$\\Documents and Settings\\User\\**",
                "\\\\server\\c$\\Documents and Settings\\User",
                true
                );
        }

        [Test]
        public void Unc()
        {
            // Check UNC functionality
            ValidateFileMatch
                (
                "\\\\server\\c$\\**\\*.cs",
                "\\\\server\\c$\\Documents and Settings\\User\\Source.cs",
                true
                );

            ValidateNoFileMatch
                (
                "\\\\server\\c$\\**\\*.cs",
                "\\\\server\\c$\\Documents and Settings\\User\\Source.txt",
                true
                );
            ValidateFileMatch
                (
                "\\\\**",
                "\\\\server\\c$\\Documents and Settings\\User\\Source.cs",
                true
                );
            ValidateFileMatch
                (
                "\\\\**\\*.*",
                "\\\\server\\c$\\Documents and Settings\\User\\Source.cs",
                true
                );


            ValidateFileMatch
                (
                "**",
                "\\\\server\\c$\\Documents and Settings\\User\\Source.cs",
                true
                );
        }

        [Test]
        public void ExplicitToolCompatibility()
        {
            // Explicit ANT compatibility. These patterns taken from the ANT documentation.
            ValidateFileMatch("**/SourceSafe/*", "./SourceSafe/Repository", true);
            ValidateFileMatch("**\\SourceSafe/*", "./SourceSafe/Repository", true);
            ValidateFileMatch("**/SourceSafe/*", ".\\SourceSafe\\Repository", true);
            ValidateFileMatch("**/SourceSafe/*", "./org/IIS/SourceSafe/Entries", true);
            ValidateFileMatch("**/SourceSafe/*", "./org/IIS/pluggin/tools/tool/SourceSafe/Entries", true);
            ValidateNoFileMatch("**/SourceSafe/*", "./org/IIS/SourceSafe/foo/bar/Entries", true);
            ValidateNoFileMatch("**/SourceSafe/*", "./SourceSafeRepository", true);
            ValidateNoFileMatch("**/SourceSafe/*", "./aSourceSafe/Repository", true);

            ValidateFileMatch("org/IIS/pluggin/**", "org/IIS/pluggin/tools/tool/docs/index.html", true);
            ValidateFileMatch("org/IIS/pluggin/**", "org/IIS/pluggin/test.xml", true);
            ValidateFileMatch("org/IIS/pluggin/**", "org/IIS/pluggin\\test.xml", true);
            ValidateNoFileMatch("org/IIS/pluggin/**", "org/IIS/abc.cs", true);

            ValidateFileMatch("org/IIS/**/SourceSafe/*", "org/IIS/SourceSafe/Entries", true);
            ValidateFileMatch("org/IIS/**/SourceSafe/*", "org\\IIS/SourceSafe/Entries", true);
            ValidateFileMatch("org/IIS/**/SourceSafe/*", "org/IIS\\SourceSafe/Entries", true);
            ValidateFileMatch("org/IIS/**/SourceSafe/*", "org/IIS/pluggin/tools/tool/SourceSafe/Entries", true);
            ValidateNoFileMatch("org/IIS/**/SourceSafe/*", "org/IIS/SourceSafe/foo/bar/Entries", true);
            ValidateNoFileMatch("org/IIS/**/SourceSafe/*", "org/IISSourceSage/Entries", true);
        }

        [Test]
        public void ExplicitToolIncompatibility()
        {
            // NOTE: Weirdly, ANT syntax is to match a file here.
            // We don't because MSBuild philosophy is that a trailing slash indicates a directory
            ValidateNoFileMatch("**/test/**", ".\\test", true);

            // NOTE: We deviate from ANT format here. ANT would append a ** to any path
            // that ends with '/' or '\'. We think this is the wrong thing because 'folder\'
            // is a valid folder name.
            ValidateNoFileMatch("org/", "org/IISSourceSage/Entries", false);
            ValidateNoFileMatch("org\\", "org/IISSourceSage/Entries", false);
        }

        [Test]
        public void MultipleStarStar()
        {
            // Multiple-** matches 
            ValidateFileMatch("c:\\**\\user\\**\\*.*", "c:\\Documents and Settings\\user\\NTUSER.DAT", true);
            ValidateNoFileMatch("c:\\**\\user1\\**\\*.*", "c:\\Documents and Settings\\user\\NTUSER.DAT", true);
            ValidateFileMatch("c:\\**\\user\\**\\*.*", "c://Documents and Settings\\user\\NTUSER.DAT", true);
            ValidateNoFileMatch("c:\\**\\user1\\**\\*.*", "c:\\Documents and Settings//user\\NTUSER.DAT", true);
        }

        [Test]
        public void RegressItemRecursionWorksAsExpected()
        {
            // Regress bug#54411:  Item recursion doesn't work as expected on "c:\foo\**"
            ValidateFileMatch("c:\\foo\\**", "c:\\foo\\two\\subfile.txt", true);
        }

        [Test]
        public void IllegalPaths()
        {
            // Certain patterns are illegal.
            ValidateIllegal("**.cs");
            ValidateIllegal("***");
            ValidateIllegal("****");
            ValidateIllegal("*.cs**");
            ValidateIllegal("*.cs**");
            ValidateIllegal("...\\*.cs");
            ValidateIllegal("http://www.website.com");
            ValidateIllegal("<:tag:>");
            ValidateIllegal("<:\\**");
        }

        [Test]
        public void IllegalTooLongPath()
        {
            string longString = new string('X', 500) + "*"; // need a wildcard to do anything
            string[] result = FileMatcher.GetFiles(@"c:\", longString);

            Assert.AreEqual(longString, result[0]); // Does not throw

            // Not checking that GetFileSpecInfo returns the illegal-path flag,
            // not certain that won't break something; this fix is merely to avoid a crash.
        }

        [Test]
        public void SplitFileSpec()
        {
            /*************************************************************************************
            * Call ValidateSplitFileSpec with various supported combinations.
            *************************************************************************************/
            ValidateSplitFileSpec("foo.cs", "", "", "foo.cs");
            ValidateSplitFileSpec("**\\foo.cs", "", "**\\", "foo.cs");
            ValidateSplitFileSpec("f:\\dir1\\**\\foo.cs", "f:\\dir1\\", "**\\", "foo.cs");
            ValidateSplitFileSpec("..\\**\\foo.cs", "..\\", "**\\", "foo.cs");
            ValidateSplitFileSpec("f:\\dir1\\foo.cs", "f:\\dir1\\", "", "foo.cs");
            ValidateSplitFileSpec("f:\\dir?\\foo.cs", "f:\\", "dir?\\", "foo.cs");
            ValidateSplitFileSpec("dir?\\foo.cs", "", "dir?\\", "foo.cs");
            ValidateSplitFileSpec(@"**\test\**", "", @"**\test\**\", "*.*");
        }

        [Test]
        public void Regress367780_CrashOnStarDotDot()
        {
            string workingPath = Path.Combine(Path.GetTempPath(), "Regress367780");
            string workingPathSubfolder = Path.Combine(workingPath, "SubDir");
            string offendingPattern = Path.Combine(workingPath, @"*\..\bar");
            string[] files = new string[0];

            try
            {
                Directory.CreateDirectory(workingPath);
                Directory.CreateDirectory(workingPathSubfolder);

                files = FileMatcher.GetFiles(workingPath, offendingPattern);
            }
            finally
            {
                Directory.Delete(workingPathSubfolder);
                Directory.Delete(workingPath);
            }
        }

        [Test]
        public void Regress141071_StarStarSlashStarStarIsLiteral()
        {
            string workingPath = Path.Combine(Path.GetTempPath(), "Regress141071");
            string fileName = Path.Combine(workingPath, "MyFile.txt");
            string offendingPattern = Path.Combine(workingPath, @"**\**");

            string[] files = new string[0];

            try
            {
                Directory.CreateDirectory(workingPath);
                File.WriteAllText(fileName, "Hello there.");
                files = FileMatcher.GetFiles(workingPath, offendingPattern);
            }
            finally
            {
                File.Delete(fileName);
                Directory.Delete(workingPath);
            }

            string result = String.Join(", ", files);
            Console.WriteLine(result);
            Assert.IsFalse(result.Contains("**"));
            Assert.IsTrue(result.Contains("MyFile.txt"));
        }

        [Test]
        public void Regress14090_TrailingDotMatchesNoExtension()
        {
            string workingPath = Path.Combine(Path.GetTempPath(), "Regress141071");
            string workingPathSubdir = Path.Combine(workingPath, "subdir");
            string workingPathSubdirBing = Path.Combine(workingPathSubdir, "bing");

            string offendingPattern = Path.Combine(workingPath, @"**\sub*\*.");

            string[] files = new string[0];

            try
            {
                Directory.CreateDirectory(workingPath);
                Directory.CreateDirectory(workingPathSubdir);
                File.AppendAllText(workingPathSubdirBing, "y");
                files = FileMatcher.GetFiles(workingPath, offendingPattern);
            }
            finally
            {
                Directory.Delete(workingPath, true);
            }

            string result = String.Join(", ", files);
            Console.WriteLine(result);
            Assert.AreEqual(1, files.Length);
        }

        [Test]
        public void Regress14090_TrailingDotMatchesNoExtension_Part2()
        {
            ValidateFileMatch(@"c:\mydir\**\*.", @"c:\mydir\subdir\bing", true, /* simulate filesystem? */ false);
            ValidateNoFileMatch(@"c:\mydir\**\*.", @"c:\mydir\subdir\bing.txt", true);
        }

        [Test]
        public void RemoveProjectDirectory()
        {
            string[] strings = new string[1] { "c:\\1.file" };
            FileMatcher.RemoveProjectDirectory(strings, "c:\\");
            Assert.AreEqual(strings[0], "1.file");

            strings = new string[1] { "c:\\directory\\1.file" };
            FileMatcher.RemoveProjectDirectory(strings, "c:\\");
            Assert.AreEqual(strings[0], "directory\\1.file");

            strings = new string[1] { "c:\\directory\\1.file" };
            FileMatcher.RemoveProjectDirectory(strings, "c:\\directory");
            Assert.AreEqual(strings[0], "1.file");

            strings = new string[1] { "c:\\1.file" };
            FileMatcher.RemoveProjectDirectory(strings, "c:\\directory");
            Assert.AreEqual(strings[0], "c:\\1.file");

            strings = new string[1] { "c:\\directorymorechars\\1.file" };
            FileMatcher.RemoveProjectDirectory(strings, "c:\\directory");
            Assert.AreEqual(strings[0], "c:\\directorymorechars\\1.file");

            strings = new string[1] { "\\Machine\\1.file" };
            FileMatcher.RemoveProjectDirectory(strings, "\\Machine");
            Assert.AreEqual(strings[0], "1.file");

            strings = new string[1] { "\\Machine\\directory\\1.file" };
            FileMatcher.RemoveProjectDirectory(strings, "\\Machine");
            Assert.AreEqual(strings[0], "directory\\1.file");

            strings = new string[1] { "\\Machine\\directory\\1.file" };
            FileMatcher.RemoveProjectDirectory(strings, "\\Machine\\directory");
            Assert.AreEqual(strings[0], "1.file");

            strings = new string[1] { "\\Machine\\1.file" };
            FileMatcher.RemoveProjectDirectory(strings, "\\Machine\\directory");
            Assert.AreEqual(strings[0], "\\Machine\\1.file");

            strings = new string[1] { "\\Machine\\directorymorechars\\1.file" };
            FileMatcher.RemoveProjectDirectory(strings, "\\Machine\\directory");
            Assert.AreEqual(strings[0], "\\Machine\\directorymorechars\\1.file");
        }

        #region Support functions.

        /// <summary>
        /// This support class simulates a file system.
        /// It accepts multiple sets of files and keeps track of how many files were "hit"
        /// In this case, "hit" means that the caller asked for that file directly.
        /// </summary>
        internal class MockFileSystem
        {
            /// <summary>
            /// Array of files (set1)
            /// </summary>
            private string[] _fileSet1;

            /// <summary>
            /// Array of files (set2)
            /// </summary>
            private string[] _fileSet2;

            /// <summary>
            /// Array of files (set3)
            /// </summary>
            private string[] _fileSet3;

            /// <summary>
            /// Number of times a file from set 1 was requested.
            /// </summary>
            private int _fileSet1Hits = 0;

            /// <summary>
            /// Number of times a file from set 2 was requested.
            /// </summary>
            private int _fileSet2Hits = 0;

            /// <summary>
            /// Number of times a file from set 3 was requested.
            /// </summary>
            private int _fileSet3Hits = 0;

            /// <summary>
            /// Construct.
            /// </summary>
            /// <param name="fileSet1">First set of files.</param>
            /// <param name="fileSet2">Second set of files.</param>
            /// <param name="fileSet3">Third set of files.</param>
            internal MockFileSystem
            (
                string[] fileSet1,
                string[] fileSet2,
                string[] fileSet3
            )
            {
                _fileSet1 = fileSet1;
                _fileSet2 = fileSet2;
                _fileSet3 = fileSet3;
            }

            /// <summary>
            /// Number of times a file from set 1 was requested.
            /// </summary>
            internal int FileHits1
            {
                get { return _fileSet1Hits; }
            }

            /// <summary>
            /// Number of times a file from set 2 was requested.
            /// </summary>
            internal int FileHits2
            {
                get { return _fileSet2Hits; }
            }

            /// <summary>
            /// Number of times a file from set 3 was requested.
            /// </summary>
            internal int FileHits3
            {
                get { return _fileSet3Hits; }
            }


            /// <summary>
            /// Return files that match the given files.
            /// </summary>
            /// <param name="candidates">Candidate files.</param>
            /// <param name="path">The path to search within</param>
            /// <param name="pattern">The pattern to search for.</param>
            /// <param name="files">Hashtable receives the files.</param>
            /// <returns></returns>
            private int GetMatchingFiles(string[] candidates, string path, string pattern, Hashtable files)
            {
                int hits = 0;
                if (candidates != null)
                {
                    foreach (string candidate in candidates)
                    {
                        string normalizedCandidate = Normalize(candidate);

                        // Get the candidate directory.
                        string candidateDirectoryName = "";
                        if (normalizedCandidate.IndexOfAny(FileMatcher.directorySeparatorCharacters) != -1)
                        {
                            candidateDirectoryName = Path.GetDirectoryName(normalizedCandidate) + Path.DirectorySeparatorChar;
                        }

                        // Does the candidate directory match the requested path?
                        if (String.Compare(path, candidateDirectoryName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // Match the basic *.* or null. These both match any file.
                            if
                            (
                                pattern == null ||
                                String.Compare(pattern, "*.*", StringComparison.OrdinalIgnoreCase) == 0
                            )
                            {
                                ++hits;
                                files[normalizedCandidate] = String.Empty;
                            }
                            else if (pattern.Substring(0, 2) == "*.") // Match patterns like *.cs
                            {
                                string tail = pattern.Substring(1);
                                string candidateTail = candidate.Substring(candidate.Length - tail.Length);
                                if (String.Compare(tail, candidateTail, StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    ++hits;
                                    files[normalizedCandidate] = String.Empty;
                                }
                            }
                            else if (pattern.Substring(pattern.Length - 4, 2) == ".?") // Match patterns like foo.?xt
                            {
                                string leader = pattern.Substring(0, pattern.Length - 4);
                                string candidateLeader = candidate.Substring(candidate.Length - leader.Length - 4, leader.Length);
                                if (String.Compare(leader, candidateLeader, StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    string tail = pattern.Substring(pattern.Length - 2);
                                    string candidateTail = candidate.Substring(candidate.Length - 2);
                                    if (String.Compare(tail, candidateTail, StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        ++hits;
                                        files[normalizedCandidate] = String.Empty;
                                    }
                                }
                            }
                            else
                            {
                                Assert.Fail(String.Format("Unhandled case in GetMatchingFiles: {0}", pattern));
                            }
                        }
                    }
                }

                return hits;
            }

            /// <summary>
            /// Given a path and pattern, return all the simulated directories out of candidates.
            /// </summary>
            /// <param name="candidates">Candidate file to extract directories from.</param>
            /// <param name="path">The path to search.</param>
            /// <param name="pattern">The pattern to match.</param>
            /// <param name="directories">Receives the directories.</param>
            private void GetMatchingDirectories(string[] candidates, string path, string pattern, Hashtable directories)
            {
                if (candidates != null)
                {
                    foreach (string candidate in candidates)
                    {
                        string normalizedCandidate = Normalize(candidate);

                        if (IsMatchingDirectory(path, normalizedCandidate))
                        {
                            int nextSlash = normalizedCandidate.IndexOfAny(FileMatcher.directorySeparatorCharacters, path.Length + 1);
                            if (nextSlash != -1)
                            {
                                string match = normalizedCandidate.Substring(0, nextSlash + 1);
                                string baseMatch = Path.GetFileName(normalizedCandidate.Substring(0, nextSlash));
                                if
                                (
                                    String.Compare(pattern, "*.*", StringComparison.OrdinalIgnoreCase) == 0
                                    || pattern == null
                                )
                                {
                                    directories[match] = String.Empty;
                                }
                                else if    // Match patterns like ?emp
                                    (
                                    pattern.Substring(0, 1) == "?"
                                    && pattern.Length == baseMatch.Length
                                )
                                {
                                    string tail = pattern.Substring(1);
                                    string baseMatchTail = baseMatch.Substring(1);
                                    if (String.Compare(tail, baseMatchTail, StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        directories[match] = String.Empty;
                                    }
                                }
                                else
                                {
                                    Assert.Fail(String.Format("Unhandled case in GetMatchingDirectories: {0}", pattern));
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Method that is delegable for use by FileMatcher. This method simulates a filesystem by returning
            /// files and\or folders that match the requested path and pattern.
            /// </summary>
            /// <param name="entityType">Files, Directories or both</param>
            /// <param name="path">The path to search.</param>
            /// <param name="pattern">The pattern to search (may be null)</param>
            /// <returns>The matched files or folders.</returns>
            internal string[] GetAccessibleFileSystemEntries(FileMatcher.FileSystemEntity entityType, string path, string pattern, string projectDirectory, bool stripProjectDirectory)
            {
                string normalizedPath = Normalize(path);

                Hashtable files = new Hashtable();
                if (entityType == FileMatcher.FileSystemEntity.Files || entityType == FileMatcher.FileSystemEntity.FilesAndDirectories)
                {
                    _fileSet1Hits += GetMatchingFiles(_fileSet1, normalizedPath, pattern, files);
                    _fileSet2Hits += GetMatchingFiles(_fileSet2, normalizedPath, pattern, files);
                    _fileSet3Hits += GetMatchingFiles(_fileSet3, normalizedPath, pattern, files);
                }

                if (entityType == FileMatcher.FileSystemEntity.Directories || entityType == FileMatcher.FileSystemEntity.FilesAndDirectories)
                {
                    GetMatchingDirectories(_fileSet1, normalizedPath, pattern, files);
                    GetMatchingDirectories(_fileSet2, normalizedPath, pattern, files);
                    GetMatchingDirectories(_fileSet3, normalizedPath, pattern, files);
                }
                ArrayList uniqueFiles = new ArrayList();
                uniqueFiles.AddRange(files.Keys);

                return (string[])uniqueFiles.ToArray(typeof(string));
            }

            /// <summary>
            /// Given a path, fix it up so that it can be compared to another path.
            /// </summary>
            /// <param name="path">The path to fix up.</param>
            /// <returns>The normalized path.</returns>
            internal static string Normalize(string path)
            {
                if (path.Length == 0)
                {
                    return path;
                }

                string normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                if (Path.DirectorySeparatorChar != '\\')
                {
                    normalized = path.Replace("\\", Path.DirectorySeparatorChar.ToString());
                }
                // Replace leading UNC.
                else if (normalized.Substring(0, 2) == @"\\")
                {
                    normalized = "<:UNC:>" + normalized.Substring(2);
                }

                // Preserve parent-directory markers.
                normalized = normalized.Replace(@".." + Path.DirectorySeparatorChar, "<:PARENT:>");


                // Just get rid of doubles enough to satisfy our test cases.
                string doubleSeparator = Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString();
                normalized = normalized.Replace(doubleSeparator, Path.DirectorySeparatorChar.ToString());
                normalized = normalized.Replace(doubleSeparator, Path.DirectorySeparatorChar.ToString());
                normalized = normalized.Replace(doubleSeparator, Path.DirectorySeparatorChar.ToString());

                // Strip any .\
                normalized = normalized.Replace(@"." + Path.DirectorySeparatorChar, "");

                // Put back the preserved markers.
                normalized = normalized.Replace("<:UNC:>", @"\\");
                normalized = normalized.Replace("<:PARENT:>", @".." + Path.DirectorySeparatorChar);

                return normalized;
            }

            /// <summary>
            /// Determines whether candidate is in a subfolder of path.
            /// </summary>
            /// <param name="path"></param>
            /// <param name="candidate"></param>
            /// <returns>True if there is a match.</returns>
            private bool IsMatchingDirectory(string path, string candidate)
            {
                string normalizedPath = Normalize(path);
                string normalizedCandidate = Normalize(candidate);

                // Current directory always matches for non-rooted paths.
                if (path.Length == 0 && !Path.IsPathRooted(candidate))
                {
                    return true;
                }

                if (normalizedCandidate.Length > normalizedPath.Length)
                {
                    if (String.Compare(normalizedPath, 0, normalizedCandidate, 0, normalizedPath.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (FileUtilities.EndsWithSlash(normalizedPath))
                        {
                            return true;
                        }
                        else if (FileUtilities.IsSlash(normalizedCandidate[normalizedPath.Length]))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }


            /// <summary>
            /// Searches the candidates array for one that matches path
            /// </summary>
            /// <param name="path"></param>
            /// <param name="candidates"></param>
            /// <returns>The index of the first match or negative one.</returns>
            private int IndexOfFirstMatchingDirectory(string path, string[] candidates)
            {
                if (candidates != null)
                {
                    int i = 0;
                    foreach (string candidate in candidates)
                    {
                        if (IsMatchingDirectory(path, candidate))
                        {
                            return i;
                        }

                        ++i;
                    }
                }

                return -1;
            }

            /// <summary>
            /// Delegable method that returns true if the given directory exists in this simulated filesystem
            /// </summary>
            /// <param name="path">The path to check.</param>
            /// <returns>True if the directory exists.</returns>
            internal bool DirectoryExists(string path)
            {
                if (IndexOfFirstMatchingDirectory(path, _fileSet1) != -1)
                {
                    return true;
                }

                if (IndexOfFirstMatchingDirectory(path, _fileSet2) != -1)
                {
                    return true;
                }

                if (IndexOfFirstMatchingDirectory(path, _fileSet3) != -1)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// A general purpose method used to:
        /// 
        /// (1) Simulate a file system.
        /// (2) Check whether all matchingFiles where hit by the filespec pattern.
        /// (3) Check whether all nonmatchingFiles were *not* hit by the filespec pattern.
        /// (4) Check whether all untouchableFiles were not even requested (usually for perf reasons).
        /// 
        /// These can be used in various combinations to test the filematcher framework.
        /// </summary>
        /// <param name="filespec">A FileMatcher filespec, possibly with wildcards.</param>
        /// <param name="matchingFiles">Files that exist and should be matched.</param>
        /// <param name="nonmatchingFiles">Files that exists and should not be matched.</param>
        /// <param name="untouchableFiles">Files that exist but should not be requested.</param>
        private static void MatchDriver
        (
            string filespec,
            string[] matchingFiles,
            string[] nonmatchingFiles,
            string[] untouchableFiles
        )
        {
            MockFileSystem mockFileSystem = new MockFileSystem(matchingFiles, nonmatchingFiles, untouchableFiles);
            string[] files = FileMatcher.GetFiles
            (
                String.Empty, /* we don't need project directory as we use mock filesystem */
                filespec,
                new FileMatcher.GetFileSystemEntries(mockFileSystem.GetAccessibleFileSystemEntries),
                new DirectoryExists(mockFileSystem.DirectoryExists)
            );

            // Validate the matching files.
            if (matchingFiles != null)
            {
                foreach (string matchingFile in matchingFiles)
                {
                    int timesFound = 0;
                    foreach (string file in files)
                    {
                        string normalizedFile = MockFileSystem.Normalize(file);
                        string normalizedMatchingFile = MockFileSystem.Normalize(matchingFile);
                        if (String.Compare(normalizedFile, normalizedMatchingFile, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            ++timesFound;
                        }
                    }
                    Assert.IsTrue(timesFound == 1, String.Format("Expected to find matching file '{0}' exactly one times. Found it '{1}' times instead.", matchingFile, timesFound));
                }
            }


            // Validate the non-matching files
            if (nonmatchingFiles != null)
            {
                foreach (string nonmatchingFile in nonmatchingFiles)
                {
                    int timesFound = 0;
                    foreach (string file in files)
                    {
                        string normalizedFile = MockFileSystem.Normalize(file);
                        string normalizedNonmatchingFile = MockFileSystem.Normalize(nonmatchingFile);
                        if (String.Compare(normalizedFile, normalizedNonmatchingFile, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            ++timesFound;
                        }
                    }
                    Assert.IsTrue(timesFound == 0, String.Format("Expected not to match file '{0}' but did.", nonmatchingFile));
                }
            }

            // Check untouchable files.
            Assert.IsTrue(mockFileSystem.FileHits3 == 0, "At least one file that was marked untouchable was referenced.");
        }



        /// <summary>
        /// Simulate GetFileSystemEntries
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <returns>Array of matching file system entries (can be empty).</returns>
        private static string[] GetFileSystemEntriesLoopBack(FileMatcher.FileSystemEntity entityType, string path, string pattern, string projectDirectory, bool stripProjectDirectory)
        {
            return new string[] { Path.Combine(path, pattern) };
        }

        /*************************************************************************************
         * Validate that SplitFileSpec(...) is returning the expected constituent values.
         *************************************************************************************/

        private static void ValidateSplitFileSpec
            (
            string filespec,
            string expectedFixedDirectoryPart,
            string expectedWildcardDirectoryPart,
            string expectedFilenamePart
            )
        {
            string fixedDirectoryPart;
            string wildcardDirectoryPart;
            string filenamePart;
            FileMatcher.SplitFileSpec
            (
                filespec,
                out fixedDirectoryPart,
                out wildcardDirectoryPart,
                out filenamePart,
                new FileMatcher.GetFileSystemEntries(GetFileSystemEntriesLoopBack)
            );

            expectedFixedDirectoryPart = FileUtilities.FixFilePath(expectedFixedDirectoryPart);
            expectedWildcardDirectoryPart = FileUtilities.FixFilePath(expectedWildcardDirectoryPart);
            expectedFilenamePart = FileUtilities.FixFilePath(expectedFilenamePart);

            if
                (
                expectedWildcardDirectoryPart != wildcardDirectoryPart
                || expectedFixedDirectoryPart != fixedDirectoryPart
                || expectedFilenamePart != filenamePart
                )
            {
                Console.WriteLine("Expect Fixed '{0}' got '{1}'", expectedFixedDirectoryPart, fixedDirectoryPart);
                Console.WriteLine("Expect Wildcard '{0}' got '{1}'", expectedWildcardDirectoryPart, wildcardDirectoryPart);
                Console.WriteLine("Expect Filename '{0}' got '{1}'", expectedFilenamePart, filenamePart);
                Assert.Fail("FileMatcher Regression: Failure while validating SplitFileSpec.");
            }
        }

        /*************************************************************************************
        * Given a pattern (filespec) and a candidate filename (fileToMatch). Verify that they
        * do indeed match.
        *************************************************************************************/
        private static void ValidateFileMatch
            (
            string filespec,
            string fileToMatch,
            bool shouldBeRecursive
            )
        {
            ValidateFileMatch(filespec, fileToMatch, shouldBeRecursive, /* Simulate filesystem? */ true);
        }

        /*************************************************************************************
        * Given a pattern (filespec) and a candidate filename (fileToMatch). Verify that they
        * do indeed match.
        *************************************************************************************/
        private static void ValidateFileMatch
            (
            string filespec,
            string fileToMatch,
            bool shouldBeRecursive,
            bool fileSystemSimulation
            )
        {
            if (!IsFileMatchAssertIfIllegal(filespec, fileToMatch, shouldBeRecursive))
            {
                Assert.Fail("FileMatcher Regression: Failure while validating that files match.");
            }

            // Now, simulate a filesystem with only fileToMatch. Make sure the file exists that way.
            if (fileSystemSimulation)
            {
                MatchDriver
                (
                    filespec,
                    new string[] { fileToMatch },
                    null,
                    null
                );
            }
        }

        /*************************************************************************************
        * Given a pattern (filespec) and a candidate filename (fileToMatch). Verify that they
        * DON'T match.
        *************************************************************************************/
        private static void ValidateNoFileMatch
            (
            string filespec,
            string fileToMatch,
            bool shouldBeRecursive
            )
        {
            if (IsFileMatchAssertIfIllegal(filespec, fileToMatch, shouldBeRecursive))
            {
                Assert.Fail("FileMatcher Regression: Failure while validating that files don't match.");
            }

            // Now, simulate a filesystem with only fileToMatch. Make sure the file doesn't exist that way.
            MatchDriver
            (
                filespec,
                null,
                new string[] { fileToMatch },
                null
            );
        }

        /*************************************************************************************
        * Verify that the given filespec is illegal.
        *************************************************************************************/
        private static void ValidateIllegal
            (
            string filespec
            )
        {
            Regex regexFileMatch;
            bool needsRecursion;
            bool isLegalFileSpec;
            FileMatcher.GetFileSpecInfo
            (
                filespec,
                out regexFileMatch,
                out needsRecursion,
                out isLegalFileSpec,
                new FileMatcher.GetFileSystemEntries(GetFileSystemEntriesLoopBack)
            );

            if (isLegalFileSpec)
            {
                Assert.Fail("FileMatcher Regression: Expected an illegal filespec, but got a legal one.");
            }

            // Now, FileMatcher is supposed to take any legal file name and just return it immediately.
            // Let's see if it does.
            MatchDriver
            (
                filespec,                        // Not legal.
                new string[] { filespec },        // Should match
                null,
                null
            );
        }
        /*************************************************************************************
        * Given a pattern (filespec) and a candidate filename (fileToMatch) return true if
        * FileMatcher would say that they match.
        *************************************************************************************/
        private static bool IsFileMatchAssertIfIllegal
        (
            string filespec,
            string fileToMatch,
            bool shouldBeRecursive
        )
        {
            FileMatcher.Result match = FileMatcher.FileMatch(filespec, fileToMatch);

            if (!match.isLegalFileSpec)
            {
                Console.WriteLine("Checking FileSpec: '{0}' against '{1}'", filespec, fileToMatch);
                Assert.Fail("FileMatcher Regression: Invalid filespec.");
            }
            if (shouldBeRecursive != match.isFileSpecRecursive)
            {
                Console.WriteLine("Checking FileSpec: '{0}' against '{1}'", filespec, fileToMatch);
                Assert.IsTrue(shouldBeRecursive, "FileMatcher Regression: Match was recursive when it shouldn't be.");
                Assert.IsFalse(shouldBeRecursive, "FileMatcher Regression: Match was not recursive when it should have been.");
            }
            return match.isMatch;
        }

        #endregion
    }
}




