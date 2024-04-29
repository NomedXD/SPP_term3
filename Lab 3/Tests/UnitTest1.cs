using AssemblyBrowserLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        private const string value = "C:\\Users\\coovs\\OneDrive\\Desktop\\SPP\\SPP_term3\\Lab 3\\TestLibrary\\bin\\Debug\\TestLibrary.dll";

        [TestMethod]
        public void TestMethod1()
        {
            AssemblyBrowser assemblyBrowser = new AssemblyBrowser();
            List<ContainerInfo> container = new List<ContainerInfo>(assemblyBrowser.GetNamespaces(value));
            Assert.IsTrue(container[0].DeclarationName.Equals("TestLibrary") && container[1].DeclarationName.Equals("TestLibrary.Extension"));
        }
    }
}
