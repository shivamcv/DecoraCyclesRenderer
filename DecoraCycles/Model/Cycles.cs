using ccl;
using ccl.ShaderNodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DecoraCsycles.Model
{

    public class Cycles
    {
     
        public static Client client;
        public static ccl.Scene scene;
        public static Session Session;
        Device dev;

        public static int samples = 10;

        public Cycles()
        {
           CSycles.set_kernel_path("lib");
           CSycles.initialise();

           client = new Client();

           var scene_params = new SceneParameters(client, ShadingSystem.SVM, BvhType.Static, false, false, false, false);
           scene = new ccl.Scene(client, scene_params, Device.Default);
        }

        public void Render(ccl.CSycles.RenderTileCallback WriteRenderTileCallback,
            ccl.CSycles.RenderTileCallback UpdateRenderTileCallback,
            CSycles.UpdateCallback StatusUpdateCallback,
            ccl.CSycles.LoggerCallback LoggerCallback)
        {
            LoadBackground();

            StartSession(WriteRenderTileCallback,UpdateRenderTileCallback,StatusUpdateCallback,LoggerCallback);
        }

        public static void StartSession(ccl.CSycles.RenderTileCallback WriteRenderTileCallback,
            ccl.CSycles.RenderTileCallback UpdateRenderTileCallback,
            CSycles.UpdateCallback StatusUpdateCallback,
            ccl.CSycles.LoggerCallback LoggerCallback)
        {
            client = new Client();
                       
            CSycles.set_logger(client.Id, LoggerCallback);

            var session_params = new SessionParameters(client, Device.Default)
            {
                Experimental = false,
                Samples = (int)samples,
                TileSize = new Size(64, 64),
                StartResolution = 64,
                Threads = 0,
                ShadingSystem = ShadingSystem.SVM,
                Background = true,
                ProgressiveRefine = false
            };

            if (Session != null)
                Session.Cancel("asdf");

            Session = new Session(client, session_params, scene);
            Session.Reset((uint)scene.Camera.Size.Width, (uint)scene.Camera.Size.Height, (uint)samples);

            Session.UpdateCallback = StatusUpdateCallback;
            Session.UpdateTileCallback = UpdateRenderTileCallback;
            Session.WriteTileCallback = WriteRenderTileCallback;
           
                Session.Start();
                Session.Wait();
        }
       
        public void CycleSetVertices(float[] allFloats, short[] indexElements, float[] UVs, Transform world, uint shaderId)
        {
            var ob = CSycles.scene_add_object(Cycles.client.Id, Cycles.scene.Id);
            CSycles.object_set_matrix(Cycles.client.Id, Cycles.scene.Id, ob, world);
            var me = CSycles.scene_add_mesh(Cycles.client.Id, Cycles.scene.Id, ob, shaderId);

            CSycles.mesh_set_verts(Cycles.client.Id, Cycles.scene.Id, me, ref allFloats, (uint)(allFloats.Length / 3));

            for (int i = 0; i < indexElements.Length; i += 3)
            {
                var v0 = indexElements[i];
                var v1 = indexElements[i + 1];
                var v2 = indexElements[i + 2];
                CSycles.mesh_add_triangle(Cycles.client.Id, Cycles.scene.Id, me, (uint)v0, (uint)v1, (uint)v2, shaderId, false);
            }

            if(UVs!= null)
            CSycles.mesh_set_uvs(Cycles.client.Id, Cycles.scene.Id, me, ref UVs, (uint)(UVs.Length / 2));

        }

        private void LoadBackground()
        {
            //var bgnode = new BackgroundNode();
            //bgnode.ins.Color.Value = new float4(0.7f);
            //bgnode.ins.Strength.Value = 1.0f;

            //background_shader.AddNode(bgnode);
            //bgnode.outs.Background.Connect(background_shader.Output.ins.Surface);
            //background_shader.FinalizeGraph();

            //scene.AddShader(background_shader);

            //scene.Background.Shader = background_shader;
            //scene.Background.AoDistance = 0.0f;
            //scene.Background.AoFactor = 0.0f;
            //scene.Background.Visibility = PathRay.PATH_RAY_ALL_VISIBILITY;

            var shader = new Shader(client, Shader.ShaderType.World) { Name = Guid.NewGuid().ToString() };

            //var skynode = new SkyTexture();
            //shader.AddNode(skynode);
            

            var bgnode = new BackgroundNode();
            bgnode.ins.Strength.Value = 1f;
            bgnode.ins.Color.Value = new float4(1f);
            shader.AddNode(bgnode);


           //skynode.outputs.Socket("color").Connect(bgnode.inputs.Socket("color"));

          
            var fromsocket = bgnode.outputs.Socket("background");

            var tonode = shader.Output;
            var tosocket = tonode.inputs.Socket("surface");

            fromsocket.Connect(tosocket);

            scene.AddShader(shader);
            scene.Background.Shader = shader;
            scene.Background.AoDistance = 0.0f;
            scene.Background.AoFactor = 0.0f;
            scene.Background.Visibility = PathRay.AllVisibility;

            shader.FinalizeGraph();
        }

        public void LoadCamera(Transform View)
        {
                client.Scene.Camera.SensorWidth = 32;
                client.Scene.Camera.SensorHeight = 18;
                client.Scene.Camera.NearClip = .1f;
                client.Scene.Camera.FarClip = 2000f;
                client.Scene.Camera.FocalDistance = 0;
                client.Scene.Camera.Fov = .661f;
                client.Scene.Camera.ApertureSize = 0;
                client.Scene.Camera.Size = new Size(800, 600);
                client.Scene.Camera.Type = CameraType.Perspective;
                client.Scene.Camera.Matrix = View;
                client.Scene.Camera.ComputeAutoViewPlane();
                client.Scene.Camera.Update();
        }
      
    }
}
