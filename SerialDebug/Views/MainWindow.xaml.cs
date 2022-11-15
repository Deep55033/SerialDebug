using MaterialDesignThemes.Wpf;
using Prism.Events;
using SerialDebug.Extension;
using SerialDebug.Service;
using SerialDebug.Views.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SerialDebug.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IConfigureService
    {
        private readonly IEventAggregator eventAggregator;

        public MainWindow(IEventAggregator _eventAggregator)
        {
            InitializeComponent();
            ColorZone.MouseDoubleClick += ColorZone_MouseDoubleClick;
            ColorZone.MouseMove += ColorZone_MouseMove;

            WindowMaxButton.Click += (sender, args) =>
            {
                if (WindowState.Maximized == WindowState)
                {
                    WindowState = WindowState.Normal;
                }
                else
                {
                    WindowState = WindowState.Maximized;
                }
            };

            WindowMinButton.Click += (sender, args) => { WindowState = WindowState.Minimized; };

            WindowCloseButton.Click += (sender, args) => { Close(); };

            Task.Factory.StartNew(() => Thread.Sleep(2000)).ContinueWith(t =>
            {
                MainSnackBar.MessageQueue?.Enqueue("欢迎使用串口调试助手", null, null, null, false, true, TimeSpan.FromMilliseconds(1000));
            }, TaskScheduler.FromCurrentSynchronizationContext());
            this.eventAggregator = _eventAggregator;

            _eventAggregator.SerrialConnectRegister((_serialConnectModel =>
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    if (_serialConnectModel == null) return;
                    if (_serialConnectModel.IsOpen)
                    {
                        dialogHost.IsOpen = true;
                        if (dialogHost.IsOpen)
                        {
                            dialogHost.DialogContent = new NotificationDialog();
                        }
                    }
                    else
                    {
                        dialogHost.IsOpen = false;
                    }
                }));
            }));

            _eventAggregator.MainSnackerbarMessageRegister(model =>
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    MainSnackBar.MessageQueue?.Enqueue(model.Msg, null, null, null, false, true, TimeSpan.FromMilliseconds(800));
                    MainSnackBar.Background = model.Background;
                }));
            });
        }

        private void ColorZone_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ColorZone_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        public void configure()
        {
        }
    }
}