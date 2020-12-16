using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ServicesCeltaWare.UtilitariosInfra.GoogleApiStandard
{
    public class HelperLogs
    {
        public async static void WriteLog(string _msg)
        {
            var stream = new System.IO.FileStream("ServicesCeltaWareLog.txt", FileMode.Create);
            StreamWriter sw = null;
            sw = new StreamWriter(stream);
            sw.WriteLine(DateTime.Now.ToString() + ": Services CeltaWare - " + _msg);
            sw.Flush();
            //sw.Close();
        }
    }
}
