﻿using ccl;
using ccl.ShaderNodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace DecoraCsycles.Model
{
  public  class CyclesShader
    {
      string errors = "";
      List<ShaderNode> Nodes = new List<ShaderNode>();
      List<Link> Links = new List<Link>();
      List<int> ignoreShaders = new List<int>();
      Shader currentShader;
        internal void GetMaterial(string path, ref uint id)
        {
            try
            {
                currentShader = new Shader(Cycles.client, Shader.ShaderType.Material);

                var xDoc = XDocument.Load(new StreamReader(path));
                XElement root = xDoc.Elements().First();

                foreach (var node in root.Nodes())
                {
                    switch (((XElement)node).Name.LocalName)
                    {
                        case "nodes":
                            {
                                readNode((XElement)node);
                            }
                            break;

                        case "links":
                            {
                                readLinks((XElement)node);
                            }
                            break;

                        case "groups":
                            {

                            }
                            break;
                        default:
                            throw new NotImplementedException();
                            break;
                    }
                }


                foreach (var temp in Links)
                {
                    if (!ignoreShaders.Contains(temp.from) && !ignoreShaders.Contains(temp.to))
                    {
                        var from = Nodes.ElementAt(temp.from).outputs.Socket(Nodes.ElementAt(temp.from).outputs.PropertyNames.ElementAt(temp.output));
                        var to = Nodes.ElementAt(temp.to).inputs.Socket(Nodes.ElementAt(temp.to).inputs.PropertyNames.ElementAt(temp.input));
                        from.Connect(to);
                    }
                }

                for (int i = 0; i < Nodes.Count; i++)
                {
                    if(!ignoreShaders.Contains(i))
                    currentShader.AddNode(Nodes[i]);
                }

                currentShader.FinalizeGraph();

                id = Cycles.scene.AddShader(currentShader);
            }catch(Exception ex)
            {
                errors += "\n" + ex.Message;
            }
           if (!string.IsNullOrEmpty(errors))
           {
               File.WriteAllText("errors.txt", errors);
               Process.Start("errors.txt");
           }
        }

        private void readLinks(XElement root)
        {
            foreach (XElement node in root.Nodes())
            {
                if (node.Name.LocalName != "link")
                    throw new InvalidOperationException();

                Link temp = readLink(node);

                Links.Add(temp);
               
                
            }
        }

        private Link readLink(XElement node)
        {
            Link temp = new Link();

            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "to":
                        {
                            temp.to = int.Parse(prop.Value);
                        }
                        break;
                    case "from":
                        {
                            temp.from = int.Parse(prop.Value);
                        }
                        break;
                    case "input":
                        {
                            temp.input = int.Parse(prop.Value);
                        }
                        break;
                    case "output":
                        {
                            temp.output = int.Parse(prop.Value);
                        }
                        break;
                    default :
                        errors += "\n " + node.Value;
                        break;
                }
            }

            return temp;
        }

        private  void readNode(XElement root)
        {
            int i = 0;
            foreach (XElement node in root.Nodes())
            {
                switch (node.Attribute("type").Value)
                {
                    case "OUTPUT_MATERIAL":
                        {
                            OutputNode temp = new OutputNode();
                            Nodes.Add(temp);
                        }
                        break;
                    case "BSDF_DIFFUSE":
                        {
                            DiffuseBsdfNode temp = getDiffuseBsdfNode(node);
                            Nodes.Add(temp);
                        }   
                        break;
                    case "BSDF_TRANSPARENT" :
                        {
                            RefractionBsdfNode temp = getRefractionBsdfNode(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "BSDF_GLOSSY":
                        {
                            GlossyBsdfNode temp = getGlossyBsdfNode(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "MIX_SHADER":
                        {
                            MixClosureNode temp =  getMixClosureNode(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "MIX_RGB":
                        {
                            MixNode temp = getMixNode(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "LAYER_WEIGHT":
                        {
                            LayerWeightNode temp = getLayerWeightNode(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "RGB":
                        {
                            ColorNode temp =  getColorNode(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "FRESNEL":
                        {
                            FresnelNode temp = getFresnelNode(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "GAMMA":
                        {
                            GammaNode temp = getGammaNode(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "TEX_VORONOI":
                        {
                            VoronoiTexture temp = getVoronoiTexture(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "MATH":
                        {
                            MathNode temp = getMathNode(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "MAPPING":
                        {
                            MappingNode temp = getMappingNode(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "TEX_NOISE":
                        {
                            NoiseTexture temp = getNoiseTexture(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "TEX_COORD":
                        {
                            TextureCoordinateNode temp = getTextureCoordinateNode(node);
                            Nodes.Add(temp);
                        }
                        break;
                    case "VECT_MATH": 
                    case "BSDF_VELVET":
                    case "HUE_SAT":
                    case "BRIGHTCONTRAST"://not found
                    case "VALTORGB": //not found
                    case "HOLDOUT": //not found
                        ignoreShaders.Add(i);
                        Nodes.Add(new DiffuseBsdfNode());
                        errors += "\n " + node.Value;
                         break;
                    default:
                         errors += "\n " + node.Value;
                        break;
                }
                i++;
            }
        }

        private TextureCoordinateNode getTextureCoordinateNode(XElement node)
        {
            TextureCoordinateNode temp = new TextureCoordinateNode();

            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "dupli":
                        {
                         
                        }
                        break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private NoiseTexture getNoiseTexture(XElement node)
        {
            NoiseTexture temp = new NoiseTexture();

            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "scale":
                        {
                            temp.ins.Scale.Value = readFloat(prop.Value);
                        }
                        break;
                    case "detail":
                        {
                            temp.ins.Detail.Value = readFloat(prop.Value);
                        }
                        break;
                    case "distortion":
                        {
                            temp.ins.Distortion.Value = readFloat(prop.Value);
                        }
                        break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private MappingNode getMappingNode(XElement node)
        {
            MappingNode temp = new MappingNode();
            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "vector":
                        {
                            temp.ins.Vector.Value = readVector(prop.Value);
                        }
                        break;
                    case "use_max":
                        {
                            temp.UseMax = string.Compare("true", prop.Value, true) == 0 ? true : false;
                        }break;
                    case "use_min":
                        {
                            temp.UseMin = string.Compare("true", prop.Value, true) == 0 ? true : false;
                        } break;
                    case "translation":
                        {
                            temp.Translation = readVector(prop.Value);
                        }
                        break;
                    case "rotation":
                        {
                            temp.Rotation = readVector(prop.Value);
                        }
                        break;
                    case "scale":
                        {
                            temp.Scale = readVector(prop.Value);
                        }
                        break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private float4 readVector(string p)
        {
            int i = p.IndexOf('(') + 1;
            int j = p.IndexOf(')');
            string temp = p.Substring(i, j - i);

            var w = temp.Split(',');

            if (w.Count() != 3)
                throw new InvalidOperationException();

            float[] r = new float[3];

            for (int k = 0; k < 3; k++)
                r[k] = readFloat(w[k]);

            return new float4(r[0], r[1], r[2]);
        }

        private MathNode getMathNode(XElement node)
        {
            MathNode temp = new MathNode();
            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "operation":
                        {
                            temp.Operation =  (MathNode.Operations)Enum.Parse(typeof(MathNode.Operations), prop.Value, true);
                        }
                        break;
                    case "use_clamp":
                        {
                            temp.UseClamp = string.Compare("true", prop.Value, true) == 0 ? true : false;
                        }
                        break;
                    case "value1":
                        {
                            temp.ins.Value1.Value = readFloat(prop.Value);
                        }break;
                    case "value2" :
                        {
                            temp.ins.Value2.Value = readFloat(prop.Value);
                        }break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private VoronoiTexture getVoronoiTexture(XElement node)
        {
            VoronoiTexture temp = new VoronoiTexture();
            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "scale":
                        {
                            temp.ins.Scale.Value = readFloat(prop.Value);
                        }
                        break;
                    case "coloring":
                        {
                            temp.Coloring = prop.Value;
                        }
                        break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private GammaNode getGammaNode(XElement node)
        {
            GammaNode temp = new GammaNode();
            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "color":
                        {
                            temp.ins.Color.Value = readColor(prop.Value);
                        }
                        break;
                    case "gamma":
                        {
                            temp.ins.Gamma.Value = readFloat(prop.Value);
                        }
                        break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;

                        break;
                }
            }

            return temp;
        }

        private FresnelNode getFresnelNode(XElement node)
        {
            FresnelNode temp = new FresnelNode();
            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "ior":
                        {
                            temp.ins.IOR.Value = readFloat(prop.Value);
                        }
                        break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private MixClosureNode getMixClosureNode(XElement node)
        {
            MixClosureNode temp = new MixClosureNode();
            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "fac":
                        {
                            temp.ins.Fac.Value = readFloat(prop.Value);
                        }
                        break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private ColorNode getColorNode(XElement node)
        {
            ColorNode temp = new ColorNode();
            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "color":
                        {
                          temp.outs.Color.Value =   readColor(prop.Value);
                        }
                        break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private LayerWeightNode getLayerWeightNode(XElement node)
        {
            LayerWeightNode temp = new LayerWeightNode();
            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "blend":
                        {
                            temp.ins.Blend.Value = readFloat(prop.Value);
                        }
                        break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private GlossyBsdfNode getGlossyBsdfNode(XElement node)
        {
            GlossyBsdfNode temp = new GlossyBsdfNode();
            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "color":
                        {
                            temp.ins.Color.Value = readColor(prop.Value);
                        }
                        break;
                    case "roughness":
                        {
                            temp.ins.Roughness.Value = readFloat(prop.Value);
                           
                        }
                        break;
                    case "distribution" :
                        {
                            temp.Distribution = prop.Value;
                        }
                        break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private MixNode getMixNode(XElement node)
        {
            MixNode temp = new MixNode();
            foreach (var prop in node.Attributes())
            {
                switch (prop.Name.LocalName)
                {
                    case "fac":
                        {
                            temp.ins.Fac.Value = readFloat(prop.Value);
                        }
                        break;
                    case "color1":
                        {
                            temp.ins.Color1.Value = readColor(prop.Value);
                        }
                        break;
                    case "color2":
                        {
                            temp.ins.Color2.Value = readColor(prop.Value);
                        }
                        break;
                    case "blend_type" :
                        {
                            temp.BlendType = prop.Value;
                        }
                        break;
                    case "use_clamp": // not found
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private RefractionBsdfNode getRefractionBsdfNode(XElement node)
        {
            RefractionBsdfNode temp = new RefractionBsdfNode();
             foreach (var prop in node.Attributes())
            {
                switch(prop.Name.LocalName)
                {
                    case "color":
                        {
                            temp.ins.Color.Value = readColor(prop.Value);
                        }
                        break;
                    case "width":
                    case "loc":
                    case "type":
                        break;
                    default:
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

             return temp;
        }

        private DiffuseBsdfNode getDiffuseBsdfNode(XElement node)
        {
            DiffuseBsdfNode temp = new DiffuseBsdfNode();

            foreach (var prop in node.Attributes())
            {
                switch(prop.Name.LocalName)
                {
                    case "color":
                        {
                            temp.ins.Color.Value = readColor(prop.Value);
                        }
                        break;
                    case "roughness" :
                        {
                            temp.ins.Roughness.Value = readFloat(prop.Value);
                        }
                        break;
                    case "width":
                    case "loc" :
                    case "type" :
                        break;
                    default :
                        errors += "\n " + node.Value + " : " + prop.Value;
                        break;
                }
            }

            return temp;
        }

        private float readFloat(string p)
        {
            float f;
            float.TryParse(p, out f);
             return f;
        }

        private float4 readColor(string p)
        {
            int i = p.IndexOf('(') +1;
            int j = p.IndexOf(')');
            string temp = p.Substring(i , j - i);

            var w = temp.Split(',');

            if (w.Count() != 4)
                throw new InvalidOperationException();

            float[] r = new float[4];

            for (int k = 0; k < 4; k++)
                    r[k] = readFloat(w[k]);

            return new float4(r[0], r[1], r[2], r[3]);
        }
    }

    public struct Link
    {
        public int output { get; set; }
        public int input { get; set; }
        public int from { get; set; }
        public int to { get; set; }
    }
}
