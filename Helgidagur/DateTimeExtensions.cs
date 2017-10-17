using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Helgidagur
{
    public static class DateTimeExtensions
    {
        public static bool IsHoliday(this DateTime dateTime, CultureInfo culture)
        {
            var key = new Tuple<CultureInfo, int>(culture, dateTime.Year);
            var calendar = Get(culture);
            if (calendar.Dayoff == dateTime.DayOfWeek)
            {
                return true;
            }

            var days = CalendarYearCache.Get(key);
            if (days == null)
            {
                days = calendar.GetDays(dateTime.Year).Where(d => d.Type == DayType.Holiday);
                CalendarYearCache.Add(key, days);
            }

            return days.Any(d => d.Date == dateTime);
        }

        public static HolidayCalendar Get(CultureInfo culture)
        {
            HolidayCalendar calendar;
            if (!CalendarCache.TryGetValue(culture, out calendar))
            {
                calendar = CalendarCache[culture] = new HolidayCalendar(culture);
            }

            return calendar;
        }

        private static readonly IDictionary<CultureInfo, HolidayCalendar> CalendarCache = new ConcurrentDictionary<CultureInfo, HolidayCalendar>();
        private static readonly LRUCache<Tuple<CultureInfo, int>, IEnumerable<Day>> CalendarYearCache = new LRUCache<Tuple<CultureInfo, int>, IEnumerable<Day>>(20);
    }
}
