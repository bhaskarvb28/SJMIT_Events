namespace SJMIT_Events.Views.Controls;

public partial class AddressBar : ContentView
{
    //public EventHandler DashboardRequested;
    private Dashboard _dashboard;
    public AddressBar()
    {
        InitializeComponent();
    }

    private void OnDashboardIconTapped(object sender, EventArgs e)
    {
        _dashboard.ShowDashboard();
    }

    public void SetDashboardControl(Dashboard dashboard)
    {
        _dashboard = dashboard;
    }
}


