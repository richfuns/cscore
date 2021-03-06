using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.csutil.http;
using Xunit;

namespace com.csutil.tests {

    public class FileTests {

        [Fact]
        public void ExampleUsage1() {
            // First cleanup existing test folder if they already exist:
            EnvironmentV2.instance.GetTempFolder("ExampleUsage1").DeleteV2();

            // Get a directory to work in:
            DirectoryInfo myDirectory = EnvironmentV2.instance.GetTempFolder("ExampleUsage1");
            Log.d("The directory path is: " + myDirectory.FullPath());

            // Get a non-existing child directory
            var childDir = myDirectory.GetChildDir("MyExampleSubDirectory1");

            // Create the sub directory:
            childDir.CreateV2(); // myDirectory.CreateSubdirectory("..") works too

            // Rename the directory:
            childDir.Rename("MyExampleSubDirectory2");

            // Get a file in the child directory:
            FileInfo file1 = childDir.GetChild("MyFile1.txt");

            // Saving and loading from files:
            string someTextToStoreInTheFile = "Some text to store in the file";
            file1.SaveAsText(someTextToStoreInTheFile);
            string loadedText = file1.LoadAs<string>(); // loading JSON would work as well
            Assert.Equal(someTextToStoreInTheFile, loadedText);

            // Deleting directories:
            Assert.True(childDir.DeleteV2()); // (Deleting non-existing directories would returns false)
            // Check that the directory no longer exists:
            Assert.False(childDir.IsNotNullAndExists());

        }

        [Fact]
        public void TestIsNotNullAndExists() {
            DirectoryInfo appDataDir = null;
            Assert.False(appDataDir.IsNotNullAndExists());
            appDataDir = EnvironmentV2.instance.GetAppDataFolder();
            Log.d("appDataDir=" + appDataDir.FullPath());
            Assert.True(appDataDir.IsNotNullAndExists());
        }

        [Fact]
        public void TestDirectoryMethods() {
            var rootDir = CreateDirectoryForTesting("TestDirectoryMethods");

            // Test FullPath:
            var dir1 = rootDir.GetChildDir("TestDir 1");
            var alsoDir1 = dir1.Parent.GetChildDir("TestDir 1");
            Assert.Equal(dir1.FullPath(), alsoDir1.FullPath());

            // Test deleting and creating Dir 1:
            dir1.DeleteV2();
            Assert.False(dir1.IsNotNullAndExists());
            Assert.False(dir1.Exists);
            Assert.True(dir1.CreateV2().Exists);
            Assert.True(dir1.IsNotNullAndExists());
            dir1.Create(); // Should do nothing and not throw an exception

            // Test creating sub dirs and files:
            var subDir = dir1.CreateSubdirectory("ChildDir 1");
            SaveAndLoadTextToFile(subDir.GetChild("Test file 1"), textToSave: "Test 123");
            SaveAndLoadTextToFile(subDir.GetChild("Test file 2.txt"), textToSave: "Test 123");

            { // Test moving folders:
                var oldPath = dir1.FullPath();
                var dir2 = rootDir.GetChildDir("TestDir 2");
                dir2.DeleteV2();
                dir1.MoveToV2(dir2);
                Assert.True(dir2.IsNotNullAndExists());
                Assert.Equal(dir2.FullPath(), dir1.FullPath());
                Assert.False(new DirectoryInfo(oldPath).Exists);
            }
            { // Test that moving to existing folders fails:
                var dir3 = rootDir.GetChildDir("TestDir 3").CreateV2();
                Assert.Equal("TestDir 3", dir3.Name);
                dir3 = rootDir.CreateSubdirectory(dir3.Name);
                AssertV2.Throws<Exception>(() => {
                    dir1.MoveToV2(dir3); // This should fail since dir3 already exists
                });
                Assert.NotEqual(dir3.FullPath(), dir1.FullPath());
                dir3.Delete(); // Cleanup after test
            }
            { // Test copying folders:
                var oldPath = dir1.FullPath();
                var dir4 = rootDir.GetChildDir("TestDir 4");
                dir4.DeleteV2(); // Make sure dir does not yet exist from previous tests
                dir1.CopyTo(dir4);
                Assert.True(dir4.IsNotNullAndExists(), "dir=" + dir4.FullPath());
                Assert.NotEqual(dir4.FullPath(), dir1.FullPath());
                Assert.True(new DirectoryInfo(oldPath).Exists);
                dir4.DeleteV2(); // Cleanup after test
            }
            { // Test renaming folders:
                var newName = "TestDir 5";
                rootDir.GetChildDir(newName).DeleteV2();
                var oldPath = dir1.FullPath();
                dir1.Rename(newName);
                Assert.False(new DirectoryInfo(oldPath).Exists);
                Assert.True(dir1.Exists);
                Assert.Equal(newName, dir1.NameV2());

                // DirectoryInfo.Name does not correctly update after renaming, 
                // Thats why DirectoryInfo.name must return an incorrect value:
                Assert.NotEqual(newName, dir1.Name);
                // TODO is DirectoryInfo.Name broken for all .net versions?

            }

            rootDir.DeleteV2(); // Cleanup after test
        }

        private static void SaveAndLoadTextToFile(FileInfo testFile, string textToSave) {
            testFile.SaveAsText(textToSave);
            Assert.True(testFile.IsNotNullAndExists());
            Assert.Equal(textToSave, testFile.LoadAs<string>()); // Load again and compare
        }

        [Fact]
        public void TestFileWriteAndRead() {
            DirectoryInfo rootDir = CreateDirectoryForTesting("TestFileWriteAndRead");

            SaveAndLoadTextToFile(rootDir.GetChild("f1.txt"), textToSave: "Test 123");
            Assert.True(rootDir.GetChild("f1.txt").IsNotNullAndExists());

            var objToSave = new MyClass1() { myString = "I am a string", myInt = 123 };
            var jsonFile = rootDir.GetChild("MyClass1InAJsonFile.txt");
            jsonFile.SaveAsJson(objToSave);
            jsonFile.SaveAsJson(objToSave); // This will override the existing file
            MyClass1 loadedObj = jsonFile.LoadAs<MyClass1>(); // Load the object again and compare:
            Assert.Equal(objToSave.myString, loadedObj.myString);
            Assert.Equal(objToSave.myInt, loadedObj.myInt);
            loadedObj = jsonFile.LoadAs<MyClass1>(); // Load the object again and compare:
            Assert.Equal(objToSave.myString, loadedObj.myString);
            Assert.Equal(objToSave.myInt, loadedObj.myInt);
            rootDir.DeleteV2();
        }

        private static DirectoryInfo CreateDirectoryForTesting(string name) {
            var rootDir = EnvironmentV2.instance.GetCurrentDirectory().CreateSubdirectory(name);
            rootDir.DeleteV2(); // ensure that the test folder does not yet exist
            rootDir.CreateV2();
            Assert.True(rootDir.IsNotNullAndExists());
            return rootDir;
        }

        [Fact]
        public void TestFileRenameAndMove() {
            var rootDir = CreateDirectoryForTesting("TestFileRename");

            var myFile = rootDir.GetChild("MyFile1.txt");
            SaveAndLoadTextToFile(myFile, textToSave: "Test 123");
            Assert.True(myFile.IsNotNullAndExists());

            var newName = "MyFile2.txt";
            var oldPath = new FileInfo(myFile.FullPath());
            myFile.Rename(newName);
            Assert.Equal(oldPath.ParentDir().FullPath(), myFile.ParentDir().FullPath());
            Assert.Equal(newName, myFile.Name);
            Assert.NotEqual(oldPath.Name, myFile.Name);

            var subdir = rootDir.CreateSubdirectory("subdir");
            myFile.MoveToV2(subdir);
            Assert.Single(subdir.GetFiles()); // The folder should now contain 1 entry
            Assert.Equal(subdir.FullPath(), myFile.ParentDir().FullPath());

            rootDir.DeleteV2();
        }

        private class MyClass1 {
            public string myString;
            public int myInt;
        }

    }
}