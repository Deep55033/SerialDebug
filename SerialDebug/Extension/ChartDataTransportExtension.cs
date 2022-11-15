using Prism.Events;
using SerialDebug.Event;
using SerialDebug.Event.Model;
using System;

namespace SerialDebug.Extension
{
    public static class ChartDataTransportExtension
    {
        // 注册消息
        public static void ChartDataTransportEventRegister(this IEventAggregator eventAggregator, Action<ChartDataTransportModel> action)
        {
            eventAggregator.GetEvent<ChartDataTransportEvent>().Subscribe(action);
        }

        // 发送消息
        public static void ChartDataTransportEventSend(this IEventAggregator eventAggregator, ChartDataTransportModel chartDataTransportModel)
        {
            eventAggregator.GetEvent<ChartDataTransportEvent>().Publish(chartDataTransportModel);
        }
    }
}