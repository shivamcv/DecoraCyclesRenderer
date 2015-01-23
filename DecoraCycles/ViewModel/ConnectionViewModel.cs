using ccl;
using DecoraCsycles.HelperClasses;
using DecoraCsycles.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace DecoraCsycles.ViewModel
{
   public class ConnectionViewModel :ViewModelBase
    {
        public RenderInitialise SunburnScene { get; set; }
        public const String PIPENAME = "DecoraCycle\\000f6f1e-dcv2-43b5-8999-e6e6b1e93edb";

        public Cycles Renderer { get; set; }
        public DecoraShaders DShaders { get; set; }
        
        private int samples = 10;
        public int Samples
        {
            get { return samples; }
            set
            {
                samples = value;
                RaisePropertyChanged("Samples");
            }
        }

        public bool SaveOutput { get; set; }
        private int currentMesh;

        public int CurrentMesh
        {
            get { return currentMesh; }
            set { currentMesh = value;
                RaisePropertyChanged("CurrentMesh");
            }
        }

        private bool isIndeterminate;
        public bool IsIndeterminate
        {
            get { return isIndeterminate; }
            set
            {
                isIndeterminate = value;
                RaisePropertyChanged("IsIndeterminate");
            }
        }

        private int meshCount;
        public int MeshCount
        {
            get { return meshCount; }
            set { meshCount = value;
            RaisePropertyChanged("MeshCount");
            }
        }
        
        public ConnectionViewModel()
           {
               if (isIndeterminate) return;

               IsIndeterminate = true;
              // Task.Factory.StartNew(FetchData);
              // Task<int>.Factory.StartNew(loadDefaultData,null);
           }

        public int loadDefaultData( object arg)
        {
            var xDoc = XDocument.Load(new StreamReader("output.xml"));

            XElement root = xDoc.Elements().First();

            float[] vertex, uv;
            short[] index;


            vertex = root.Attributes("P").First().Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(p => float.Parse(p)).ToArray();
            index = root.Attributes("verts").First().Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(p => short.Parse(p)).ToArray();
            uv = root.Attributes("UV").First().Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(p => float.Parse(p)).ToArray(); 

            Renderer = new Cycles();
            Cycles.samples = samples;

            DShaders = new DecoraShaders();

            var mainVm = SimpleIoc.Default.GetInstance<MainViewModel>();
            float pi = 22f / 7f;
         
            Renderer.LoadCamera(Transform.Rotate(pi, new float4(0, 0, 1)) * Transform.Rotate(pi, new float4(0, 1, 0)) * mainVm.CameraView);

            uint id = DShaders.ShaderList["Diffuse"];

            if (arg != null)
               new CyclesShader().GetMaterial(arg.ToString(), ref id);

            Renderer.CycleSetVertices(vertex, index, uv, Transform.Identity(), id);

            DispatcherHelper.UIDispatcher.BeginInvoke((Action)Connected);

            return 0;
        }

        public void FetchData()
        {
            using (var namedPipeClientStream = new NamedPipeClientStream(".", PIPENAME, PipeDirection.InOut))
            {
                namedPipeClientStream.Connect();
                namedPipeClientStream.ReadMode = PipeTransmissionMode.Byte;


                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Binder = new SerialisableSceneVersionBinder();
                SunburnScene = (RenderInitialise)formatter.Deserialize(namedPipeClientStream);
            }

            if (Cycles.Session != null)
            {
                Cycles.Session.Cancel("canceled for new session");
                CSycles.shutdown();
              //  Cycles.Session.Destroy();
            }
           

            Renderer = new Cycles();
            Cycles.samples = samples;
            IsIndeterminate = false;
            MeshCount = SunburnScene.MeshCount;
            CurrentMesh = 0;
            DShaders = new DecoraShaders();

            File.Delete("output.xml");
            File.WriteAllLines("output.xml", new string[] { "<?xml version=\"1.0\" ?>"});

            var mainVm = SimpleIoc.Default.GetInstance<MainViewModel>();
            float pi = 22f / 7f;
            Transform view = (Transform.RhinoToCyclesCam * (Transform.Scale(SunburnScene.CameraScale[0], SunburnScene.CameraScale[1], SunburnScene.CameraScale[2]) *
                        Transform.Rotate(SunburnScene.CameraRotation[0], new float4(SunburnScene.CameraRotation[1], SunburnScene.CameraRotation[2], SunburnScene.CameraRotation[3]))
                         * Transform.Translate(SunburnScene.CameraPosition[0], SunburnScene.CameraPosition[1], SunburnScene.CameraPosition[2]))) ;

            mainVm.FinalView = view.ToString();

            if (mainVm.CameraView == null)
                Renderer.LoadCamera(view);
                //Renderer.LoadCamera(SunburnScene.View.XnaToBlenderMatrix() );
            else
                Renderer.LoadCamera(Transform.Rotate(22f / 7f, new float4(0, 0, 1)) * Transform.Rotate(22f / 7f, new float4(0, 1, 0)) * mainVm.CameraView);

            for (int i = 0; i < SunburnScene.MeshCount; i++)
            {
                using (var namedPipeClientStream = new NamedPipeClientStream(".", PIPENAME + "\\Mesh", PipeDirection.InOut))
                {
                    namedPipeClientStream.Connect();
                    namedPipeClientStream.ReadMode = PipeTransmissionMode.Byte;

                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Binder = new SerialisableSceneVersionBinder();
                    RenderMesh mesh = (RenderMesh)formatter.Deserialize(namedPipeClientStream);
                                        
                    uint id = DShaders.ShaderList[mesh.MaterialName];
                   // if(i<2)
                    Renderer.CycleSetVertices(mesh.Vertices, mesh.Indices, mesh.UVs, Transform.Identity(), id);  //new Transform( mesh.World).XnaToBlenderMatrix()

                    if (SaveOutput)
                    {
                        StringBuilder str = new StringBuilder();

                        str.Append("<mesh P=\"");
                        foreach (var item in mesh.Vertices)
                        {
                            str.Append(item + " ");
                        }   

                        str.Append("\" UV=\"");

                        foreach (var item in mesh.UVs)
                        {
                            str.Append(item + " ");
                        }

                        str.Append("\" nverts=\"");

                        for (int j = 0; j < mesh.Indices.Count() / 3; j++)
                        {
                            str.Append("3 ");
                        }

                        str.Append("\" verts=\"");

                        foreach (var item in mesh.Indices)
                        {
                            str.Append(item + " ");
                        }
                        str.Append("\"/>\n");

                        File.AppendAllText("output.xml", str.ToString());
                    }
                }
            }

            if (SaveOutput)
            File.Copy("output.xml", @"F:\Project Cycles\SVN\trunk\Cycles\cycles\csycles_tester\bin\Debug\objects\output.xml",true);
            var diffSHader = Cycles.scene.ShaderWithName("Emission");
            var ids = Cycles.scene.ShaderSceneId(diffSHader);

            var vertices = "1.0 1.0 -1.0  1.0 -1.0 -1.0  -1.0 -1.0  -1.0  -1.0 1.0 -1.0  1.0 1.0 9.0  0.999999  -1.0 9.0  -1.0 -1.0  9.0  -1.0 1.0 9.0 ";
            var indices = "0 1 2  2 0 3  4 7 6  6 4 5  0 4 5  5 0 1  1 5 6  6 1 2  2 6 7  7 2 3  4 0 3  3 4 7  ";
            AddMesh(vertices, indices, ccl.Transform.Scale(22, 22, 22) * ccl.Transform.Translate(5.854f, 3.813f, 0.617f), ids);
           
            DispatcherHelper.UIDispatcher.BeginInvoke((Action)Connected);
        }

        private void AddMesh( string vertices, string indices, Transform world, uint shaderId)
        {
            return;
            float[] v = vertices.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(p => float.Parse(p)).ToArray();
            short[] i = indices.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(p => short.Parse(p)).ToArray();

            Renderer.CycleSetVertices(v, i,null, world, shaderId);
        }
       
        public void Connected()
            {
                Views.Homepage p = new Views.Homepage();
                p.DataContext = new HomeViewModel();
                App.RootFrame.Content = p;
               //SimpleIoc.Default.GetInstance<MainViewModel>().Navigate.Execute("Views/Homepage.xaml");
            }
    }

   
}
