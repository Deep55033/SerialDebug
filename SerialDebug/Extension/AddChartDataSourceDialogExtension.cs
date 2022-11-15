using Prism.Events;
using SerialDebug.Event;
using SerialDebug.Event.Model;
using System;

namespace SerialDebug.Extension
{
    public static class AddChartDataSourceDialogExtension
    {
        public static void AddChartDataSourceDialogEventRegister(this IEventAggregator eventAggregator, Action<AddChartDataSourceDialogModel> action)
        {
            eventAggregator.GetEvent<AddChartDataSourceDialogEvent>().Subscribe(action);
        }

        public static void AddChartDataSourceDialogEventClose(this IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<AddChartDataSourceDialogEvent>().Publish(new AddChartDataSourceDialogModel { isOpen = false });
        }

        public static void AddChartDataSourceDialogEventOpen(this IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<AddChartDataSourceDialogEvent>().Publish(new AddChartDataSourceDialogModel { isOpen = true });
        }
    }
}