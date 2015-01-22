using ccl;
using DecoraCsycles.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DecoraCsycles.ViewModel
{
    public class HomeViewModel :ViewModelBase
    {
        enum updateType
        {
            logger, writeTile, updateTile, status
        }

        private string status;
        public string Status
        {
            get { return status; }
            set { status = value;
            RaisePropertyChanged("Status");
            }
        }

        private string logger;
        public string Logger
        {
            get { return logger; }
            set
            {
                logger = value;
                RaisePropertyChanged("Logger");
            }
        }
        private string writeTile;
        public string WriteTile
        {
            get { return writeTile; }
            set
            {
                writeTile = value;
                RaisePropertyChanged("WriteTile");
            }
        }
        private string updateTile;
        public string UpdateTile
        {
            get { return updateTile; }
            set
            {
                updateTile = value;
                RaisePropertyChanged("UpdateTile");
            }
        }

        private ccl.Transform stringToTransform(string p)
        {
            try
            {
                p = p.Replace("[", " ");
                p = p.Replace("]", " ");

                float[] temp = p.Split(',').Select(q => float.Parse(q)).ToArray();
                return ccl.Transform.Transpose(new ccl.Transform(temp));
            }catch
            {
                return new ccl.Transform();
            }
        }
       

         public HomeViewModel()
        {
           // cameraView = SimpleIoc.Default.GetInstance<ConnectionViewModel>().SunburnScene.View.ToString();
            var width = (uint)Cycles.scene.Camera.Size.Width;
            var height = (uint)Cycles.scene.Camera.Size.Height;

            writeableBitmap = new WriteableBitmap((int)width, (int)height, 96, 96, PixelFormats.Bgr32, null);
            
            Write("Connected", updateType.status);
            Task.Factory.StartNew(() =>
                {
                    var Con = SimpleIoc.Default.GetInstance<ConnectionViewModel>();
                     Con.Renderer.Render(WriteRenderTileCallback, UpdateRenderTileCallback, StatusUpdateCallback, LoggerCallback);
                });

            //Task.Factory.StartNew(LoadCameraView);
            //Task.Factory.StartNew(SimpleIoc.Default.GetInstance<ConnectionViewModel>().FetchData);

        }

         private void LoadCameraView()
         {
             using (var namedPipeClientStream = new NamedPipeClientStream(".", ConnectionViewModel.PIPENAME + "\\Camera", PipeDirection.InOut))
             {
                 namedPipeClientStream.Connect();
                 namedPipeClientStream.ReadMode = PipeTransmissionMode.Message;

                 int byteCount = 0;
                 byte[] msgBuff = new byte[1024];
                 StringBuilder mb = new StringBuilder();
                 do
                 {
                     byteCount = namedPipeClientStream.Read(msgBuff, 0, 1024);
                     mb.Append(System.Text.Encoding.UTF8.GetString(msgBuff, 0, msgBuff.Length));
                 } while (!(namedPipeClientStream.IsMessageComplete));

                 string viewString = mb.ToString();

                 float[] temp = viewString.Split(',').Select(q => float.Parse(q)).ToArray();

                 var view = new ccl.Transform(temp);

                 var Con = SimpleIoc.Default.GetInstance<ConnectionViewModel>();

                 CameraView = view.ToString();
                 Cycles.Session.Cancel("Asdf");
                 Cycles.scene.Camera.Matrix = view;

                 Cycles.Session.Reset((uint)Cycles.scene.Camera.Size.Width, (uint)Cycles.scene.Camera.Size.Height, (uint)Cycles.samples);
               //  Cycles.scene.Reset();

                 Cycles.Session.Start();
             }

             LoadCameraView();
         }
        
        static string GetString(byte[] bytes)
         {
             char[] chars = new char[bytes.Length / sizeof(char)];
             System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
             return new string(chars);
         }

         public void StatusUpdateCallback(uint sessionId)
         {
             float progress;
             double total_time,render_time,tile_Time;

             CSycles.progress_get_progress(Cycles.client.Id, sessionId, out progress, out total_time, out render_time,out tile_Time);
             var status = CSycles.progress_get_status(Cycles.client.Id, sessionId);
             var substatus = CSycles.progress_get_substatus(Cycles.client.Id, sessionId);
             uint samples;
             uint num_samples;

             CSycles.tilemanager_get_sample_info(Cycles.client.Id, sessionId, out samples, out num_samples);

             if (status.Equals("Finished"))
             {
                 Console.WriteLine("wohoo... :D");
             }
             
                  status = "[" + status + "]";
                 if (!substatus.Equals(string.Empty)) status = status + ": " + substatus;
                 Write(string.Format("C# status update: {0} {1} {2} {3} <|> {4:N}s {5:P}", CSycles.progress_get_sample(Cycles.client.Id, sessionId), status, samples, num_samples, total_time, progress), updateType.status);
         }

         public WriteableBitmap writeableBitmap { get; set; }

         private void UpdateBitmap(uint x, uint y, uint w, uint h)
         {
                 var width = (uint)Cycles.scene.Camera.Size.Width;
                 var height = (uint)Cycles.scene.Camera.Size.Height;

                 uint bufsize;
                 uint bufstride;

                 CSycles.session_get_buffer_info(Cycles.client.Id, Cycles.Session.Id, out bufsize, out bufstride);

                 var pixels = CSycles.session_copy_buffer(Cycles.client.Id, Cycles.Session.Id, bufsize);

                 writeableBitmap.Lock();
                 
                int[] Data = new int[w*h];
                int ctr = 0;
                 unsafe
                 {
                     for (var row = x; row < x + w; row++)
                     {
                         for (var col = y; col < y + h; col++)
                         {
                             var i = col * (int)width * 4 + row * 4;
                             var r = Cycles.ColorClamp((int)(pixels[i] * 255.0f));
                             var g = Cycles.ColorClamp((int)(pixels[i + 1] * 255.0f));
                             var b = Cycles.ColorClamp((int)(pixels[i + 2] * 255.0f));
                             var a = Cycles.ColorClamp((int)(pixels[i + 3] * 255.0f));

                             Int64 pBackBuffer = (Int64)writeableBitmap.BackBuffer;
                            
                             pBackBuffer += (int)col * writeableBitmap.BackBufferStride;
                             pBackBuffer += (int)row * 4;

                             int color_data = r << 16; // R
                             color_data |= g << 8;   // G
                             color_data |= b << 0;
                                     
                             Data[ctr++] = color_data;

                             *((Int64*)pBackBuffer) = color_data;
                         }
                     }
                 }
              //  writeableBitmap.WritePixels(new Int32Rect((int)x, (int)y, (int)w, (int)h),Data,64,0);
                 writeableBitmap.AddDirtyRect(new Int32Rect((int)x, (int)y, (int)w, (int)h));

                 writeableBitmap.Unlock();
         }

         public void WriteRenderTileCallback(uint sessionId, uint x, uint y, uint w, uint h, uint depth)
         {
             Write(string.Format("C# Write Render Tile for session {0} at ({1},{2}) [{3}]", sessionId, x, y, depth), updateType.writeTile);
             DispatcherHelper.UIDispatcher.BeginInvoke((Action)delegate
             {
                 UpdateBitmap(x, y, w, h);
             });
         }

         public  void UpdateRenderTileCallback(uint sessionId, uint x, uint y, uint w, uint h, uint depth)
         {
             Write(string.Format("C# Update Render Tile for session {0} at ({1},{2}) [{3}]", sessionId, x, y, depth), updateType.updateTile);
         }

         public  void LoggerCallback(string msg)
         {
             Write(string.Format("DBG: {0}", msg), updateType.logger);
         }
         
         void Write(string msg, updateType u)
         {
             switch (u)
             {
                 case updateType.logger: Logger = "Logger : " + msg;  
                     break;
                 case updateType.writeTile: WriteTile = "Writter : " + msg;
                     break;
                 case updateType.updateTile: UpdateTile = "Update : " + msg;
                     break;
                 case updateType.status: Status = "Status : " + msg;
                     break;
                 default:
                     break;
             }
            
         }

         private RelayCommand cancel;

         public RelayCommand Cancel
         {
             get
             {
                 return cancel ?? (cancel = new RelayCommand(() =>
                     {
                         Cycles.Session.Cancel("asdfhh");
                     }));
             }
         }
         

         private RelayCommand start;

         public RelayCommand Start
         {
             get
             {
                 return start ?? (start = new RelayCommand(() =>
                     {
                        // Cycles.Session.Reset((uint)Cycles.scene.Camera.Size.Width, (uint)Cycles.scene.Camera.Size.Width, 5);
                         Cycles.Session.Start();
                        // Cycles.Session.Wait();
                     }));
             }
         }

         private RelayCommand update;

         public RelayCommand Update
         {
             get
             {
                 return update ?? (update = new RelayCommand(() =>
                     {
                         UpdateBitmap(0, 0, 400, 400);
                     }));
             }
         }

         private string cameraView;

         public string CameraView
         {
             get { return cameraView; }
             set { cameraView = value;
             RaisePropertyChanged("CameraView");

             var t = stringToTransform(cameraView);
             Cycles.Session.Cancel("Asdf");
             Cycles.Session.Scene.Camera.Matrix = t;
             Cycles.Session.Scene.Camera.ComputeAutoViewPlane();
             Cycles.Session.Scene.Camera.Update();
          
             Task.Factory.StartNew(() =>
                 {
                     Cycles.Session.Start();
                 });
             }
         }
        
        
    }
}
