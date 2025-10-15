using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using SJMIT_Events.Models;

namespace SJMIT_Events.Services
{
    public class SemesterService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "https://semester_API_URL";

        // Get current semester
        public async Task<Semester> GetCurrentSemesterAsync()
        {
            var json = await _httpClient.GetStringAsync($"{ApiBaseUrl}?isCurrent=true");

            using var doc = JsonDocument.Parse(json);
            var semestersJson = doc.RootElement.GetProperty("semesters").GetRawText();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var semesters = JsonSerializer.Deserialize<List<Semester>>(semestersJson, options);

            return semesters?.FirstOrDefault();
        }



        // Get all semesters
        public async Task<List<Semester>> GetAllSemestersAsync()
        {
            var json = await _httpClient.GetStringAsync(ApiBaseUrl);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Deserialize<List<Semester>>(json, options) ?? new List<Semester>();
        }
    }
}
