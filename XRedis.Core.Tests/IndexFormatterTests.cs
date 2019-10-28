using Microsoft.VisualStudio.TestTools.UnitTesting;
using XRedis.Core.Fields;
using XRedis.Core.Fields.Indexes;
using XRedis.Core.Fields.IndexFormatters;

namespace XRedis.Core.Tests
{
    [TestClass]
    public class IndexFormatterTests
    {
        class MockRecord : IRecord
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }


        [TestMethod]
        public void TestMethod1()
        {
            var record = new MockRecord(){FirstName = "Clayton", LastName = "Hall"};
            var index = new Index("test", typeof(MockRecord), new IndexableField(typeof(MockRecord), typeof(MockRecord).GetProperty("FirstName")));
            var formatted = index.Format(record);
            Assert.AreEqual(formatted, "Clayton");
        }

        [TestMethod]
        public void UpperIndexFormatter()
        {
            var indexFormatter = new UpperIndexFormatter();
            var formatted = indexFormatter.Format("Clayton");
            Assert.AreEqual("CLAYTON", formatted);
        }

        [TestMethod]
        public void DefaultIndexFormatterFormatString()
        {
            var indexFormatter = new DefaultIndexFormatter();
            var formatted = indexFormatter.Format("Clayton");
            Assert.AreEqual("Clayton", formatted);
        }

        [TestMethod]
        public void DefaultIndexFormatterFormatInt()
        {
            var indexFormatter = new DefaultIndexFormatter();
            var formatted = indexFormatter.Format(1);
            Assert.AreEqual("0000000001", formatted);
        }

        [TestMethod]
        public void DefaultIndexFormatterFormatLong()
        {
            var indexFormatter = new DefaultIndexFormatter();
            var formatted = indexFormatter.Format(1L);
            Assert.AreEqual("0000000000000000001", formatted);
        }

        [TestMethod]
        public void DefaultIndexFormatterFormatShort()
        {
            var indexFormatter = new DefaultIndexFormatter();
            var formatted = indexFormatter.Format((short)1);
            Assert.AreEqual("00001", formatted);
        }

        [TestMethod]
        public void DefaultIndexFormatterFormatDecimal()
        {
            var indexFormatter = new DefaultIndexFormatter();
            var formatted = indexFormatter.Format(1M);
            Assert.AreEqual("00000000000000000000000000001", formatted);
        }

        [TestMethod]
        public void DefaultIndexFormatterFormatFloat()
        {
            var indexFormatter = new DefaultIndexFormatter();
            var formatted = indexFormatter.Format(1F);
            Assert.AreEqual("0000000000001", formatted);
        }

        [TestMethod]
        public void DefaultIndexFormatterFormatDouble()
        {
            var indexFormatter = new DefaultIndexFormatter();
            var formatted = indexFormatter.Format(1D);
            Assert.AreEqual("00000000000000000000001", formatted);
        }
    }
}
