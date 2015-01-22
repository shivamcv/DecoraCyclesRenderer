using ccl;
using ccl.ShaderNodes;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoraCsycles.Model
{
    public class DecoraShaders
    {
        public Dictionary<string, uint> ShaderList { get; set; }
        public DecoraShaders()
        {
            ShaderList = new Dictionary<string, uint>();
            AddDiffuseShader("Diffuse", Color.LightGray);
            addEmissionShader("Emission", Color.White);
        }

        private void addEmissionShader(string p, Color clr)
        {
            var shader = new Shader(Cycles.client, Shader.ShaderType.Material) { Name = p };

            var emission_node = new EmissionNode();
            emission_node.ins.Color.Value = new float4(clr.R, clr.G, clr.B);
            emission_node.ins.Strength.Value = 50f;

            shader.AddNode(emission_node);
            emission_node.outputs.Socket("Emission").Connect(shader.Output.inputs.Socket("surface"));

            shader.FinalizeGraph();
                  

            var t = Cycles.scene.AddShader(shader);
            ShaderList.Add(p, t);
        }

        private void AddDiffuseShader(string name, Color clr)
        {
            var shader = new Shader(Cycles.client, Shader.ShaderType.Material) { Name = name };

            var diffbsdf = new DiffuseBsdfNode();
            diffbsdf.ins.Color.Value = new float4((float)clr.R / 255f, (float)clr.G / 255f, (float)clr.B / 255f);

            diffbsdf.outputs.Socket("bsdf").Connect(shader.Output.inputs.Socket("surface"));

            shader.AddNode(diffbsdf);
            shader.FinalizeGraph();
            
            var t = Cycles.scene.AddShader(shader);
         

            ShaderList.Add(name, t);
        }
    }
}
