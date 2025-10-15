using Microsoft.Maui.Controls;
using SJMIT_Events.ViewModels;
using System;
using System.Threading.Tasks;

namespace SJMIT_Events.Views.Controls
{
    public partial class Events : ContentView
    {
        private readonly EventsViewModel _viewModel;

        // 🔑 Public property to expose the ViewModel
        public EventsViewModel ViewModel => _viewModel;

        public Task InitializationTask { get; }

        public Events()
        {
            InitializeComponent();
            _viewModel = new EventsViewModel();
            BindingContext = _viewModel;
            InitializationTask = InitializeAsync(); // save task
        }

        private async Task InitializeAsync()
        {
            try
            {
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing events: {ex.Message}");
            }
        }
    }
}