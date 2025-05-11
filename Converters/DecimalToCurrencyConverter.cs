using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace App1.Converters
{
    public class DecimalToCurrencyConverter : IValueConverter
    {
        private static readonly CultureInfo RussianCulture = new CultureInfo("ru-RU");

        // Конвертирует decimal в строку валюты (рубли)
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is decimal price)
            {
                return price.ToString("C", RussianCulture); // "C" - формат валюты для ru-RU
            }
            return value?.ToString() ?? string.Empty; // Возвращаем строку или пустую строку, если null
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}