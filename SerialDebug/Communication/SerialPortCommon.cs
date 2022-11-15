using System;
using System.IO;
using System.IO.Ports;
using System.Text;

namespace SerialDebug.Communication
{
    public static class SerialPortCommon
    {
        private static SerialPort _serialPort = new();

        // 设置编码协议
        public static void SetCharacterEncoding(string code)
        {
            _serialPort.Encoding = Encoding.GetEncoding(code);
        }

        // 设置握手协议
        public static void SetHandshake(bool RTS, bool DTR)
        {
            if (RTS && DTR)
            {
                _serialPort.Handshake = Handshake.RequestToSendXOnXOff;
            }
            else if (RTS)
            {
                _serialPort.Handshake = Handshake.RequestToSend;
            }
            else if (DTR)
            {
                _serialPort.Handshake |= Handshake.XOnXOff;
            }
            else
            {
                _serialPort.Handshake |= Handshake.None;
            }
        }

        // 设置停止位
        public static void SetStopBit(string stopbitStr)
        {
            var stopBits = stopbitStr switch
            {
                "One" => StopBits.One,
                "OnePointFive" => StopBits.OnePointFive,
                "Two" => StopBits.Two,
                _ => StopBits.One,
            };
            _serialPort.StopBits = stopBits;
        }

        // 设置数据位
        public static void SetDataBit(string databitStr)
        {
            try
            {
                _serialPort.DataBits = int.Parse(databitStr);
            }
            catch (Exception)
            {
                throw new Exception("数据位格式错误");
            }
        }

        // 设置奇偶校验位
        public static void SetParity(string parityStr)
        {
            var parity = parityStr switch
            {
                "Even" => Parity.Even,
                "Mark" => Parity.Mark,
                "None" => Parity.None,
                "Odd" => Parity.Odd,
                _ => Parity.None,
            };
            _serialPort.Parity = parity;
        }

        // 设置串口名
        public static void SetSerialPortName(string name)
        {
            _serialPort.PortName = name;
        }

        // 设置波特率
        public static void SetBaudRate(string baud)
        {
            try
            {
                _serialPort.BaudRate = int.Parse(baud);
            }
            catch (Exception)
            {
                throw new Exception("波特率格式错误");
            }
        }

        public static void Config(string portName, string baudRate, string parity, string dataBits, string stopBits, string encoding, bool RTS = false, bool DTR = false)
        {
            SetSerialPortName(portName);
            SetBaudRate(baudRate);
            SetParity(parity);
            SetDataBit(dataBits);
            SetStopBit(stopBits);
            SetHandshake(RTS, DTR);

            _serialPort.NewLine = "\r\n";

            // 设置超时时间
            _serialPort.ReadTimeout = 100;
            _serialPort.WriteTimeout = 1000;

            // 设置编码格式
            SetCharacterEncoding(encoding);
        }

        // 打开串口
        public static void Open()
        {
            try
            {
                _serialPort.Open();
            }
            catch (UnauthorizedAccessException)
            {
                // 对串口的访问被拒绝
                throw new Exception("对串口访问被拒绝");
            }
            catch (ArgumentOutOfRangeException)
            {
                // 串口参数错误
                throw new Exception("串口传入参数错误");
            }
            catch (ArgumentException)
            {
                // 串口名错误或不支持
                throw new Exception("串口名无效或不支持");
            }
            catch (IOException)
            {
                // 串口无效
                throw new Exception("该串口无效");
            }
            catch (InvalidOperationException)
            {
                // 当前串口已打开
                throw new Exception("该串口已被打开");
            }
        }

        // 关闭串口
        public static void Close()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        public static string ReadExisting()
        {
            try
            {
                return _serialPort.ReadExisting();
            }
            catch (InvalidOperationException)
            {
                throw new Exception("串口已关闭");
            }
        }

        public static bool IsOpen()
        {
            return _serialPort.IsOpen;
        }

        public static void Write(byte[] buf, int offset, int length, bool isSendLine)
        {
            try
            {
                _serialPort.Write(buf, offset, length);
                if (!isSendLine) return;
                var newLineBytes = _serialPort.Encoding.GetBytes(_serialPort.NewLine);
                _serialPort.Write(newLineBytes, 0, newLineBytes.Length);
            }
            catch (InvalidOperationException)
            {
                throw new Exception("串口没有打开");
            }
            catch (TimeoutException)
            {
                throw new Exception("写入超时");
            }
        }

        // 注册错误事件
        public static void RegisterErrorEvent(SerialErrorReceivedEventHandler eventHandler)
        {
            _serialPort.ErrorReceived += eventHandler;
        }

        // 注册接收数据事件
        public static void RegisterReceviceDataEvent(SerialDataReceivedEventHandler eventHandler)
        {
            _serialPort.DataReceived += eventHandler;
        }

        // 取消注册
        public static void UnRegisterReceiveDataEvent(SerialDataReceivedEventHandler eventHandler)
        {
            _serialPort.DataReceived -= eventHandler;
        }

        public static void ClearReadBuffer()
        {
            _serialPort.DiscardInBuffer();
        }

        public static void ClearOutBuffer()
        {
            _serialPort.DiscardOutBuffer();
        }

        public static int ReadByte()
        {
            return _serialPort.ReadByte();
        }

        public static int Read(byte[] buf, int offset, int length)
        {
            return _serialPort.Read(buf, offset, length);
        }

        public static int ReadBufferSize => _serialPort.ReadBufferSize;

        public static string GetCurConnectName => _serialPort.PortName;

        private static byte[] readBuffer = new byte[ReadBufferSize];

        public static string ReadLine()
        {
            try
            {
                return _serialPort.ReadLine() + _serialPort.NewLine;
            }
            catch (TimeoutException)
            {
                var readLength = Read(readBuffer, 0, readBuffer.Length);
                return _serialPort.Encoding.GetString(readBuffer, 0, readLength);
            }
            catch (Exception)
            {
                // 其他错误
                return null;
            }
        }

        public static void SetNewLine(string newLine)
        {
            _serialPort.NewLine = newLine;
        }

        public static bool HasData() => _serialPort.BytesToRead != 0;
    }
}