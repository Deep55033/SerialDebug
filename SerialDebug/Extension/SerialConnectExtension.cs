using Prism.Events;
using SerialDebug.Event;
using SerialDebug.Event.Model;
using System;

namespace SerialDebug.Extension
{
    public static class SerialConnectExtension
    {
        public static void SerialPortConnecting(this IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<SerialConnectEvent>().Publish(new SerialConnectModel { IsOpen = true, IsClose = false });
        }

        public static void SerialPortConnectSuccess(this IEventAggregator eventAggregator, bool isSuccess)
        {
            eventAggregator.GetEvent<SerialConnectEvent>().Publish(new SerialConnectModel { IsOpen = false, IsSuccess = isSuccess, IsClose = false });
        }

        public static void SerrialConnectRegister(this IEventAggregator eventAggregator, Action<SerialConnectModel> action)
        {
            eventAggregator.GetEvent<SerialConnectEvent>().Subscribe(action);
        }

        public static void SerialPortConnectClose(this IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<SerialConnectEvent>().Publish(new SerialConnectModel { IsOpen = false, IsSuccess = false, IsClose = true });
        }
    }
}