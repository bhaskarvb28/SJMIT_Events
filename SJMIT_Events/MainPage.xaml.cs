using SJMIT_Events.ViewModels;

namespace SJMIT_Events;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        Initialize();
    }

    private async void Initialize()
    {
        await EventsControl.InitializationTask;  // wait for ViewModel setup
        DashboardControl.SetEventsViewModel(EventsControl.ViewModel);
        AddressBarControl.SetDashboardControl(DashboardControl);
    }
}
