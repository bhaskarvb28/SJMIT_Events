using System.Globalization;

namespace SJMIT_Events.Converters
{
    public class DateToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime eventDate)
            {
                return eventDate.ToString("h:mm tt");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}