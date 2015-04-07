﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;

using NUnit.Framework;

namespace Microsoft.Build.UnitTests
{
    [TestFixture]
    sealed public class Touch_Tests
    {
        internal static Microsoft.Build.Shared.FileExists fileExists = new Microsoft.Build.Shared.FileExists(FileExists);
        internal static Microsoft.Build.Shared.FileCreate fileCreate = new Microsoft.Build.Shared.FileCreate(FileCreate);
        internal static Microsoft.Build.Tasks.GetAttributes fileGetAttributes = new Microsoft.Build.Tasks.GetAttributes(GetAttributes);
        internal static Microsoft.Build.Tasks.SetAttributes fileSetAttributes = new Microsoft.Build.Tasks.SetAttributes(SetAttributes);
        internal static Microsoft.Build.Tasks.SetLastAccessTime setLastAccessTime = new Microsoft.Build.Tasks.SetLastAccessTime(SetLastAccessTime);
        internal static Microsoft.Build.Tasks.SetLastWriteTime setLastWriteTime = new Microsoft.Build.Tasks.SetLastWriteTime(SetLastWriteTime);

        internal static string myexisting_txt = NativeMethodsShared.IsWindows ? @"c:\touch\myexisting.txt" : @"/touch/myexisting.txt";
        internal static string mynonexisting_txt = NativeMethodsShared.IsWindows ? @"c:\touch\mynonexisting.txt" : @"/touch/mynonexisting.txt";
        internal static string nonexisting_txt = NativeMethodsShared.IsWindows ? @"c:\touch-nonexisting\file.txt" : @"/touch-nonexisting/file.txt";
        internal static string myreadonly_txt = NativeMethodsShared.IsWindows ? @"c:\touch\myreadonly.txt" : @"/touch/myreadonly.txt";

        private bool Execute(Touch t)
        {
            return t.ExecuteImpl
            (
                fileExists,
                fileCreate,
                fileGetAttributes,
                fileSetAttributes,
                setLastAccessTime,
                setLastWriteTime
            );
        }

        /// <summary>
        /// Mock file exists.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool FileExists(string path)
        {
            if (path == myexisting_txt)
            {
                return true;
            }

            if (path == mynonexisting_txt)
            {
                return false;
            }

            if (path == nonexisting_txt)
            {
                return false;
            }

            if (path == myreadonly_txt)
            {
                return true;
            }
            Assert.Fail("Unexpected file exists: " + path);

            return true;
        }

        /// <summary>
        /// Mock file create.
        /// </summary>
        /// <param name="path"></param>
        private static FileStream FileCreate(string path)
        {
            if (path == mynonexisting_txt)
            {
                return null;
            }

            if (path == nonexisting_txt)
            {
                throw new DirectoryNotFoundException();
            }


            Assert.Fail("Unexpected file create: " + path);
            return null;
        }

        /// <summary>
        /// Mock get attributes.
        /// </summary>
        /// <param name="path"></param>
        private static FileAttributes GetAttributes(string path)
        {
            FileAttributes a = new FileAttributes();
            if (path == myexisting_txt)
            {
                return a;
            }

            if (path == mynonexisting_txt)
            {
                // Has attributes because Touch created it.
                return a;
            }

            if (path == myreadonly_txt)
            {
                a = System.IO.FileAttributes.ReadOnly;
                return a;
            }

            Assert.Fail("Unexpected file attributes: " + path);
            return a;
        }

        /// <summary>
        /// Mock get attributes.
        /// </summary>
        /// <param name="path"></param>
        private static void SetAttributes(string path, FileAttributes attributes)
        {
            if (path == myreadonly_txt)
            {
                return;
            }
            Assert.Fail("Unexpected set file attributes: " + path);
        }

        /// <summary>
        /// Mock SetLastAccessTime.
        /// </summary>
        /// <param name="path"></param>
        private static void SetLastAccessTime(string path, DateTime timestamp)
        {
            if (path == myexisting_txt)
            {
                return;
            }

            if (path == mynonexisting_txt)
            {
                return;
            }

            if (path == myreadonly_txt)
            {
                // Read-only so throw an exception
                throw new IOException();
            }

            Assert.Fail("Unexpected set last access time: " + path);
        }

        /// <summary>
        /// Mock SetLastWriteTime.
        /// </summary>
        /// <param name="path"></param>
        private static void SetLastWriteTime(string path, DateTime timestamp)
        {
            if (path == myexisting_txt)
            {
                return;
            }

            if (path == mynonexisting_txt)
            {
                return;
            }

            if (path == myreadonly_txt)
            {
                return;
            }


            Assert.Fail("Unexpected set last write time: " + path);
        }

        [Test]
        public void TouchExisting()
        {
            Touch t = new Touch();
            MockEngine engine = new MockEngine();
            t.BuildEngine = engine;

            t.Files = new ITaskItem[]
            {
                new TaskItem(myexisting_txt)
            };

            bool success = Execute(t);

            Assert.IsTrue(success);

            Assert.AreEqual(1, t.TouchedFiles.Length);

            Assert.IsTrue
            (
                engine.Log.Contains
                (
                    String.Format(AssemblyResources.GetString("Touch.Touching"), myexisting_txt)
                )
            );
        }

        [Test]
        public void TouchNonExisting()
        {
            Touch t = new Touch();
            MockEngine engine = new MockEngine();
            t.BuildEngine = engine;

            t.Files = new ITaskItem[]
            {
                new TaskItem(mynonexisting_txt)
            };

            bool success = Execute(t);

            // Not success because the file doesn't exist
            Assert.IsTrue(!success);

            Assert.IsTrue
            (
                engine.Log.Contains
                (
                    String.Format(AssemblyResources.GetString("Touch.FileDoesNotExist"), mynonexisting_txt)
                )
            );
        }

        [Test]
        public void TouchNonExistingAlwaysCreate()
        {
            Touch t = new Touch();
            MockEngine engine = new MockEngine();
            t.BuildEngine = engine;
            t.AlwaysCreate = true;

            t.Files = new ITaskItem[]
            {
                new TaskItem(mynonexisting_txt)
            };

            bool success = Execute(t);

            // Success because the file was created.
            Assert.IsTrue(success);

            Assert.IsTrue
            (
                engine.Log.Contains
                (
                    String.Format(AssemblyResources.GetString("Touch.CreatingFile"), mynonexisting_txt, "AlwaysCreate")
                )
            );
        }

        [Test]
        public void TouchNonExistingAlwaysCreateAndBadlyFormedTimestamp()
        {
            Touch t = new Touch();
            MockEngine engine = new MockEngine();
            t.BuildEngine = engine;
            t.AlwaysCreate = true;
            t.ForceTouch = false;
            t.Time = "Badly formed time String.";

            t.Files = new ITaskItem[]
            {
                new TaskItem(mynonexisting_txt)
            };

            bool success = Execute(t);

            // Failed because of badly formed time string.
            Assert.IsTrue(!success);

            Assert.IsTrue(engine.Log.Contains("MSB3376"));
        }

        [Test]
        public void TouchReadonly()
        {
            Touch t = new Touch();
            MockEngine engine = new MockEngine();
            t.BuildEngine = engine;
            t.AlwaysCreate = true;

            t.Files = new ITaskItem[]
            {
                new TaskItem(myreadonly_txt)
            };

            bool success = Execute(t);

            // Failed because file is readonly.
            Assert.IsTrue(!success);

            Assert.IsTrue(engine.Log.Contains("MSB3374"));
            Assert.IsTrue(engine.Log.Contains(myreadonly_txt));
        }

        [Test]
        public void TouchReadonlyForce()
        {
            Touch t = new Touch();
            MockEngine engine = new MockEngine();
            t.BuildEngine = engine;
            t.ForceTouch = true;
            t.AlwaysCreate = true;

            t.Files = new ITaskItem[]
            {
                new TaskItem(myreadonly_txt)
            };

            bool success = Execute(t);
        }

        [Test]
        public void TouchNonExistingDirectoryDoesntExist()
        {
            Touch t = new Touch();
            MockEngine engine = new MockEngine();
            t.BuildEngine = engine;
            t.AlwaysCreate = true;

            t.Files = new ITaskItem[]
            {
                new TaskItem(nonexisting_txt)
            };

            bool success = Execute(t);

            // Failed because the target directory didn't exist.
            Assert.IsTrue(!success);

            Assert.IsTrue(engine.Log.Contains("MSB3371"));
            Assert.IsTrue(engine.Log.Contains(nonexisting_txt));
        }
    }
}


