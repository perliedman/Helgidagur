using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Helgidagur;
using System.Globalization;

namespace UnitTestProject1
{
    [TestClass]
    public class HolidayCalendarSpec
    {
        [TestMethod]
        public void TestMethod1()
        {
            var calendar = new HolidayCalendar(new CultureInfo("se"));
        }
    }
}
