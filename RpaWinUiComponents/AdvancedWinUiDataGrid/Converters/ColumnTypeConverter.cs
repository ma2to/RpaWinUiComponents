//Converters/ColumnTypeConverter.cs - Nový converter pre rozpoznanie typu stĺpca
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Converters
{
    /// <summary>
    /// Converter ktorý rozpozná typ stĺpca a vráti Visibility pre príslušný UI element
    /// </summary>
    internal class ColumnTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var columnName = value?.ToString() ?? "";
            var expectedType = parameter?.ToString() ?? "";

            switch (expectedType)
            {
                case "DeleteAction":
                    return columnName == "DeleteAction" ? Visibility.Visible : Visibility.Collapsed;

                case "ValidAlerts":
                    return columnName == "ValidAlerts" ? Visibility.Visible : Visibility.Collapsed;

                case "Normal":
                default:
                    return (columnName != "DeleteAction" && columnName != "ValidAlerts")
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}