using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;

namespace SerialDebug.ViewModels
{
    internal class NotificationDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return false;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }
    }
}