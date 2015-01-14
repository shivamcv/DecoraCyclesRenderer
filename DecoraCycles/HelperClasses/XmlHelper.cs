using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace DecoraCsycles.HelperClasses
{
    public class XmlHelper
    {
        public static T readXml<T>(string fileName)
        {
            return Retry.Do<T>(delegate()
            {
                String tempFile = App.TempStorageLocation + "\\" + Guid.NewGuid() + ".data";


                Encryption en = new Encryption();
                en.DecryptFile(fileName, tempFile);

                T tempList;

                XmlSerializer deserializer = new XmlSerializer(typeof(T));
                TextReader textReader = new StreamReader(tempFile);
                tempList = (T)deserializer.Deserialize(textReader);
                textReader.Close();

                File.Delete(tempFile);

                return tempList;
            }, TimeSpan.FromMilliseconds(1000), 5);

        }

        public static void writeXml<T>(T tempList, string fileName)
        {
            try
            {
                File.Delete(fileName);

                String tempFile = App.TempStorageLocation + "\\" + Guid.NewGuid() + ".data";
                //TODO
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                TextWriter textWriter = new StreamWriter(tempFile);
                serializer.Serialize(textWriter, tempList);
                textWriter.Close();

                Encryption en = new Encryption();

                en.EncryptFile(tempFile, fileName);
            }
            catch (Exception ex)
            {
                DispatcherTimer tmr = new DispatcherTimer(TimeSpan.FromMilliseconds(1), DispatcherPriority.Normal, (addsd, dd) => { writeXml(tempList, fileName); ((DispatcherTimer)addsd).Stop(); }, DispatcherHelper.UIDispatcher);
                tmr.Start();

                Debug.WriteLine(ex.Message);
            }
        }
      
    }
}
