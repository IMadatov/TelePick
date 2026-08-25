using Avalonia.Data.Converters;
using System;
using System.Globalization;
using TelePick.Desktop.Models;
using Material.Icons;

namespace TelePick.Desktop.Converters
{
    public class ClipboardItemTypeToIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ClipboardItemType type)
            {
                return type switch
                {
                    ClipboardItemType.Text => MaterialIconKind.CodeBraces,
                    ClipboardItemType.Image => MaterialIconKind.Image,
                    ClipboardItemType.Files => MaterialIconKind.FileDocument,
                    _ => MaterialIconKind.ClipboardText
                };
            }
            return MaterialIconKind.HelpCircleOutline;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
