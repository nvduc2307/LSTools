using System.Windows.Data;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Support.Wpf
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : value;
        }
    }
}

