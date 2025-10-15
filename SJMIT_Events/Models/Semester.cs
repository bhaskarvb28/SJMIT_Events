using System;

namespace SJMIT_Events.Models
{
    public class Semester
    {
        public string SemesterId { get; set; }
        public string Name { get; set; }
        public string StartDate { get; set; }  // You can also use DateTime
        public string EndDate { get; set; }
        public bool IsCurrent { get; set; }
    }
}
