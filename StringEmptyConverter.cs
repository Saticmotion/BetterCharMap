using System.Windows.Data;
using System;
using System.Globalization;

namespace BetterCharMap;

public class StringEmptyConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return string.IsNullOrEmpty((string)value) ? parameter : value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}

}