using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Regions.Behaviors;
using Prism.Services.Dialogs;
using SerialDebug.Common.Model;
using SerialDebug.Communication;
using SerialDebug.Extension;
using SerialDebug.Utils;
using Timer = System.Timers.Timer;

namespace SerialDebug.ViewModels;

public class MajorViewModel : BindableBase, INavigationAware
{
    private readonly IDialogService _dialogService;
    private readonly IEventAggregator _eventAggregator;

    private readonly object _lock = new();

    // 定时发送定时器
    private readonly Timer _sendTimer = new();

    private ObservableCollection<string> _baudRateList;

    // 字符编码格式
    private ObservableCollection<string> _characterEncodings;

    private SerialPortItem _connctingSerialPort;

    // 当前选中的波特率
    private string _curSelectedBaudRate;

    // 当前选中的字符编码格式
    private string _curSelectedCharacterEncoding;

    // 当前选中的数据位
    private string _curSelectedDataBit;

    // 当前选中的校验位
    private string _curSelectedParity;

    // 当前选中的串口号
    private SerialPortItem _curSelectedSerialPortItem;

    // 当前选中的停止位
    private string _curSelectedStop;

    // 数据位列表
    private ObservableCollection<string> _dataBitList;

    // DTR 状态
    private bool _DTRCheck;

    // 是否使用十六进制发送

    private bool _isOpeningUpdateSerial;

    private bool _isRead = true;

    // 是否发送新行
    private bool _isSendNewLine;

    // 是否开启定时发送
    private bool _isTimerSend;

    private bool _isUpdateSerialList = false;

    // 打开串口命令
    private DelegateCommand _openSerialPortCommand;

    // 校验位列表
    private ObservableCollection<string> _parityList;

    private string _receiveCharContent;

    // 接收数量
    private ulong _receiveCount;

    // 接受数据的十六进制显示
    private string _receiveHexContent;

    // 接收区停止接收
    private bool _reviceAreaStopRevice;

    // RTS 状态
    private bool _RTSCheck;

    // 待发送内容
    private string _sendConetnt;

    // 发送数量
    private ulong _sendCount;

    // 定时发送周期
    private string _sendCycleMill = "1000";

    private ObservableCollection<SerialPortItem> _serialPortNumberList;

    // 停止位列表
    private ObservableCollection<string> _stopList;

    public List<byte> ReceiveContentBytes;

    private NewLineMenuModel _newLineNewLineSelectedItem;

    public NewLineMenuModel NewLineSelectedItem
    {
        get => _newLineNewLineSelectedItem;
        set
        {
            if (Equals(value, _newLineNewLineSelectedItem)) return;
            _newLineNewLineSelectedItem = value;
            RaisePropertyChanged();
            SerialPortCommon.SetNewLine(value.Content);
        }
    }


    public ObservableCollection<NewLineMenuModel> NewLineMenuModels { get; set; }

    public MajorViewModel(IEventAggregator eventAggregator, IDialogService dialogService)
    {
        _eventAggregator = eventAggregator;
        _dialogService = dialogService;

        NewLineMenuModels = new ObservableCollection<NewLineMenuModel>();
        SerialPortNumberList = new ObservableCollection<SerialPortItem>();
        BaudRateList = new ObservableCollection<string>();
        DataBitList = new ObservableCollection<string>();
        ParityList = new ObservableCollection<string>();
        StopList = new ObservableCollection<string>();
        ReceiveContentBytes = new List<byte>();
        CharacterEncodings = new ObservableCollection<string>();

        // 定时器初始化
        _sendTimer.AutoReset = true;
        _sendTimer.Interval = int.Parse(SendCycleMill);
        _sendTimer.Elapsed += (sender, args) =>
        {
            try
            {
                if (string.IsNullOrEmpty(SendConetnt)) return;
                // 判断串口是否开启
                if (!SerialPortCommon.IsOpen()) return;

                if (IsHexSend)
                {
                    // 十六进制
                    var bytes = SendConetnt.ConvertHexSendDataToBytes(CurSelectedCharacterEncoding);
                    SendCount += (ulong)bytes.Length;
                    SerialPortCommon.Write(bytes, 0, bytes.Length, IsSendNewLine);
                }
                else
                {
                    // 字符
                    var bytes = SendConetnt.ConvertCharSendDataToBytes(CurSelectedCharacterEncoding);
                    SendCount += (ulong)bytes.Length;
                    SerialPortCommon.Write(bytes, 0, bytes.Length, IsSendNewLine);
                }
            }
            catch
            {
                // ignore
            }
        };

        Init();

        // 检测握手协议改变
        DTROrRTSStateChangeCommand = new DelegateCommand(() =>
        {
            SerialPortCommon.SetHandshake(RTSCheck, DTRCheck);
            if (SerialPortCommon.IsOpen()) SerialPortCommon.ClearReadBuffer();
        });

        // 开启串口命令
        OpenSerialPortCommand = new DelegateCommand(() =>
        {
            _eventAggregator.SerialPortConnecting();
            Task.Factory.StartNew(() =>
            {
                SerialPortCommon.Config(CurSelectedSerialPortItem.Index, CurSelectedBaudRate, CurSelectedParity,
                    CurSelectedDataBit, CurSelectedStop, CurSelectedCharacterEncoding);
                SerialPortCommon.Open();
                // 清除串口缓存
                SerialPortCommon.ClearOutBuffer();
                SerialPortCommon.ClearReadBuffer();
            }).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    if (task.Exception != null)
                    {
                        var leftBracketsIndex = task.Exception.Message.IndexOf('(');
                        var rightBracketsIndex = task.Exception.Message.IndexOf(')');
                        var res = task.Exception.Message.Substring(leftBracketsIndex + 1,
                            rightBracketsIndex - leftBracketsIndex - 1);
                        _eventAggregator.MainSnakerbarMessageSendErrorMsg(res);
                    }

                    _eventAggregator.SerialPortConnectSuccess(false);
                }
                else
                {
                    _eventAggregator.MainSnakerbarMessageSendSuccessMsg("开启串口成功");
                    _eventAggregator.SerialPortConnectSuccess(true);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        });

        SerialPortConfiureUpdateCommand = new DelegateCommand(() =>
        {
            try
            {
                if (_isOpeningUpdateSerial)
                {
                    CurSelectedSerialPortItem = _connctingSerialPort;
                    _isOpeningUpdateSerial = false;
                }

                if (SerialPortCommon.IsOpen())
                {
                    SerialPortCommon.Close();
                    SerialPortCommon.Config(CurSelectedSerialPortItem.Index, CurSelectedBaudRate, CurSelectedParity,
                        CurSelectedDataBit, CurSelectedStop, CurSelectedCharacterEncoding);
                    SerialPortCommon.Open();
                    eventAggregator.MainSnakerbarMessageSendSuccessMsg("参数修改成功");
                }
                else
                {
                    SerialPortCommon.Config(CurSelectedSerialPortItem.Index, CurSelectedBaudRate, CurSelectedParity,
                        CurSelectedDataBit, CurSelectedStop, CurSelectedCharacterEncoding);
                }
            }
            catch (Exception ex)
            {
                eventAggregator.MainSnakerbarMessageSendErrorMsg(ex.Message);
            }
        });

        // 关闭串口命令
        CloseSerialPortCommand = new DelegateCommand(() =>
        {
            SerialPortCommon.Close();
            _eventAggregator.SerialPortConnectClose();
        });

        // 清空数据
        ClearDisplayDataCommand = new DelegateCommand(() =>
        {
            ReceiveCharContent = "";
            ReceiveHexContent = "";
            ReceiveContentBytes.Clear();
        });

        // 发送数据命令
        SendDataCommand = new DelegateCommand(() =>
        {
            try
            {
                if (string.IsNullOrEmpty(SendConetnt))
                {
                    _eventAggregator.MainSnakerbarMessageSendErrorMsg("发送数据不能为空");
                    return;
                }

                if (IsHexSend)
                {
                    // 十六进制
                    var bytes = SendConetnt.ConvertHexSendDataToBytes(CurSelectedCharacterEncoding);
                    SendCount += (ulong)bytes.Length;
                    SerialPortCommon.Write(bytes, 0, bytes.Length, IsSendNewLine);
                }
                else
                {
                    // 字符
                    var bytes = SendConetnt.ConvertCharSendDataToBytes(CurSelectedCharacterEncoding);
                    SendCount += (ulong)bytes.Length;
                    SerialPortCommon.Write(bytes, 0, bytes.Length, IsSendNewLine);
                }
            }
            catch (Exception ex)
            {
                // 发送错误消息
                _eventAggregator.MainSnakerbarMessageSendErrorMsg(ex.Message);

                if (!SerialPortCommon.IsOpen()) _eventAggregator.SerialPortConnectClose();
            }
        });

        // 更新串口列表
        ReferSerialPortList = new DelegateCommand(() =>
        {
            var serialPortList = GetSerialPorts();
            if (SerialPortCommon.IsOpen())
            {
                _eventAggregator.MainSnakerbarMessageSendErrorMsg("请关闭串口后，再刷新");
            }
            else
            {
                if (serialPortList.Count != 0) CurSelectedSerialPortItem = serialPortList[0];

                SerialPortNumberList = serialPortList;
                _eventAggregator.MainSnakerbarMessageSendSuccessMsg("刷新成功");
            }
        });

        // 监听串口状态任务
        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                if (!SerialPortCommon.IsOpen())
                    // 关闭连接
                    _eventAggregator.SerialPortConnectClose();

                Thread.Sleep(500);
            }
        }, TaskCreationOptions.LongRunning);

        SaveMessageToFileCommand = new DelegateCommand(() => { FileUtils.SaveStringToFile(ReceiveCharContent); });

        //ReadDataTask();
    }

    public bool IsSendNewLine
    {
        get => _isSendNewLine;
        set
        {
            if (value == _isSendNewLine) return;
            _isSendNewLine = value;
            RaisePropertyChanged();
        }
    }

    public bool IsTimerSend
    {
        get => _isTimerSend;
        set
        {
            if (value == _isTimerSend) return;
            _isTimerSend = value;
            RaisePropertyChanged();
            // 定时器操作
            _sendTimer.Enabled = value;
        }
    }

    public string SendCycleMill
    {
        get => _sendCycleMill;
        set
        {
            if (value == _sendCycleMill) return;
            _sendCycleMill = value;
            RaisePropertyChanged();
            // 修改定时器周期
            _sendTimer.Interval = int.Parse(value);
        }
    }

    public bool IsHexSend { get; set; }

    public string SendConetnt
    {
        get => _sendConetnt;
        set
        {
            _sendConetnt = value;
            RaisePropertyChanged();
        }
    }

    public bool RTSCheck
    {
        get => _RTSCheck;
        set
        {
            _RTSCheck = value;
            RaisePropertyChanged();
        }
    }

    public bool DTRCheck
    {
        get => _DTRCheck;
        set
        {
            _DTRCheck = value;
            RaisePropertyChanged();
        }
    }

    public DelegateCommand DTROrRTSStateChangeCommand { get; set; }

    public string ReceiveHexContent
    {
        get => _receiveHexContent;
        set
        {
            _receiveHexContent = value;
            RaisePropertyChanged();
        }
    }

    public string ReceiveCharContent
    {
        get => _receiveCharContent;
        set
        {
            _receiveCharContent = value;
            RaisePropertyChanged();
        }
    }

    public string CurSelectedCharacterEncoding
    {
        get => _curSelectedCharacterEncoding;
        set
        {
            _curSelectedCharacterEncoding = value;
            RaisePropertyChanged();
        }
    }

    public string CurSelectedBaudRate
    {
        get => _curSelectedBaudRate;
        set
        {
            _curSelectedBaudRate = value;
            RaisePropertyChanged();
        }
    }

    public SerialPortItem CurSelectedSerialPortItem
    {
        get => _curSelectedSerialPortItem;
        set
        {
            _curSelectedSerialPortItem = value;
            RaisePropertyChanged();
        }
    }

    public string CurSelectedDataBit
    {
        get => _curSelectedDataBit;
        set
        {
            _curSelectedDataBit = value;
            RaisePropertyChanged();
        }
    }

    public string CurSelectedParity
    {
        get => _curSelectedParity;
        set
        {
            _curSelectedParity = value;
            RaisePropertyChanged();
        }
    }

    public string CurSelectedStop
    {
        get => _curSelectedStop;
        set
        {
            _curSelectedStop = value;
            RaisePropertyChanged();
        }
    }

    // 串口号列表
    public ObservableCollection<SerialPortItem> SerialPortNumberList
    {
        get => _serialPortNumberList;
        set
        {
            _serialPortNumberList = value;
            RaisePropertyChanged();
        }
    }

    // 波特率列表
    public ObservableCollection<string> BaudRateList
    {
        get => _baudRateList;
        set
        {
            _baudRateList = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<string> ParityList
    {
        get => _parityList;
        set
        {
            _parityList = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<string> DataBitList
    {
        get => _dataBitList;
        set
        {
            _dataBitList = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<string> StopList
    {
        get => _stopList;
        set
        {
            _stopList = value;
            RaisePropertyChanged();
        }
    }

    public DelegateCommand OpenSerialPortCommand
    {
        get => _openSerialPortCommand;
        set
        {
            _openSerialPortCommand = value;
            RaisePropertyChanged();
        }
    }

    public bool ReviceAreaStopRevice
    {
        get => _reviceAreaStopRevice;
        set
        {
            _reviceAreaStopRevice = value;
            RaisePropertyChanged();
        }
    }

    public ulong SendCount
    {
        get => _sendCount;
        set
        {
            _sendCount = value;
            RaisePropertyChanged();
        }
    }

    public ulong ReceiveCount
    {
        get => _receiveCount;
        set
        {
            _receiveCount = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<string> CharacterEncodings
    {
        get => _characterEncodings;
        set
        {
            _characterEncodings = value;
            RaisePropertyChanged();
        }
    }

    // 关闭串口命令
    public DelegateCommand CloseSerialPortCommand { get; set; }

    // 串口配置参数更新命令
    public DelegateCommand SerialPortConfiureUpdateCommand { get; set; }

    // 清空数据命令
    public DelegateCommand ClearDisplayDataCommand { get; set; }

    // 更新串口列表命令
    public DelegateCommand ReferSerialPortList { get; set; }

    public DelegateCommand SendDataCommand { get; set; }

    // 保存数据命令
    public DelegateCommand SaveMessageToFileCommand { get; set; }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 注册接受数据事件
        SerialPortCommon.RegisterReceviceDataEvent(SerialPortReadEventHandler);
        _isRead = true;
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // 取消注册接受数据事件
        SerialPortCommon.UnRegisterReceiveDataEvent(SerialPortReadEventHandler);
        _isRead = false;
    }

    private bool QuerySerialPortIsExist(string portName)
    {
        var serialList = SerialPort.GetPortNames();
        foreach (var item in serialList)
            if (portName == item)
                return true;

        return false;
    }

    private bool SerialPortQueryIsContainer(ObservableCollection<SerialPortItem> serialPortList)
    {
        return serialPortList.Any(item => item.Index == CurSelectedSerialPortItem.Index);
    }

    private void Init()
    {
        // 初始化串口列表
        SerialPortNumberList.AddRange(GetSerialPorts());
        // 初始化波特率列表
        BaudRateList.AddRange(GetBaudRates());
        // 初始化数据位列表
        DataBitList.AddRange(GetDataBits());
        // 初始化校验位列表
        ParityList.AddRange(GetParitys());
        // 初始化停止位
        StopList.AddRange(GetStops());
        // 字符编码初始化
        CharacterEncodings.AddRange(GetCharacterEncodings());

        // 设置默认波特率
        CurSelectedBaudRate = "115200";
        // 设置默认串口
        if (SerialPortNumberList.Count != 0) CurSelectedSerialPortItem = SerialPortNumberList[0];

        // 设置默认数据位
        CurSelectedDataBit = "8";
        // 设置默认校验位
        CurSelectedParity = "None";
        // 设置默认停止位
        CurSelectedStop = "One";
        // 默认字符编码格式
        CurSelectedCharacterEncoding = "UTF-8";

        // < ComboBoxItem Content = "\r\n (CRLF)" ></ ComboBoxItem >
        // < ComboBoxItem Content = "\r (CR)" ></ ComboBoxItem >
        // < ComboBoxItem Content = "\n (LF)" ></ ComboBoxItem >


        var defaultSelectedItem = new NewLineMenuModel() { Content = "\r\n", DisplayContent = @"\r\n (CRLF)" };
        NewLineMenuModels.Add(defaultSelectedItem);
        NewLineMenuModels.Add(new NewLineMenuModel() { Content = "\r", DisplayContent = @"\r (CR)" });
        NewLineMenuModels.Add(new NewLineMenuModel() { Content = "\n", DisplayContent = @"\n (LF)" });

        NewLineSelectedItem = defaultSelectedItem;
    }

    public static ObservableCollection<SerialPortItem> GetSerialPorts()
    {
        var serialPortList = new ObservableCollection<SerialPortItem>();
        using (var searcher = new ManagementObjectSearcher
                   ("select * from Win32_PnPEntity where Name like '%(COM%'"))
        {
            var hardInfos = searcher.Get();
            foreach (var hardInfo in hardInfos)
            {
                var serialPortItem = new SerialPortItem();
                var deviceName = hardInfo.Properties["Name"].Value.ToString();
                var index = deviceName.IndexOf('(');
                var numIndex = index + 4;
                var numLength = 0;
                while (true)
                {
                    if ('0' <= deviceName[numIndex] && deviceName[numIndex] <= '9')
                    {
                        numLength++;
                        numIndex++;
                        continue;
                    }

                    break;
                }

                if (numLength == 0) continue;

                serialPortItem.Index = deviceName.Substring(index + 1, numLength + 3);
                serialPortItem.Manu = deviceName.Substring(0, index);
                serialPortList.Add(serialPortItem);
            }
        }

        return serialPortList;
    }

    public static ObservableCollection<string> GetBaudRates()
    {
        var baudRates = new ObservableCollection<string>();
        baudRates.Add("300");
        baudRates.Add("600");
        baudRates.Add("1200");
        baudRates.Add("2400");
        baudRates.Add("4800");
        baudRates.Add("9600");
        baudRates.Add("14400");
        baudRates.Add("19200");
        baudRates.Add("38400");
        baudRates.Add("56000");
        baudRates.Add("57600");
        baudRates.Add("115200");
        baudRates.Add("128000");
        baudRates.Add("256000");
        baudRates.Add("460800");
        baudRates.Add("512000");
        baudRates.Add("750000");
        baudRates.Add("921600");
        baudRates.Add("1500000");
        return baudRates;
    }

    public static ObservableCollection<string> GetDataBits()
    {
        var dataBits = new ObservableCollection<string>
        {
            "5",
            "6",
            "7",
            "8"
        };
        return dataBits;
    }

    public static ObservableCollection<string> GetParitys()
    {
        var parityList = new ObservableCollection<string>
        {
            "Even",
            "Mark",
            "None",
            "Odd"
        };
        return parityList;
    }

    public static ObservableCollection<string> GetStops()
    {
        var parityList = new ObservableCollection<string>
        {
            "One",
            "OnePointFive",
            "Two"
        };
        return parityList;
    }

    public static ObservableCollection<string> GetCharacterEncodings()
    {
        // UTF8  ASCII Unicode GBK GB2312
        var characterEncodingList = new ObservableCollection<string>
        {
            "ASCII",
            "UTF-8",
            "GBK",
            "GB2312"
        };
        return characterEncodingList;
    }

    private void ReadDataTask()
    {
        Task.Factory.StartNew(() =>
        {
            while (true)
                try
                {
                    // 串口打开且未暂停接收
                    if (!_isRead || !SerialPortCommon.IsOpen() || !SerialPortCommon.HasData()) continue;

                    if (ReviceAreaStopRevice)
                    {
                        SerialPortCommon.ClearReadBuffer();
                        continue;
                    }

                    var readStr = SerialPortCommon.ReadLine();
                    if (readStr == null) continue;

                    ReceiveCharContent += readStr;
                    var bytes = Encoding.GetEncoding(CurSelectedCharacterEncoding).GetBytes(readStr);
                    ReceiveCount += (ulong)bytes.Length;

                    ReceiveHexContent += BitConverter.ToString(bytes).Replace("-", " ");

                    // 对接收内容大小做出限制 防止内容太多性能问题
                    if (ReceiveCharContent.Length > 1000)
                    {
                        // 截取后1000个数据
                        ReceiveCharContent = ReceiveCharContent.Substring(300);

                        // 更新十六进制显示
                        ReceiveHexContent = BitConverter
                            .ToString(ReceiveCharContent.ConvertCharSendDataToBytes(
                                CurSelectedCharacterEncoding)).Replace("-", " ");
                    }
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
        }, TaskCreationOptions.LongRunning);
    }

    // 串口读取数据函数
    private void SerialPortReadEventHandler(object sender, SerialDataReceivedEventArgs e)
    {
        Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // 串口打开且未暂停接收
                    if (!SerialPortCommon.IsOpen()) return;

                    if (ReviceAreaStopRevice)
                    {
                        SerialPortCommon.ClearReadBuffer();
                        return;
                    }

                    var readStr = SerialPortCommon.ReadLine();

                    ReceiveCharContent += readStr;
                    var bytes = Encoding.GetEncoding(CurSelectedCharacterEncoding).GetBytes(readStr);
                    ReceiveCount += (ulong)bytes.Length;

                    ReceiveHexContent += BitConverter.ToString(bytes).Replace("-", " ");

                    // 对接收内容大小做出限制 防止内容太多性能问题
                    if (ReceiveCharContent.Length > 1000)
                    {
                        // 截取后1000个数据
                        ReceiveCharContent = ReceiveCharContent.Substring(300);

                        // 更新十六进制显示
                        ReceiveHexContent = BitConverter
                            .ToString(ReceiveCharContent.ConvertCharSendDataToBytes(
                                CurSelectedCharacterEncoding)).Replace("-", " ");
                    }
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
                catch
                {
                }
            }
        });
    }
}