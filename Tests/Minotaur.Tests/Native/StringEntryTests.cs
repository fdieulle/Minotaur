using System;
using Minotaur.Native;
using NUnit.Framework;

namespace Minotaur.Tests.Native
{
    [TestFixture]
    public unsafe class StringEntryTests
    {
        [Test]
        public void TestGetSetValue()
        {
            Assert.AreEqual(256, sizeof(StringEntry));

            var s = new StringEntry();
            
            s.SetValue("Test");
            Assert.AreEqual("Test", s.GetValue());

            // Test truncate
            var array = new char[256];
            for (var i = 0; i < 256; i++)
                array[i] = 'A';
            var str = new string(array);
            s.SetValue(str);
            Assert.AreEqual(str.Substring(0, 247), s.GetValue());
        }
    }
}
