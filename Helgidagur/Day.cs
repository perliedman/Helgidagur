using System;
using System.Collections.Generic;
using System.Text;

namespace Helgidagur
{
    public class Day
    {
        public string Name { get; set; }
        public DayType Type { get; set; }
        public DateTime Date { get; set; }
    }

    public enum DayType
    {
        Observance,
        Holiday
    }
}
