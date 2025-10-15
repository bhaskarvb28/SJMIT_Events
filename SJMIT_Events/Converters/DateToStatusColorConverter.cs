using System.Globalization;
using Microsoft.Maui.Graphics;

namespace SJMIT_Events.Converters
{
    public class DateToStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime eventDate)
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                if (eventDate.Date < today)
                    return Color.FromArgb("#EA4335"); // Red - Overdue
                else if (eventDate.Date == today)
                    return Color.FromArgb("#34A853"); // Green - Today
                else if (eventDate.Date == tomorrow)
                    return Color.FromArgb("#4285F4"); // Blue - Tomorrow
                else
                    return Color.FromArgb("#FF9800"); // Orange - Upcoming
            }
            return Color.FromArgb("#FF9800"); // Default orange
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}