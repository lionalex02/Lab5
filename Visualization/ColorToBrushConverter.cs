using System;
using System.Windows.Data;
using System.Windows.Media;

namespace GraphEditor.Visualization;

public class ColorToBrushConverter : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
        if (!(value is Color))
            throw new InvalidOperationException("Value must be a Color");
        return new SolidColorBrush((Color)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
        throw new NotImplementedException();
    }

}