using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;

namespace SerialDebug.ViewModels.Dialog
{
    internal class AddChartDataDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => throw new NotImplementedException();

        public event Action<IDialogResult> RequestClose;

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