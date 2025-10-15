using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SJMIT_Events.Models;

namespace SJMIT_Events.Services
{
    public class EventService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<List<Event>> GetEventsAsync(string semesterId)
        {
            //// Format dates as yyyy-MM-dd for query parameters
            //string start = startDate.ToString("yyyy-MM-dd");
            //string end = endDate.ToString("yyyy-MM-dd");

            // Build URL with query parameters
            var url = $"https://events_API_url";

            var json = await _httpClient.GetStringAsync(url);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<List<Event>>(json, options) ?? new List<Event>();
        }
    }
}
