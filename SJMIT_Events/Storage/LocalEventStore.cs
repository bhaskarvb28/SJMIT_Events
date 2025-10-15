using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SJMIT_Events.Models;

namespace SJMIT_Events.Storage
{
    public static class LocalEventStore
    {
        private static string EventsFilePath =>
            Path.Combine(FileSystem.AppDataDirectory, "events.json");

        private static string TimestampFilePath =>
            Path.Combine(FileSystem.AppDataDirectory, "events_timestamp.txt");

        public static void SaveEvents(List<Event> events)
        {
            var json = JsonSerializer.Serialize(events);
            File.WriteAllText(EventsFilePath, json);
        }

        public static List<Event> LoadEvents()
        {
            if (!File.Exists(EventsFilePath))
                return new List<Event>();

            var json = File.ReadAllText(EventsFilePath);
            return JsonSerializer.Deserialize<List<Event>>(json) ?? new List<Event>();
        }

        public static void SetLastUpdateTime(DateTime timestamp)
        {
            File.WriteAllText(TimestampFilePath, timestamp.ToString("o"));
        }

        public static DateTime GetLastUpdateTime()
        {
            if (!File.Exists(TimestampFilePath))
                return DateTime.MinValue; // force refresh

            var timestampString = File.ReadAllText(TimestampFilePath);
            return DateTime.TryParse(timestampString, out DateTime timestamp)
                ? timestamp
                : DateTime.MinValue;
        }
    }
}
