using System.Globalization;

namespace SJMIT_Events.Converters
{
    public class DateToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime eventDate)
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                if (eventDate.Date < today)
                    return "ENDED";
                else if (eventDate.Date == today)
                    return "TODAY";
                else if (eventDate.Date == tomorrow)
                    return "TOMORROW";
                else
                    return "UPCOMING";
            }
            return "UPCOMING";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}