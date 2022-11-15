using System;
using System.Windows.Media;
using MaterialDesignColors;
using Prism.Events;
using SerialDebug.Event;
using SerialDebug.Event.Model;

namespace SerialDebug.Extension;

public static class MainSnackbarExtension
{
    public static void MainSnackerSendMessgaeSendRow(this IEventAggregator eventAggregator, string msg,
        Brush background)
    {
        eventAggregator.GetEvent<MainSnackbarEvent>()
            .Publish(new MainSnackbarModel { Msg = msg, Background = background });
    }

    public static void MainSnackerbarMessageRegister(this IEventAggregator eventAggregator,
        Action<MainSnackbarModel> action)
    {
        eventAggregator.GetEvent<MainSnackbarEvent>().Subscribe(action);
    }

    // 发送错误消息
    public static void MainSnakerbarMessageSendErrorMsg(this IEventAggregator eventAggregator, string msg)
    {
        var brushCon = new BrushConverter();
        eventAggregator.MainSnackerSendMessgaeSendRow(msg,
            (Brush)brushCon.ConvertFromString(PrimaryColor.Red.ToString()));
    }

    // 发送提示消息
    public static void MainSnakerbarMessageSendInfo(this IEventAggregator eventAggregator, string msg)
    {
        eventAggregator.MainSnackerSendMessgaeSendRow(msg, null);
    }

    // 发送成功消息
    public static void MainSnakerbarMessageSendSuccessMsg(this IEventAggregator eventAggregator, string msg)
    {
        var brushCon = new BrushConverter();
        eventAggregator.MainSnackerSendMessgaeSendRow(msg,
            (Brush)brushCon.ConvertFromString(PrimaryColor.Green.ToString()));
    }
}