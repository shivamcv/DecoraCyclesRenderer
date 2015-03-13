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

        private int height = 400;
        public int Height
        {
            get { return height; }
            set
            {
                height = value;
                RaisePropertyChanged("Height");
            }
        }

        private int width = 600;
        public int Width
        {
            get { return width; }
            set
            {
                width = value;
                RaisePropertyChanged("Width");
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
               Task.Factory.StartNew(FetchData);
             
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
         
            Cycles.LoadCamera(Transform.Rotate(pi, new float4(0, 0, 1)) * Transform.Rotate(pi, new float4(0, 1, 0)) * mainVm.CameraView);

            uint id = DecoraShaders.ShaderList["Diffuse"];

            if (arg != null)
               new CyclesShader().GetMaterial(arg.ToString(), ref id);

            Renderer.CycleSetVertices(vertex, index, uv, Transform.Identity(), id);

            DispatcherHelper.UIDispatcher.BeginInvoke((Action)Connected);

            return 0;
        }

        float pi = 22f / 7f;
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
            showProgressWindow();

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

            File.Delete("output1.xml");
            File.WriteAllLines("output1.xml", new string[] { "<?xml version=\"1.0\" ?>"});

            var mainVm = SimpleIoc.Default.GetInstance<MainViewModel>();
            Transform view = (Transform.RhinoToCyclesCam * (Transform.Scale(SunburnScene.CameraScale[0], SunburnScene.CameraScale[1], SunburnScene.CameraScale[2]) *
                        Transform.Rotate(SunburnScene.CameraRotation[0], new float4(SunburnScene.CameraRotation[1], SunburnScene.CameraRotation[2], SunburnScene.CameraRotation[3]))
                         * Transform.Translate(SunburnScene.CameraPosition[0], SunburnScene.CameraPosition[1], SunburnScene.CameraPosition[2]))) ;

            mainVm.FinalView = view.ToString();

            if (mainVm.CameraView == null )
                Cycles.LoadCamera(view);
            else
                Cycles.LoadCamera(Transform.Rotate(22f / 7f, new float4(0, 0, 1)) * Transform.Rotate(22f / 7f, new float4(0, 1, 0)) * mainVm.CameraView);

            Cycles.LoadCamera(cc.CommaSeparateStringToTransform(mainVm.FixedView));

            CyclesShader matLib = new CyclesShader();
            new DecoraShaders();
            for (int i = 0; i < (SunburnScene.MeshCount > 300 ? 300 : SunburnScene.MeshCount); i++)
            {
                using (var namedPipeClientStream = new NamedPipeClientStream(".", PIPENAME + "\\Mesh", PipeDirection.InOut))
                {
                        namedPipeClientStream.Connect();
                        namedPipeClientStream.ReadMode = PipeTransmissionMode.Byte;

                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.Binder = new SerialisableSceneVersionBinder();
                        List<RenderMesh> meshes = (List<RenderMesh>)formatter.Deserialize(namedPipeClientStream);
                        
                   
                        foreach (var mesh in meshes)
                        {
                            uint id = 0;
                            CurrentMesh = i;
                            i++;
                            try
                            {
                                matLib.GetMaterial(mesh.MaterialName, ref id);

                                id =  DecoraShaders.ShaderList["Diffuse"];
                                if(i != 30)
                                    
                              //
                                if ( mesh.Name =="Triangle/Box008"  )
                                        id =  DecoraShaders.ShaderList["Emission"];

                                if (mesh.Name == "Triangle/polySurface13")
                                {
                                    id = DecoraShaders.ShaderList["LampEmission"];
                                }


                                var walls = new List<string>() { "Triangle/pCube1", "Triangle/pCube2", "Triangle/pCube3", "Triangle/pCube4" };
                                if (walls.Contains(mesh.Name))
                                {
                                    id = DecoraShaders.ShaderList["WallColor"];
                                }

                                if (mesh.Name == "Triangle/Box004" || mesh.Name == "Triangle/Box002")
                                {
                                    id = DecoraShaders.ShaderList["BackColor"];

                                    matLib.GetMaterial("Materials\\glossy_wood_orange.bcm", ref id);
                                }

                                if (mesh.Name == "Triangle/Box01" || mesh.Name == "Triangle/polySurface5" || mesh.Name == "Triangle/polySurface6")
                                {
                                    matLib.GetMaterial("Materials\\Cloth.bcm", ref id);
                                }

                                if (mesh.Name == "Triangle/polySurface08")
                                {
                                    matLib.GetMaterial("Materials\\carpaint_flakes_01.bcm", ref id);
                                }

                                //
                                Renderer.CycleSetVertices(mesh.Vertices, mesh.Indices, mesh.UVs, Transform.Identity(), id);  //new Transform( mesh.World).XnaToBlenderMatrix()
                            
                            }catch(Exception ex)
                            {
                                
                            }

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

                                File.AppendAllText("output1.xml", str.ToString());
                            }
                        }
                }
            }

            if (SaveOutput)
            File.Copy("output1.xml", @"F:\Project Cycles\SVN\trunk\Cycles\cycles\csycles_tester\bin\Debug\objects\output1.xml",true);

            //var vertices = "1.0 1.0 -1.0  1.0 -1.0 -1.0  -1.0 -1.0  -1.0  -1.0 1.0 -1.0  1.0 1.0 9.0  0.999999  -1.0 9.0  -1.0 -1.0  9.0  -1.0 1.0 9.0 ";
            //var indices = "0 1 2  2 0 3  4 7 6  6 4 5  0 4 5  5 0 1  1 5 6  6 1 2  2 6 7  7 2 3  4 0 3  3 4 7  ";

            //var emission = Cycles.scene.ShaderWithName("Emission");
            //var eId  = new DecoraShaders().ShaderList["Emission"];
            //AddMesh(vertices, indices, ccl.Transform.Scale(22, 22, 22) * ccl.Transform.Translate(5.854f, 143.813f, 0.617f), eId);
           
            DispatcherHelper.UIDispatcher.BeginInvoke((Action)Connected);
           // Process.Start("errors.txt");

            FetchData();
        }

      

        private void AddMesh( string vertices, string indices, Transform world, uint shaderId)
        {
            
            float[] v = vertices.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(p => float.Parse(p)).ToArray();
            short[] i = indices.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(p => short.Parse(p)).ToArray();

            Renderer.CycleSetVertices(v, i,null, world, shaderId);
        }
       
        public void Connected()
            {
                CloseProgressWindow();
                Views.Homepage p = new Views.Homepage();
                p.DataContext = new HomeViewModel();
                App.RootFrame.Content = p;
               //SimpleIoc.Default.GetInstance<MainViewModel>().Navigate.Execute("Views/Homepage.xaml");
            }

        Views.ProgressWindow ProgressWindow;

        private void CloseProgressWindow()
        {
            ProgressWindow.Close();
        }

        private void showProgressWindow()
        {
            DispatcherHelper.UIDispatcher.BeginInvoke((Action)delegate { 
           if(ProgressWindow == null)
             ProgressWindow  = new Views.ProgressWindow();
             
               // if(!ProgressWindow.IsVisible)
                //ProgressWindow.ShowDialog();
            });
        }
    }

   
}
