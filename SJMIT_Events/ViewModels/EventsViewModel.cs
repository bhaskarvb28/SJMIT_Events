using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SJMIT_Events.Models;
using SJMIT_Events.Services;
using SJMIT_Events.Storage;

namespace SJMIT_Events.ViewModels
{
    public class EventsViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<Event> _allEvents = new ObservableCollection<Event>();
        public ObservableCollection<Event> Events { get; set; } = new ObservableCollection<Event>();

        private readonly EventService _eventService = new EventService();
        private DateTime _lastManualRefresh = DateTime.MinValue;
        private bool _isRefreshing = false;
        private bool _isInitialized = false;

        public Semester _currentSemester;

        private string _viewFilter = "All";
        private DateTime _filterDate = DateTime.Today;
        
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EmptyMessage
        {
            get
            {
                return _viewFilter switch
                {
                    "Day" => $"No events found for {_filterDate:dd/MM/yyyy}",
                    "Week" => $"No events found for week of {_filterDate:dd MMM}",
                    "Month" => $"No events found for {_filterDate:MMM yyyy}",
                    _ => "No events found"
                };
            }
        }

        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        public bool IsInitialized => _isInitialized;

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            IsLoading = true;
            try
            {
                await LoadSemesterAsync();
                await LoadDataAsync();
                _isInitialized = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadSemesterAsync(bool useCache = true)
        {
            Semester semester = null;
            bool shouldFetchFromApi = false;

            if (useCache)
            {
                semester = LocalSemesterStore.LoadSemester();

                if (semester != null)
                {
                    var lastSemesterUpdate = LocalSemesterStore.GetLastUpdateTime();
                    var hoursSinceUpdate = (DateTime.Now - lastSemesterUpdate).TotalHours;

                    if (hoursSinceUpdate > 24)
                    {
                        System.Diagnostics.Debug.WriteLine($"Semester cache is {hoursSinceUpdate:F1} hours old - fetching fresh data");
                        shouldFetchFromApi = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Using cached semester data, last updated {hoursSinceUpdate:F1} hours ago");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No cached semester found");
                    shouldFetchFromApi = true;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Manual semester refresh - forcing API call");
                shouldFetchFromApi = true;
            }

            if (shouldFetchFromApi || semester == null)
            {
                try
                {
                    var semesterService = new SemesterService();
                    var freshSemester = await semesterService.GetCurrentSemesterAsync();

                    if (freshSemester != null)
                    {
                        semester = freshSemester;
                        LocalSemesterStore.SaveSemester(semester);
                        LocalSemesterStore.SetLastUpdateTime(DateTime.Now);
                        System.Diagnostics.Debug.WriteLine("Fetched fresh semester data from API");
                    }
                    else if (semester == null)
                    {
                        System.Diagnostics.Debug.WriteLine("API returned null semester and no cache available");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Semester API fetch failed: {ex.Message}");

                    if (semester == null)
                    {
                        semester = LocalSemesterStore.LoadSemester();
                        if (semester != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Using stale cached semester as fallback");
                        }
                    }
                }
            }

            if (semester != null &&
                !string.IsNullOrWhiteSpace(semester.StartDate) &&
                !string.IsNullOrWhiteSpace(semester.EndDate))
            {
                if (DateTime.TryParse(semester.StartDate, out var start))
                    StartDate = start;
                if (DateTime.TryParse(semester.EndDate, out var end))
                    EndDate = end;

                _currentSemester = semester;
                System.Diagnostics.Debug.WriteLine($"Semester loaded: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}");
            }
        }

        private async Task LoadDataAsync(bool useCache = true)
        {
            List<Event> events = new();
            bool shouldFetchFromApi = false;

            if (useCache)
            {
                var lastUpdate = LocalEventStore.GetLastUpdateTime();
                var hoursSinceUpdate = (DateTime.Now - lastUpdate).TotalHours;

                if (hoursSinceUpdate > 6)
                {
                    System.Diagnostics.Debug.WriteLine($"Cache is {hoursSinceUpdate:F1} hours old - fetching fresh data");
                    shouldFetchFromApi = true;
                }
                else
                {
                    events = LocalEventStore.LoadEvents()
                        .Where(e => e.Date >= StartDate && e.Date <= EndDate)
                        .OrderBy(e => e.Date)
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"Using cached data ({events.Count} events)");
                }
            }
            else
            {
                shouldFetchFromApi = true;
                System.Diagnostics.Debug.WriteLine("Manual refresh - forcing API call");
            }

            if (shouldFetchFromApi || events.Count == 0)
            {
                try
                {
                    var semesterEvents = await _eventService.GetEventsAsync(_currentSemester.SemesterId);
                    events = semesterEvents
                        .Where(e => e.SemesterId == _currentSemester.SemesterId)
                        .OrderBy(e => e.Date)
                        .ToList();

                    LocalEventStore.SaveEvents(events);
                    LocalEventStore.SetLastUpdateTime(DateTime.Now);
                    System.Diagnostics.Debug.WriteLine($"Fetched {events.Count} fresh events from API");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"API fetch failed: {ex.Message}");

                    if (events.Count == 0)
                    {
                        events = LocalEventStore.LoadEvents()
                            .Where(e => e.SemesterId == _currentSemester.SemesterId)
                            .OrderBy(e => e.Date)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"Using cached fallback ({events.Count} events)");
                    }
                }
            }

            _allEvents.Clear();
            foreach (var ev in events)
                _allEvents.Add(ev);

            ApplyFilter();
        }

        public async Task RefreshAsync()
        {
            if (_isRefreshing)
            {
                System.Diagnostics.Debug.WriteLine("Refresh already in progress, ignoring");
                return;
            }

            var timeSinceLastRefresh = DateTime.Now - _lastManualRefresh;
            if (timeSinceLastRefresh.TotalSeconds < 30)
            {
                var remainingSeconds = 30 - (int)timeSinceLastRefresh.TotalSeconds;
                System.Diagnostics.Debug.WriteLine($"Refresh cooldown: {remainingSeconds} seconds remaining");
                Application.Current.MainPage.DisplayAlert("Refresh Request", "Showing Latest Data", "Ok");
                return;
            }

            _isRefreshing = true;
            _lastManualRefresh = DateTime.Now;
            IsLoading = true;
            Events.Clear();

            try
            {
                System.Diagnostics.Debug.WriteLine("Manual refresh started");
                await LoadSemesterAsync(useCache: false);
                await LoadDataAsync(useCache: false);
                System.Diagnostics.Debug.WriteLine("Manual refresh completed");
            }
            finally
            {
                _isRefreshing = false;
                IsLoading = false;
            }
        }

        public void SetFilterAndDate(string filter, DateTime selectedDate)
        {
            if (!_isInitialized || IsLoading)
            {
                System.Diagnostics.Debug.WriteLine("Skipping filter - still loading data");
                return;
            }

            _viewFilter = filter;
            _filterDate = selectedDate;
            ApplyFilter();
            OnPropertyChanged(nameof(EmptyMessage)); // Notify UI that empty message changed
        }

        private void ApplyFilter()
        {
            if (_allEvents == null || !_allEvents.Any())
            {
                Events.Clear();
                return;
            }

            var filteredEvents = _viewFilter switch
            {
                "Day" => GetDayEvents(_filterDate),
                "Week" => GetWeekEvents(_filterDate),
                "Month" => GetMonthEvents(_filterDate),
                "All" => GetAllEvents(),
                _ => GetAllEvents()
            };

            Events.Clear();
            foreach (var ev in filteredEvents.OrderBy(e => e.Date))
                Events.Add(ev);

            System.Diagnostics.Debug.WriteLine($"Applied filter '{_viewFilter}' - showing {Events.Count} events");
        }

        private IEnumerable<Event> GetDayEvents(DateTime date)
        {
            return _allEvents.Where(e => e.Date.Date == date.Date);
        }

        private IEnumerable<Event> GetWeekEvents(DateTime date)
        {
            var startOfWeek = date.Date.AddDays(-(int)date.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(6);
            return _allEvents.Where(e => e.Date.Date >= startOfWeek && e.Date.Date <= endOfWeek);
        }

        private IEnumerable<Event> GetMonthEvents(DateTime date)
        {
            var startOfMonth = new DateTime(date.Year, date.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            return _allEvents.Where(e => e.Date.Date >= startOfMonth && e.Date.Date <= endOfMonth);
        }

        private IEnumerable<Event> GetAllEvents()
        {
            if (_currentSemester == null)
                return _allEvents;

            if (DateTime.TryParse(_currentSemester.StartDate, out var startDate) &&
                DateTime.TryParse(_currentSemester.EndDate, out var endDate))
            {
                return _allEvents.Where(e => e.Date.Date >= startDate.Date && e.Date.Date <= endDate.Date);
            }
            
            return _allEvents;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}