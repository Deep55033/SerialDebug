using Prism.Events;
using SerialDebug.Extension;
using System.Windows;
using System.Windows.Controls;

namespace SerialDebug.Views
{
    /// <summary>
    /// ChartView.xaml 的交互逻辑
    /// </summary>
    public partial class ChartView : UserControl
    {
        private readonly IEventAggregator _eventAggregator;

        public ChartView(IEventAggregator eventAggregator)
        {
            InitializeComponent();

            addChartDataSourceButton.Click += AddChartDataSourceButton_Click;
            this._eventAggregator = eventAggregator;

            _eventAggregator.AddChartDataSourceDialogEventRegister((model) => { dialogHost.IsOpen = model.isOpen; });

            bothZoomRadio.Click += ZoomRadio_Click;
            XZoomRadio.Click += ZoomRadio_Click;
            YZoomRadio.Click += ZoomRadio_Click;
        }

        private void ZoomRadio_Click(object sender, RoutedEventArgs e)
        {
            if (bothZoomRadio.IsChecked == true)
            {
                chart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.Both;
            }
            else if (XZoomRadio.IsChecked == true)
            {
                chart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X;
            }
            else if (YZoomRadio.IsChecked == true)
            {
                chart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.Y;
            }
        }

        private void AddChartDataSourceButton_Click(object sender, RoutedEventArgs e)
        {
            dialogHost.IsOpen = true;
        }
    }
}