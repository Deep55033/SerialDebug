using Prism.Events;
using SerialDebug.Extension;
using SerialDebug.ViewModels;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SerialDebug.Views
{
    /// <summary>
    /// MajorView.xaml 的交互逻辑
    /// </summary>
    public partial class MajorView : UserControl
    {
        private int _lastInputCount;

        public MajorView(IEventAggregator _eventAggregator)
        {
            InitializeComponent();
            _eventAggregator.SerrialConnectRegister((model) =>
            {
                if (model.IsClose)
                {
                    this.Dispatcher.Invoke(new Action(delegate
                    {
                        connectButton.Content = "打开串口";
                        connectButton.Style = (Style)App.Current.Resources["MaterialDesignRaisedButton"];
                        connectButton.Command = ((MajorViewModel)connectButton.DataContext).OpenSerialPortCommand;
                    }));
                }
                else
                {
                    this.Dispatcher.Invoke(new Action(delegate
                    {
                        if (model.IsOpen) return;
                        if (!model.IsSuccess) return;
                        connectButton.Content = "关闭串口";
                        connectButton.Style = (Style)Application.Current.Resources["MaterialDesignRaisedAccentButton"];
                        connectButton.Command = ((MajorViewModel)connectButton.DataContext).CloseSerialPortCommand;
                    }));
                }
            });

            hexDisplayCheckBox.Click += HexDisplayCheckBox_Click;
            displayReceiveContent.TextChanged += DisplayReceiveContent_TextChanged;

            var contentBinding = new Binding("ReceiveCharContent");
            displayReceiveContent.SetBinding(TextBox.TextProperty, contentBinding);

            inputSendContent.TextChanged += InputSendContent_TextChanged;

            hexSendCheckBox.Click += HexSendCheckBox_Click;
        }

        private void HexSendCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (hexSendCheckBox.IsChecked == true)
            {
                var bytes = inputSendContent.Text.ConvertCharSendDataToBytes((string)charecterEncodingComoBox
                    .SelectedItem);
                ((MajorViewModel)DataContext).SendConetnt = BitConverter.ToString(bytes).Replace("-", " ");
                inputSendContent.Select(inputSendContent.Text.Length, 0);
            }
            else
            {
                var bytes = inputSendContent.Text.ConvertHexSendDataToBytes((string)charecterEncodingComoBox
                    .SelectedItem);
                var str = Encoding.GetEncoding((string)charecterEncodingComoBox.SelectedItem).GetString(bytes);
                ((MajorViewModel)DataContext).SendConetnt = str;
                inputSendContent.Select(inputSendContent.Text.Length, 0);
            }
        }

        private void InputSendContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 删除操作
            if (_lastInputCount > inputSendContent.Text.Length)
            {
                _lastInputCount = inputSendContent.Text.Length;
                return;
            }

            if (hexSendCheckBox.IsChecked == null || !(bool)hexSendCheckBox.IsChecked) return;

            var regex = new Regex("[^0-9A-Fa-f ]");
            var result = regex.Replace(inputSendContent.Text, "");
            if (result.Length >= 2 && result[^1] != ' ' && result[^2] != ' ')
            {
                result += " ";
            }

            ((MajorViewModel)DataContext).SendConetnt = result;
            // 将光标放在最后
            inputSendContent.Select(inputSendContent.Text.Length, 0);
            _lastInputCount = inputSendContent.Text.Length;
        }

        private void HexDisplayCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (hexDisplayCheckBox.IsChecked != null && (bool)hexDisplayCheckBox.IsChecked) // 选中
            {
                Binding contentBinding = new Binding("ReceiveHexContent");
                displayReceiveContent.SetBinding(TextBox.TextProperty, contentBinding);
            }
            else
            {
                Binding contentBinding = new Binding("ReceiveCharContent");
                displayReceiveContent.SetBinding(TextBox.TextProperty, contentBinding);
            }
        }

        private void DisplayReceiveContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            displayReceiveContent.ScrollToEnd();
        }
    }
}