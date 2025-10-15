using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SJMIT_Events.ViewModels;

namespace SJMIT_Events.Views.Controls
{
    public partial class Dashboard : ContentView
    {
        private const double MENU_WIDTH = 280; // Match the width from XAML
        private EventsViewModel _eventsViewModel; // Passed in from MainPage

        private string _viewFilter = "All";

        public Dashboard()
        {
            InitializeComponent();
            SetCurrentDate();

            DashboardMenu.IsVisible = false;
            DashboardOverlay.IsVisible = false;

            // Set initial position immediately
            DashboardMenu.TranslationX = -MENU_WIDTH;

            // Listen for theme changes
            Application.Current.RequestedThemeChanged += OnThemeChanged;
        }

        private void SetCurrentDate()
        {
            var today = DateTime.Today;
            var dayName = today.ToString("dddd");
            var fullDate = today.ToString("MMMM d, yyyy");
            CurrentDateLabel.Text = $"{dayName}, {fullDate}";

            // Set initial date for Date Picker
            EventDatePicker.Date = DateTime.Today;
            EventDatePicker.Format = "dd MMM yyyy";
        }

        public async Task ShowDashboard()
        {
            DashboardOverlay.Opacity = 0;
            DashboardOverlay.IsVisible = true;
            DashboardMenu.IsVisible = true;

            if (DashboardMenu.Width <= 0)
            {
                await Task.Delay(1);
            }

            double menuWidth = DashboardMenu.Width > 0 ? DashboardMenu.Width : MENU_WIDTH;
            DashboardMenu.TranslationX = -menuWidth;

            var overlayTask = DashboardOverlay.FadeTo(0.3, 300);
            var slideTask = DashboardMenu.TranslateTo(0, 0, 350, Easing.SinOut);

            await Task.WhenAll(overlayTask, slideTask);
        }

        public async void HideDashboard(object sender, EventArgs e)
        {
            double menuWidth = DashboardMenu.Width > 0 ? DashboardMenu.Width : MENU_WIDTH;
            var slideTask = DashboardMenu.TranslateTo(-menuWidth, 0, 350, Easing.SinIn);
            var fadeTask = DashboardOverlay.FadeTo(0, 350, Easing.Linear);

            await Task.WhenAll(slideTask, fadeTask);

            DashboardOverlay.IsVisible = false;
            DashboardMenu.IsVisible = false;
        }

        // Called by MainPage to connect the EventsViewModel
        public void SetEventsViewModel(EventsViewModel viewModel)
        {

            _eventsViewModel = viewModel;
            BindingContext = _eventsViewModel;

            //Sync initial values when we first attach
            _eventsViewModel.SetFilterAndDate(_viewFilter, EventDatePicker.Date);
        }

        private void OnDatePickerDateSelected(object sender, DateChangedEventArgs e)
        {
            // 🔑 Notify EventsViewModel if it's set
            _eventsViewModel.SetFilterAndDate(_viewFilter, EventDatePicker.Date);
        }

        private void OnViewFilterClicked(object sender, EventArgs e)
        {
            if (sender is not Button button)
                return;

            var selectedFilter = button.CommandParameter?.ToString();
            if (string.IsNullOrEmpty(selectedFilter))
                return;

            _viewFilter = selectedFilter;
            UpdateFilterButtonStyles(selectedFilter);

            // 🔑 Notify EventsViewModel if it's set
            _eventsViewModel.SetFilterAndDate(_viewFilter, EventDatePicker.Date);
        }

        private void UpdateFilterButtonStyles(string selectedFilter)
        {
            var selectedLight = Color.FromArgb("#E3F2FD");
            var selectedDark = Color.FromArgb("#1565C0");
            var unselectedLight = Color.FromArgb("#F8F9FA");
            var unselectedDark = Color.FromArgb("#242424");

            Color GetThemeColor(Color light, Color dark)
                => Application.Current.RequestedTheme == AppTheme.Dark ? dark : light;

            DayFilterBorder.BackgroundColor = selectedFilter == "Day"
                ? GetThemeColor(selectedLight, selectedDark)
                : GetThemeColor(unselectedLight, unselectedDark);

            WeekFilterBorder.BackgroundColor = selectedFilter == "Week"
                ? GetThemeColor(selectedLight, selectedDark)
                : GetThemeColor(unselectedLight, unselectedDark);

            MonthFilterBorder.BackgroundColor = selectedFilter == "Month"
                ? GetThemeColor(selectedLight, selectedDark)
                : GetThemeColor(unselectedLight, unselectedDark);

            AllFilterBorder.BackgroundColor = selectedFilter == "All"
                ? GetThemeColor(selectedLight, selectedDark)
                : GetThemeColor(unselectedLight, unselectedDark);
        }

        private void OnThemeChanged(object sender, AppThemeChangedEventArgs e)
        {
            // Update filter button styles when theme changes
            UpdateFilterButtonStyles(_viewFilter);
        }

        private async void OnRefreshButtonTapped(object sender, EventArgs e)
        {

            if (sender is Border border)
            {
                // Simple "press" animation
                await border.ScaleTo(0.95, 100, Easing.CubicIn);   // shrink slightly
                await border.ScaleTo(1, 100, Easing.CubicOut);     // return to normal
            }

            HideDashboard(this, EventArgs.Empty);

            // Call refresh on the events viewmodel
            if (_eventsViewModel != null)
            {
                await _eventsViewModel.RefreshAsync();
            }
        }
    }
}








