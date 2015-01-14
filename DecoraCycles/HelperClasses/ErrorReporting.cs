using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DecoraCsycles.HelperClasses
{
    public static class ErrorReporting
    {

        public static void reportError(Exception e, string message = null)
        {
            try
            {
                string logFile = App.StorageLocation + "\\errorlog.xml";
                List<Error> tempErrors;
                if (File.Exists(logFile))
                {
                    tempErrors = XmlHelper.readXml<List<Error>>(logFile);
                }
                else
                {
                    tempErrors = new List<Error>();
                }

                if (e == null)
                    e = new Exception();

                tempErrors.Add(new Error()
                {
                    errorTimeStamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                    msg = message == null ? e.Message : message,
                    source = e.Source,
                    stackTrace = e.StackTrace
                });

                Debug.WriteLine("cv=> " + (message ?? "") + ", " + e.Message);
                XmlHelper.writeXml(tempErrors, logFile);
            }
            catch
            {

            }
        }


        public static void uploadErrorLog(string LicenseKey, string Version)
        {
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += (snder, ee) =>
            {
                try
                {
                    string logFile = App.StorageLocation + "\\errorlog.xml";

                    if (!File.Exists(logFile))
                        return;

                    List<Error> Errors = XmlHelper.readXml<List<Error>>(logFile);

                    if (Errors.Count < 10)
                        sendError(Errors, LicenseKey, Version);
                    else
                    {
                        int ctr = 0;
                        do
                        {
                            sendError(Errors.Skip(ctr).Take(10).ToList(), LicenseKey, Version);
                            ctr += 10;

                        } while (ctr < Errors.Count);
                    }
                    File.Delete(logFile);
                }
                catch
                {

                }

            };

            bgw.RunWorkerAsync();
        }

        private static void sendError(List<Error> Errors, string LicenseKey, string Version)
        {
            string environment = "";
            AddEnvironmentInfo(ref environment);



            ServiceError srvError = new ServiceError()
            {
                exceptionID = Guid.NewGuid().ToString(),
                edition = "Standard",
                Error = Errors,
                environement = environment,
                licenceKey = LicenseKey,
                version = Version,
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(srvError);

            json = Encryption.EncryptStringToBytes(json);
            tempClass str = new tempClass() { strData = json };
            json = Newtonsoft.Json.JsonConvert.SerializeObject(str);


            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] data = encoding.GetBytes(json);

            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.ContentType] = "application/json";


            //var response1 = wc.UploadData("http://demo.simformsolutions.com:55200/WebService/RegistrationServices.svc/StoreException", data);

            var response = wc.UploadData("http://app.decorastudio.com/WebService/RegistrationServices.svc/StoreException", data);
            //var response = wc.UploadData("http://localhost:61200/WebService/RegistrationServices.svc/StoreException", data);
            //var response = wc.UploadData("http://jaguar:4500/WebService/RegistrationServices.svc/StoreException", data);

        }



        private static void AddEnvironmentInfo(ref string str)
        {
            str += "\n *************************EnvironMent****************************************\n\n";

            str += "\n\n\n=============> Operating System\n";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");

            foreach (ManagementObject mo in searcher.Get())
            {
                foreach (PropertyData property in mo.Properties)
                {
                    str += "\t<b>" + property.Name + "</b> : ";
                    str += property.Value;
                }
            }

            str += "\n\n\n======================> Video Controller\n";

            searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            foreach (ManagementObject mo in searcher.Get())
            {
                foreach (PropertyData property in mo.Properties)
                {
                    str += "\t<b>" + property.Name + "</b>: ";
                    str += property.Value;

                }
            }

        }
    }

    public class Error
    {
        public string errorTimeStamp { get; set; }
        public string stackTrace { get; set; }
        public string source { get; set; }
        public string msg { get; set; }


        public Error()
        {

        }
    }

    public class tempClass
    {
        public string strData { get; set; }
        public tempClass()
        { }
    }

    public class ServiceError
    {
        public string exceptionID { get; set; }
        public string licenceKey { get; set; }
        public string edition { get; set; }
        public string version { get; set; }
        public List<Error> Error { get; set; }
        public string environement { get; set; }
    }
}
