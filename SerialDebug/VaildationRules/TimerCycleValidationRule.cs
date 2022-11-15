using System.Globalization;
using System.Windows.Controls;

namespace SerialDebug.VaildationRules
{
    public class TimerCycleValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if ((((string)value)!).Length == 0) return new ValidationResult(false, "不能为空");
            try
            {
                var res = int.Parse((string)value ?? string.Empty);
                return ValidationResult.ValidResult;
            }
            catch
            {
                return new ValidationResult(false, "非法的输入");
            }
        }
    }
}