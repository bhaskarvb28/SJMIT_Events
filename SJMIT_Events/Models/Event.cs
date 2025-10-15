using System;

namespace SJMIT_Events.Models
{
    public class Event
    {
        public string EventId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string SemesterId { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
    }
}
