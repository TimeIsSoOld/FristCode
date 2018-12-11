using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SUNC_Main_DoctorProcess
{

    public class HttpWebResponseHelper
    {
        private Tool.INIClass iniClass = null;
        private string baseAddress;
        public HttpWebResponseHelper()
        {
            string filename = System.IO.Directory.GetCurrentDirectory() + "\\INIConfig\\OptionsSave.ini";
            iniClass = new Tool.INIClass(filename);
            baseAddress = iniClass.IniReadValue("ApiUrl", "BaseAddress");
        }
        public HttpWebResponse HttpPost(string url,string body)
        {
            System.GC.Collect();
            Encoding encoding = Encoding.UTF8;
            string urlllllll = baseAddress + "/" + url;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlllllll);

            request.ProtocolVersion = HttpVersion.Version11;
            request.Method = "POST";
            request.KeepAlive = false;
            request.Timeout = 1200000;
            request.ContentType = "application/json";
            //request.ContentType = "application/x-www-form-urlencoded";

            byte[] buffer = encoding.GetBytes(body);
            request.ContentLength = buffer.Length;

            using (Stream writer = request.GetRequestStream())
            {
                writer.Write(buffer, 0, buffer.Length);
                writer.Close();
            }
            HttpWebResponse response;
            try
            {
                try
                {
                    ServicePointManager.DefaultConnectionLimit = 200;
                    //response = (HttpWebResponse)request.GetResponse();
                    Task<WebResponse> task = request.GetResponseAsync();
                    while (!task.IsCompleted)
                    {
                        System.Threading.Thread.Sleep(20);
                    }
                    response = (HttpWebResponse)task.Result;
                }
                catch (WebException ex)
                {
                    response = (HttpWebResponse)ex.Response;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }                    

            WebHeaderCollection c = response.Headers;
            string[] carrary = c.GetValues("Content-Disposition");
            string[] carraryl = c.GetValues("Content-Length");

            return response;
        }
    }
}
