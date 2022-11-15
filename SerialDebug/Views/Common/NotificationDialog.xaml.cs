using System.Windows.Controls;

namespace SerialDebug.Views.Common
{
    /// <summary>
    /// NotificationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class NotificationDialog : UserControl
    {
        public NotificationDialog(string content = "正在连接...")
        {
            InitializeComponent();
            displayContent.Text = content;
        }
    }
}