using Microsoft.Win32;
using System.IO;

namespace SerialDebug.Utils
{
    public static class FileUtils
    {
        public static void SaveStringToFile(string msg)
        {
            var fileDialog = new SaveFileDialog();
            fileDialog.Filter = "TXT FILE(*.txt)|*.txt";
            var res = fileDialog.ShowDialog();
            if (res == true)
            {
                var fileStr = new FileStream(fileDialog.FileName, FileMode.Create);
                StreamWriter streamWriter = new StreamWriter(fileStr);
                streamWriter.Write(msg);
                streamWriter.Flush();
                streamWriter.Close();
            }
        }
    }
}