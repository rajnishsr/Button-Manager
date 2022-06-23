using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Hca.SSOSolution.Service.Logic;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LASSOTests
{
    /// <summary>
    /// Summary description for TestLoad
    /// </summary>
    [TestFixture]
    public class Tests
    {
        
        ISingleSignOnService srvc = null;
        [SetUp]
        public void Setup()
        {
            srvc = new SingleSignOnService();
        }

        [Test]
        public void TestLoadMethod()
        {
            string message = "";
            srvc.Load(out message);
            Assert.AreSame(message, "");
        }

        [Test]
        public void TestSave()
        {
            Exception ex = null;
            try
            {
                srvc.Save();
            }
            catch(Exception e)
            {
                ex = e;
            }
            Assert.IsNull(ex);

        }

        [TearDown]
        public void TearItDown()
        {
            srvc = null;
        }
    }
}
