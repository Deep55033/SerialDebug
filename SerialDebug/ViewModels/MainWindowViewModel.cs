using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using SerialDebug.Common.Model;
using SerialDebug.Core;
using SerialDebug.Service;

namespace SerialDebug.ViewModels;

public class MainWindowViewModel : BindableBase, IConfigureService
{
    private readonly IRegionManager _regionManager;

    private MenuBarItem _homeBarItem;
    private MenuBarItem _chartBarItem;
    private MenuBarItem _settingBarItem;
    private MenuBarItem _moreInfoBarItem;

    public MenuBarItem HomeBarItem
    {
        get => _homeBarItem;
        set
        {
            if (Equals(value, _homeBarItem)) return;
            _homeBarItem = value;
            RaisePropertyChanged();
        }
    }

    public MenuBarItem ChartBarItem
    {
        get => _chartBarItem;
        set
        {
            if (Equals(value, _chartBarItem)) return;
            _chartBarItem = value;
            RaisePropertyChanged();
        }
    }

    public MenuBarItem SettingBarItem
    {
        get => _settingBarItem;
        set
        {
            if (Equals(value, _settingBarItem)) return;
            _settingBarItem = value;
            RaisePropertyChanged();
        }
    }

    public MenuBarItem MoreInfoBarItem
    {
        get => _moreInfoBarItem;
        set
        {
            if (Equals(value, _moreInfoBarItem)) return;
            _moreInfoBarItem = value;
            RaisePropertyChanged();
        }
    }

    // 切换主题
    private bool _isDarkTheme = false;

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (value == _isDarkTheme) return;
            _isDarkTheme = value;
            RaisePropertyChanged();
            ModifyTheme(value);
        }
    }

    // 右边抽屉状态
    private bool _isRightDrawerOpen = false;

    public bool IsRightDrawerOpen
    {
        get => _isRightDrawerOpen;
        set
        {
            if (value == _isRightDrawerOpen) return;
            _isRightDrawerOpen = value;
            RaisePropertyChanged();
        }
    }

    // 打开命令
    public DelegateCommand OpenRightDrawCommand { get; set; }

    public MainWindowViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;

        OpenRightDrawCommand = new DelegateCommand(() => { IsRightDrawerOpen = true; });

        NavigateCommand = new DelegateCommand<MenuBarItem>(item =>
        {
            if (item == null) return;
            _regionManager.Regions[PrismRegion.MainContentRegionName]?.RequestNavigate(item.ViewNameSpace);
        });
    }

    public DelegateCommand<MenuBarItem> NavigateCommand { get; set; }

    // 初始化配置函数
    public void configure()
    {
        CreateMenuBar();
        // 初始起始界面
        _regionManager.Regions[PrismRegion.MainContentRegionName]?.RequestNavigate("MajorView");
    }

    private void CreateMenuBar()
    {
        HomeBarItem = (new MenuBarItem { Icon = "Home", Title = "主页", ViewNameSpace = "MajorView" });
        ChartBarItem = (new MenuBarItem { Icon = "ChartLine", Title = "波形图", ViewNameSpace = "ChartView" });
        MoreInfoBarItem = (new MenuBarItem { Icon = "Information", Title = "关于软件", ViewNameSpace = "MoreInfoView" });
    }

    private static void ModifyTheme(bool isDarkTheme)
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();

        theme.SetBaseTheme(isDarkTheme ? Theme.Dark : Theme.Light);
        paletteHelper.SetTheme(theme);
    }
}