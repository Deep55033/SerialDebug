using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SerialDebug.Extension;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;

namespace SerialDebug.ViewModels.Dialog
{
    public class ColorSelectorDialogViewModel : BindableBase, IDialogAware
    {
        private ColorScheme _activeScheme;

        public ColorScheme ActiveScheme
        {
            get => _activeScheme;
            set
            {
                if (_activeScheme != value)
                {
                    _activeScheme = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color? _selectedColor;

        public Color? SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    RaisePropertyChanged();

                    // if we are triggering a change internally its a hue change and the colors will match
                    // so we don't want to trigger a custom color change.
                    var currentSchemeColor = ActiveScheme switch
                    {
                        ColorScheme.Primary => _primaryColor,
                        ColorScheme.Secondary => _secondaryColor,
                        ColorScheme.PrimaryForeground => _primaryForegroundColor,
                        ColorScheme.SecondaryForeground => _secondaryForegroundColor,
                        _ => throw new NotSupportedException($"{ActiveScheme} is not a handled ColorScheme.. Ye daft programmer!")
                    };

                    if (_selectedColor != currentSchemeColor && value is Color color)
                    {
                        ChangeCustomColor(color);
                    }
                }
            }
        }

        private void ChangeCustomColor(object? obj)
        {
            var color = (Color)obj!;
            _paletteHelper.ChangePrimaryColor(color);
            _primaryColor = color;
        }

        public IEnumerable<ISwatch> Swatches { get; } = SwatchHelper.Swatches;

        private readonly PaletteHelper _paletteHelper = new PaletteHelper();

        public ICommand ChangeHueCommand { get; }

        private Color? _primaryColor;

        private Color? _secondaryColor;

        private Color? _primaryForegroundColor;

        private Color? _secondaryForegroundColor;

        private void ChangeHue(object? obj)
        {
            var hue = (Color)obj!;
            CloseDialog(hue.ToString());
        }

        public string Title => "选择颜色";

        public event Action<IDialogResult> RequestClose;

        public ColorSelectorDialogViewModel()
        {
            ChangeHueCommand = new DelegateCommand<object>(ChangeHue);
        }

        public void CloseDialog(string color)
        {
            if (color == null)
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.No));
                return;
            }
            var par = new DialogParameters();
            par.Add("color", color);
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, par));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }
    }
}