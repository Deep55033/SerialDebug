using ImTools;
using System.Collections.Generic;
using System.Text;

namespace SerialDebug.Extension
{
    public static class StringExtension
    {
        public static byte[] ConvertHexSendDataToBytes(this string str, string encodingFormat)
        {
            string[] splitResult = str.Split(" ");
            foreach (string item in splitResult)
            {
                if (string.IsNullOrWhiteSpace(item) || string.IsNullOrEmpty(item))
                {
                    splitResult = splitResult.Remove(item);
                }
            };
            byte[] bytes = new byte[splitResult.Length];
            for (int i = 0; i < splitResult.Length; i++)
            {
                bytes[i] = byte.Parse(splitResult[i], System.Globalization.NumberStyles.HexNumber);
            }
            return bytes;
        }

        public static byte[] ConvertCharSendDataToBytes(this string str, string encodingFormat)
        {
            return Encoding.GetEncoding(encodingFormat).GetBytes(str);
        }
    }
}