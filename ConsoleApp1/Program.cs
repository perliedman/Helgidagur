using Helgidagur;
using System;
using System.Globalization;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var d = DateTime.Today;
            var culture = new CultureInfo("sv-SE");

            for (var i = 0; i < 90; i++)
            {
                Console.WriteLine(d.ToString() + ": " + d.IsHoliday(culture));
                d = d.AddDays(1);
            }

            var calendar = new HolidayCalendar(new CultureInfo("sv-SE"));
            foreach (var day in calendar.GetDays(2017))
            {
                Console.WriteLine(string.Format("{0}: {1} ({2})", day.Date, day.Name, day.Type));
            }

            Console.ReadLine();
        }
    }
}
