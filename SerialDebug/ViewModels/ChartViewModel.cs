using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using SerialDebug.Common.Model;
using SerialDebug.Communication;
using SerialDebug.Extension;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SerialDebug.Core;
using SerialDebug.Service;

namespace SerialDebug.ViewModels
{
    public class ChartViewModel : BindableBase, INavigationAware, IConfigureService
    {
        // 是否停止接受
        private bool _isStopReceiveData = false;

        public bool IsStopReceiveData
        {
            get => _isStopReceiveData;
            set
            {
                _isStopReceiveData = value;
                RaisePropertyChanged();
            }
        }

        private string _addName = "data";

        public string AddName
        {
            get => _addName;
            set
            {
                _addName = value;
                RaisePropertyChanged();
            }
        }

        private int _addThinkess = 1;

        public int AddThinkess
        {
            get { return _addThinkess; }
            set
            {
                _addThinkess = value;
                RaisePropertyChanged();
            }
        }

        private string _addColor = "#ffffff";

        public string AddColor
        {
            get => _addColor;
            set
            {
                _addColor = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<ChartDataModel> _chartDataModels;

        public ObservableCollection<ChartDataModel> ChartDataModels
        {
            get => _chartDataModels;
            set
            {
                _chartDataModels = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<ISeries> _seriesSource;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogService _dialogService;
        private readonly IRegionManager _regionManager;

        public ObservableCollection<ISeries> SeriesSource
        {
            get => _seriesSource;
            set
            {
                _seriesSource = value;
                RaisePropertyChanged();
            }
        }

        public Axis[] XAxes { get; set; } =
        {
            new Axis
            {
                SeparatorsPaint = new SolidColorPaint
                {
                    Color = SKColors.Gray,
                    StrokeThickness = 1
                }
            }
        };

        public Axis[] YAxes { get; set; } =
        {
            new Axis
            {
                SeparatorsPaint = new SolidColorPaint
                {
                    Color = SKColors.Gray,
                    StrokeThickness = 1,
                }
            }
        };

        public DelegateCommand<string> RemoveSourceItemCommand { get; set; }

        public DelegateCommand<string> OpenColorSelectorDialog { get; set; }

        public DelegateCommand<ChartDataModel> UpdateChartDataSourceName { get; set; }
        public DelegateCommand<ChartDataModel> UpdateChartDataSourceThinkess { get; set; }

        public DelegateCommand AddChartDataSourceCommand { get; set; }

        public DelegateCommand ClearChartDataSourceCommand { get; set; }

        // 添加确认命令
        public DelegateCommand AddChartDataSourceAccept { get; set; }

        // 添加取消命令
        public DelegateCommand AddChartDataSourceCancel { get; set; }

        // 清除波形命令
        public DelegateCommand ClearReceiveDataCommand { get; set; }

        public DelegateCommand ResetChartXAndY { get; set; }

        public ChartViewModel(IEventAggregator eventAggregator, IDialogService dialogService, IRegionManager regionManager)
        {
            this._eventAggregator = eventAggregator;
            this._dialogService = dialogService;
            _regionManager = regionManager;

            SeriesSource = new ObservableCollection<ISeries>();
            ChartDataModels = new ObservableCollection<ChartDataModel>();

            OpenColorSelectorDialog = new DelegateCommand<string>((name) =>
            {
                _dialogService.ShowDialog("ColorSelectorDialog", r =>
                {
                    if (r.Result == ButtonResult.OK)
                    {
                        var color = r.Parameters.GetValue<string>("color");
                        if (name != null)
                        {
                            UpdateChartDataSourceColor(name, color);
                        }
                        else
                        {
                            AddColor = color;
                        }
                    }
                });
            });

            // 名字更新命令
            UpdateChartDataSourceName = new DelegateCommand<ChartDataModel>((model) =>
            {
                var index = ChartDataModels.IndexOf(model);
                SeriesSource[index].Name = model.Name;
            });

            // 粗细更新命令
            UpdateChartDataSourceThinkess = new DelegateCommand<ChartDataModel>((model) =>
            {
                var index = ChartDataModels.IndexOf(model);
                var stroke = ((LineSeries<ObservableValue>)SeriesSource[index]).Stroke;
                if (stroke != null)
                    stroke.StrokeThickness = model.Thickness;
            });

            RemoveSourceItemCommand = new DelegateCommand<string>(RemoveChartDataSource);

            // 清除所有数据源
            ClearChartDataSourceCommand = new DelegateCommand(() =>
            {
                ChartDataModels.Clear();
                SeriesSource.Clear();
            });

            // 添加数据源
            AddChartDataSourceCommand = new DelegateCommand(() => { });

            // 添加取消命令
            AddChartDataSourceCancel = new DelegateCommand(() =>
            {
                // 恢复默认值
                AddColor = "#ffffff";
                AddName = "data";
                AddThinkess = 1;
                // 关闭对话框
                _eventAggregator.AddChartDataSourceDialogEventClose();
            });

            // 添加确认命令
            AddChartDataSourceAccept = new DelegateCommand(() =>
            {
                if (ChartSourceNameIsContainer(AddName))
                {
                    return;
                }

                RegisterDisplaySeries(AddName, AddColor, AddThinkess);
                // 恢复默认值
                AddColor = "#ffffff";
                AddName = "data";
                AddThinkess = 1;
                // 关闭对话框
                _eventAggregator.AddChartDataSourceDialogEventClose();
            });

            ClearReceiveDataCommand = new DelegateCommand(() =>
            {
                foreach (var item in ChartDataModels)
                {
                    item.DataSourece.Clear();
                }
            });

            // 复位图标
            ResetChartXAndY = new DelegateCommand(() =>
            {
                XAxes[0].MaxLimit = null;
                XAxes[0].MinLimit = null;

                YAxes[0].MaxLimit = null;
                YAxes[0].MinLimit = null;
            });

            //CreateTestData();
        }

        private bool ChartSourceNameIsContainer(string name)
        {
            return ChartDataModels.Any(item => item.Name == name);
        }

        private void RegisterDisplaySeries(string name, string color, int think)
        {
            var chartDataModel = new ChartDataModel
            {
                Name = name,
                Color = color,
                Thickness = think,
                DataSourece = new SynchronizedObservableCollection<ObservableValue>()
            };

            var lineSeries = new LineSeries<ObservableValue>
            {
                Values = chartDataModel.DataSourece,
                Name = chartDataModel.Name,
                Stroke = new SolidColorPaint(SKColor.Parse(chartDataModel.Color))
                    { StrokeThickness = chartDataModel.Thickness },
                GeometrySize = 0,
                Fill = null,
            };

            // 添加数据源
            SeriesSource.Add(lineSeries);
            ChartDataModels.Add(chartDataModel);
        }

        private ChartDataModel FindChartModel(string name)
        {
            return ChartDataModels.FirstOrDefault(item => item.Name == name);
        }

        private ISeries FindSeries(string name)
        {
            return SeriesSource.FirstOrDefault(item => item.Name == name);
        }

        // 删除数据源
        private void RemoveChartDataSource(string name)
        {
            try
            {
                ChartDataModels.Remove(FindChartModel(name));
                SeriesSource.Remove(FindSeries(name));
                _eventAggregator.MainSnakerbarMessageSendSuccessMsg("删除成功");
            }
            catch
            {
                _eventAggregator.MainSnakerbarMessageSendErrorMsg("删除失败");
            }
        }

        // 更新数据源颜色
        private void UpdateChartDataSourceColor(string name, string color)
        {
            FindChartModel(name).Color = color;
            (((Paint)((LineSeries<ObservableValue>)FindSeries(name)).Stroke)!).Color = SKColor.Parse(color);
        }

        private void PutChartData(string name, double data)
        {
            var model = FindChartModel(name);
            if (model == null) return;
            if (model.DataSourece.Count > 2000)
            {
                model.DataSourece.RemoveAt(0);
            }

            model.DataSourece.Add(new ObservableValue(data));
        }

        // 用于创建测试数据
        private void CreateTestData()
        {
            for (var i = 1; i < 4; i++)
            {
                RegisterDisplaySeries("name" + i, SKColors.AliceBlue.ToString(), 1);
            }
        }

        private bool _isRead = false;

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            SerialPortCommon.RegisterReceviceDataEvent(SerialReceiveEventHandler);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            SerialPortCommon.UnRegisterReceiveDataEvent(SerialReceiveEventHandler);
        }

        private void ReadDataThread()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        // 串口打开且未暂停接收
                        if (!_isRead || !SerialPortCommon.IsOpen()) continue;

                        // 清除串口缓存
                        if (IsStopReceiveData)
                        {
                            SerialPortCommon.ClearReadBuffer();
                            //SerialPortCommon.ClearOutBuffer();
                            continue;
                        }

                        var readStr = SerialPortCommon.ReadLine();
                        ParseStringToChartTransportAndSend(readStr);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // 串口关闭处理
                        // 发送错误消息
                        _eventAggregator.MainSnakerbarMessageSendErrorMsg(ex.Message);
                        // 更改按钮样式
                        _eventAggregator.SerialPortConnectClose();
                    }
                    catch (TimeoutException)
                    {
                        // 超时
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        private readonly object _readLock = new();

        private void SerialReceiveEventHandler(object sender, SerialDataReceivedEventArgs e)
        {
            Task.Run(() =>
            {
                lock (_readLock)
                {
                    try
                    {
                        // 串口打开且未暂停接收
                        if (!SerialPortCommon.IsOpen()) return;

                        // 清除串口缓存
                        if (IsStopReceiveData)
                        {
                            SerialPortCommon.ClearReadBuffer();
                            return;
                        }

                        var readStr = SerialPortCommon.ReadLine();
                        ParseStringToChartTransportAndSend(readStr);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // 串口关闭处理
                        // 发送错误消息
                        _eventAggregator.MainSnakerbarMessageSendErrorMsg(ex.Message);
                        // 更改按钮样式
                        _eventAggregator.SerialPortConnectClose();
                    }
                    catch (TimeoutException)
                    {
                        // 超时
                    }
                }
            });
        }

        // 懂得都懂
        private readonly Regex _regex = new("([\\w]+)[\\s]*=[\\s]*([\\d.+-]+)");

        private void ParseStringToChartTransportAndSend(string msg)
        {
            try
            {
                var matches = _regex.Matches(msg);
                for (var i = 0; i < matches.Count; i++)
                {
                    var name = matches[i].Groups[1].Value;
                    double data = double.Parse(matches[i].Groups[2].Value);
                    PutChartData(name, data);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void configure()
        {
            _regionManager.Regions[PrismRegion.MainContentRegionName].RequestNavigate("ChartView");
        }
    }
}