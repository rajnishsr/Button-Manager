using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Hca.SSOSolution.Service.Logic;
using Assert = NUnit.Framework.Assert;

namespace LASSOTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>

    [TestFixture]
    public class UnitTest1
    {
        ISingleSignOnService srvc = null;
        [SetUp]
        public void Setup()
        {
            
            
            srvc = new SingleSignOnService();
        }

        [Test]
        public void TestMethod1()
        {
            Assert.IsNotNull(srvc);
        }

        [Test]
        public void TestMethod2()
        {
            throw new NotImplementedException();
        }

        [TearDown]
        public void TearItDown()
        {
            srvc = null;
        }
    }
}
