using SerialDebug.Common.Model;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SerialDebug.VaildationRules
{
    public class NameNotEmptyAndRepeatValidtionRule : ValidationRule
    {
        public ValidationParams Parmams { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrEmpty((string)value) || string.IsNullOrWhiteSpace((string)value))
            {
                return new ValidationResult(false, "不能为空");
            }

            foreach (var item in (ObservableCollection<ChartDataModel>)Parmams.Source)
            {
                if (item.Name == (string)value)
                {
                    return new ValidationResult(false, "名字重复");
                }
            }

            return ValidationResult.ValidResult;
        }
    }

    public class ValidationParams : DependencyObject
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Source", typeof(object), typeof(ValidationParams), new FrameworkPropertyMetadata(null));

        public object Source
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }
    }
}