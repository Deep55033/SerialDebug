using System.Globalization;
using System.Windows.Controls;

namespace SerialDebug.Common.Domain
{
    public class NotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return string.IsNullOrWhiteSpace((value ?? "").ToString())
                ? new ValidationResult(false, "该值不能为空")
                : ValidationResult.ValidResult;
        }
    }
}