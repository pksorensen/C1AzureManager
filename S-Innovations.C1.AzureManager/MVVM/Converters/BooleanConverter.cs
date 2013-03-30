using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace S_Innovations.C1.AzureManager.MVVM.Converters
{
     public class BooleanConverter: IValueConverter {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
           return ((value as string).Equals("true")) ? true : false;
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
           return (bool)value ? "true" : "false";
      }
 }
}
