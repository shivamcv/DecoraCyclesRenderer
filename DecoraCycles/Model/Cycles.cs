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
        //
        public static Client client;
        public static ccl.Scene scene;
        public static Session Session;

        public static int samples = 10;

        //
        Device dev;
        public Cycles()
        {
            CSycles.set_kernel_path("lib");
            CSycles.initialise();
            
            client = new Client();

        
            var scene_params = new SceneParameters(client, ShadingSystem.SVM, BvhType.Static, false, false, false, false);
            scene = new ccl.Scene(client, scene_params, Device.FirstCuda);

            
        }

        public void Render(ccl.CSycles.RenderTileCallback WriteRenderTileCallback,
            ccl.CSycles.RenderTileCallback UpdateRenderTileCallback,
            CSycles.UpdateCallback StatusUpdateCallback,
            ccl.CSycles.LoggerCallback LoggerCallback)
        {
          

            LoadBackground();

            StartSession(WriteRenderTileCallback,UpdateRenderTileCallback,StatusUpdateCallback,LoggerCallback);

            //uint bufsize;
            //uint bufstride;

            //CSycles.session_get_buffer_info(client.Id, Session.Id, out bufsize, out bufstride);
            //var pixels = CSycles.session_copy_buffer(client.Id, Session.Id, bufsize);

            //var width = (uint)scene.Camera.Size.Width;
            //var height = (uint)scene.Camera.Size.Height;

            //var bmp = new Bitmap((int)width, (int)height);
            //for (var x = 0; x < width; x++)
            //{
            //    for (var y = 0; y < height; y++)
            //    {
            //        var i = y * (int)width * 4 + x * 4;
            //        var r = ColorClamp((int)(pixels[i] * 255.0f));
            //        var g = ColorClamp((int)(pixels[i + 1] * 255.0f));
            //        var b = ColorClamp((int)(pixels[i + 2] * 255.0f));
            //        var a = ColorClamp((int)(pixels[i + 3] * 255.0f));
            //        bmp.SetPixel(x, y, Color.FromArgb(a, r, g, b));
            //    }
            //}
            //bmp.Save("test.bmp");


        }

        public static void StartSession(ccl.CSycles.RenderTileCallback WriteRenderTileCallback,
            ccl.CSycles.RenderTileCallback UpdateRenderTileCallback,
            CSycles.UpdateCallback StatusUpdateCallback,
            ccl.CSycles.LoggerCallback LoggerCallback)
        {
            client = new Client();
                       
            CSycles.set_logger(client.Id, LoggerCallback);

            var session_params = new SessionParameters(client, Device.FirstCuda)
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
        static public Shader create_some_setup_shader()
        {
            var some_setup = new Shader(client, Shader.ShaderType.Material)
            {
                Name = "some_setup",
                UseMis = false,
                UseTransparentShadow = true,
                HeterogeneousVolume = false
            };


            var brick_texture = new BrickTexture();
            brick_texture.ins.Vector.Value = new float4(0.000f, 0.000f, 0.000f);
            brick_texture.ins.Color1.Value = new float4(0.800f, 0.800f, 0.800f);
            brick_texture.ins.Color2.Value = new float4(0.200f, 0.200f, 0.200f);
            brick_texture.ins.Mortar.Value = new float4(0.000f, 0.000f, 0.000f);
            brick_texture.ins.Scale.Value = 1.000f;
            brick_texture.ins.MortarSize.Value = 0.020f;
            brick_texture.ins.Bias.Value = 0.000f;
            brick_texture.ins.BrickWidth.Value = 0.500f;
            brick_texture.ins.RowHeight.Value = 0.250f;

            var checker_texture = new CheckerTexture();
            checker_texture.ins.Vector.Value = new float4(0.000f, 0.000f, 0.000f);
            checker_texture.ins.Color1.Value = new float4(0.000f, 0.004f, 0.800f);
            checker_texture.ins.Color2.Value = new float4(0.200f, 0.000f, 0.007f);
            checker_texture.ins.Scale.Value = 5.000f;

            var diffuse_bsdf = new DiffuseBsdfNode();
            diffuse_bsdf.ins.Color.Value = new float4(0.800f, 0.800f, 0.800f);
            diffuse_bsdf.ins.Roughness.Value = 0.000f;
            diffuse_bsdf.ins.Normal.Value = new float4(0.000f, 0.000f, 0.000f);

            var texture_coordinate = new TextureCoordinateNode();          


            some_setup.AddNode(brick_texture);
            some_setup.AddNode(checker_texture);
            some_setup.AddNode(diffuse_bsdf);
            some_setup.AddNode(texture_coordinate);

            brick_texture.outs.Color.Connect(diffuse_bsdf.ins.Color);
            checker_texture.outs.Color.Connect(brick_texture.ins.Mortar);
            texture_coordinate.outs.Normal.Connect(checker_texture.ins.Vector);
            texture_coordinate.outs.UV.Connect(brick_texture.ins.Vector);

            diffuse_bsdf.outs.BSDF.Connect(some_setup.Output.ins.Surface);

            some_setup.FinalizeGraph();

            return some_setup;
        }
        public static int ColorClamp(int ch)
        {
            if (ch < 0) return 0;
            return ch > 255 ? 255 : ch;
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
