using Prism.Ioc;
using SerialDebug.Views;
using System.Windows;
using SerialDebug.ViewModels;
using SerialDebug.Service;
using System.Text;
using SerialDebug.Views.Dialog;
using SerialDebug.ViewModels.Dialog;

namespace SerialDebug
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void OnInitialized()
        {
            // 出初始化MainView界面
            if (App.Current.MainWindow != null)
            {
                var mainService = App.Current.MainWindow.DataContext as IConfigureService;
                var mainWindowService = App.Current.MainWindow as IConfigureService;
                mainService?.configure();
                mainWindowService?.configure();
            }
            // 初始化ChartView界面


            // 初始化GBK编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            base.OnInitialized();
        }
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<MajorView, MajorViewModel>();
            containerRegistry.RegisterForNavigation<SettingView, SettingViewModel>();
            containerRegistry.RegisterForNavigation<MoreInfoView, MoreInfoViewModel>();
            containerRegistry.RegisterForNavigation<ChartView, ChartViewModel>();
            
            containerRegistry.RegisterDialog<ColorSelectorDialog, ColorSelectorDialogViewModel>();
        }
    }
}
