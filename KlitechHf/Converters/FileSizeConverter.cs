using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace KlitechHf.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var postFixes = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            var offset = 0;
            if (value is int i)
            {
                double size = i;
                while (size / 1024 >= 1)
                {
                    size /= 1024;
                    offset++;
                }

                return $"{size.ToString("0.#")} {postFixes[offset]}";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return 0;
        }
    }
}
