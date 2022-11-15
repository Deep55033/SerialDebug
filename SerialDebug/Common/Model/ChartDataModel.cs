using LiveChartsCore.Defaults;
using Prism.Mvvm;
using SerialDebug.Extension;

namespace SerialDebug.Common.Model
{
    public class ChartDataModel : BindableBase
    {
        private string _name;

        public string Name
        {
            get => _name; set
            {
                _name = value; RaisePropertyChanged();
            }
        }

        // 线条颜色
        private string _color;

        public string Color
        {
            get { return _color; }
            set { _color = value; RaisePropertyChanged(); }
        }

        // 线条粗细
        private int _thickness;

        public int Thickness
        {
            get { return _thickness; }
            set { _thickness = value; RaisePropertyChanged(); }
        }

        public SynchronizedObservableCollection<ObservableValue> DataSourece { get; set; }
    }
}