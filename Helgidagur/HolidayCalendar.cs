using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Helgidagur
{
    public class HolidayCalendar
    {
        static HolidayCalendar()
        {
            MyAssembly = Assembly.GetExecutingAssembly();
        }

        public HolidayCalendar(CultureInfo culture)
            : this(culture, new RegionInfo(culture.LCID), MyAssembly.GetManifestResourceStream($"Helgidagur.Data.{new RegionInfo(culture.LCID).TwoLetterISORegionName}.yaml"))
        {

        }

        public HolidayCalendar(CultureInfo culture, RegionInfo region, Stream configData)
        {
            var builder = new DeserializerBuilder()
                .WithNamingConvention(new UnderscoredNamingConvention());
            var yamlDeserializer = builder.Build();
            using (var reader = new StreamReader(configData))
            {
                var config = (IDictionary<object, object>)yamlDeserializer.Deserialize(reader);
                var holidays = (IDictionary<object, object>)config["holidays"];
                var cultureConfig = (IDictionary<object, object>)holidays[region.TwoLetterISORegionName];
                Names = ((IDictionary<object, object>)cultureConfig["names"]).ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToString());
                Dayoff = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), Capitalize((string)cultureConfig["dayoff"]));
                Langs = ((IEnumerable<object>)cultureConfig["langs"]).Cast<string>();
                Days = ((IDictionary<object, object>)cultureConfig["days"])
                    .Select(kvp => new CalendarDay(
                        (string)kvp.Key, 
                        ((IDictionary<object, object>)kvp.Value)
                        .ToDictionary(kvp2 => kvp2.Key.ToString(), kvp2 => kvp2.Value), culture))
                    .ToList();
            }
        }

        public IDictionary<string, string> Names { get; private set; }
        public DayOfWeek Dayoff { get; private set; }
        public IEnumerable<string> Langs { get; private set; }
        
        public IEnumerable<Day> GetDays(int year)
        {
            return Days.Select(d => new Day
            {
                Name = d.Name,
                Type = d.Type,
                Date = d.ToDate(year)
            });
        }

        private static readonly Assembly MyAssembly;
        private IList<CalendarDay> Days { get; set; }

        private static readonly IDictionary<string, Func<int, DateTime>> GlobalHolidays = new Dictionary<string, Func<int, DateTime>>
        {
            {
                "easter", year =>
                {
                    // https://stackoverflow.com/a/2510411/890
                    int day = 0;
                    int month = 0;

                    int g = year % 19;
                    int c = year / 100;
                    int h = (c - (int)(c / 4) - (int)((8 * c + 13) / 25) + 19 * g + 15) % 30;
                    int i = h - (int)(h / 28) * (1 - (int)(h / 28) * (int)(29 / (h + 1)) * (int)((21 - g) / 11));

                    day   = i - ((year + (int)(year / 4) + i + 2 - c + (int)(c / 4)) % 7) + 28;
                    month = 3;

                    if (day > 31)
                    {
                        month++;
                        day -= 31;
                    }

                    return new DateTime(year, month, day);
                }
            }
        };

        private class CalendarDay
        {
            private static readonly IEnumerable<DateExpression> DateExpressions = new[]
            {
                new DateExpression
                {
                    Pattern = new Regex(@"^(\d{2})-(\d{2})$"),
                    ToDate = (match, year) => new DateTime(year, int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value))
                },
                new DateExpression
                {
                    Pattern = new Regex(@"^(\w+)$"),
                    ToDate = (match, year) => GlobalHolidays[match.Groups[1].Value.ToLower()](year)
                },
                new DateExpression
                {
                    Pattern = new Regex(@"^(\w+)\s+([\-\d]+)$"),
                    ToDate = (match, year) => GlobalHolidays[match.Groups[1].Value.ToLower()](year).AddDays(int.Parse(match.Groups[2].Value))
                },
                new DateExpression
                {
                    Pattern = new Regex(@"^(\w+)\s+(\w+)\s+(\d{2})-(\d{2})$"),
                    ToDate = (match, year) => {
                        var dayName = match.Groups[1].Value;
                        var dayOfWeek = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), Capitalize(dayName));
                        var dir = match.Groups[2].Value.ToLower() == "after" ? 1 : -1;
                        var date = new DateTime(year, int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value));
                        while (date.DayOfWeek != dayOfWeek)
                        {
                            date = date.AddDays(dir);
                        }
                        return date;
                    }
                },
                new DateExpression
                {
                    Pattern = new Regex(@"^(\w+)\s+in\s+(\w+)$"),
                    ToDate = (match, year) => {
                        var dayName = match.Groups[1].Value;
                        var dayOfWeek = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), Capitalize(dayName));
                        var date = DateTime.ParseExact(string.Format("{0} 01, {1}", match.Groups[2].Value, year), "MMMM dd, yyyy", CultureInfo.InvariantCulture);
                        while (date.DayOfWeek != dayOfWeek)
                        {
                            date = date.AddDays(1);
                        }
                        return date;
                    }
                }
            };

            public CalendarDay(string dateExpression, IDictionary<string, object> properties, CultureInfo culture)
            {
                Name = properties.ContainsKey("name") ? ((IDictionary<object, object>)properties["name"])[culture.TwoLetterISOLanguageName].ToString() : (string)properties["_name"];
                var type = (string)(properties.ContainsKey("type") ? properties["type"] : "Holiday");
                Type = (DayType)Enum.Parse(typeof(DayType), Capitalize(type));

                foreach (var expr in DateExpressions)
                {
                    var match = expr.Pattern.Match(dateExpression);
                    if (match.Success)
                    {
                        ToDate = i => expr.ToDate(match, i);
                        return;
                    }
                }

                throw new Exception(string.Format("Unknown date expression \"{0}\".", dateExpression));
            }

            public string Name { get; private set; }
            public DayType Type { get; private set; }
            public Func<int, DateTime> ToDate { get; set; }

        }

        private static string Capitalize(string s)
        {
            return s.Substring(0, 1).ToUpper() + s.Substring(1);
        }

        private class DateExpression
        {
            public Regex Pattern { get; set; }
            public Func<Match, int, DateTime> ToDate { get; set; }
        }
    }
}
