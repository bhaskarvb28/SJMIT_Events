using System.IO;
using System.Text.Json;
using SJMIT_Events.Models;

namespace SJMIT_Events.Storage
{
    public static class LocalSemesterStore
    {
        private static readonly string FilePath =
            Path.Combine(FileSystem.AppDataDirectory, "current_semester.json");

        private const string SEMESTER_UPDATE_TIME_KEY = "semester_last_update";

        public static void SaveSemester(Semester semester)
        {
            var json = JsonSerializer.Serialize(semester);
            File.WriteAllText(FilePath, json);
        }

        public static Semester? LoadSemester()
        {
            if (!File.Exists(FilePath))
                return null;

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Semester>(json);
        }

        public static void SetLastUpdateTime(DateTime updateTime)
        {
            Preferences.Set(SEMESTER_UPDATE_TIME_KEY, updateTime.ToBinary());
        }

        public static DateTime GetLastUpdateTime()
        {
            var binaryTime = Preferences.Get(SEMESTER_UPDATE_TIME_KEY, DateTime.MinValue.ToBinary());
            return DateTime.FromBinary(binaryTime);
        }
    }
}