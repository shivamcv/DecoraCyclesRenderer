
using ccl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;


namespace DecoraCsycles
{
    [Serializable]
    public class RenderInitialise : ISerializable
    {
        public RenderInitialise(SerializationInfo info, StreamingContext context)
        { 
           
            View = stringToTransform(info.GetString("View"));
            MeshCount = info.GetInt32("MeshCount");
            CameraPosition = (float[])info.GetValue("CameraPosition", typeof(float[]));
            CameraRotation = (float[])info.GetValue("CameraRotation", typeof(float[]));
            CameraScale = (float[])info.GetValue("CameraScale", typeof(float[]));
        }

        private Transform stringToTransform(string p)
        { 
            float[] temp =  p.Split(',').Select(q=> float.Parse(q)).ToArray();
           
            return Transform.Transpose(new Transform(temp));
        }

        public RenderInitialise()
        {

        }
        public int MeshCount { get; set; }
        public float[] CameraScale { get; set; }
        public Transform View { get; set; }
        public float[] CameraRotation { get; set; }
        public float[] CameraPosition { get; set; }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {

        }
    }

    [Serializable]
    public class RenderMesh
    {
        public string MaterialName { get; set; }
        public short[] Indices { get; set; }
        public float[] Vertices { get; set; }
        public float[] UVs { get; set; }
        public float[] World { get; set; }
        public float[] scale { get; set; }
        public float[] roatate { get; set; }
        public float[] translate { get; set; }
        public string Name { get; set; }
        public RenderMesh(float[] allFloats, short[] indexElements, float[] uvs, float[] s, float[] r, float[] t, string p, string name = "")
        {
            Name = name;
            Vertices = allFloats;
            Indices = indexElements;
            MaterialName = p;
            UVs = uvs;
            scale = s;
            roatate = r;
            translate = t;
        }

        public RenderMesh(float[] allFloats, short[] indexElements, float[] uvs, float[] world, string p)
        {
            Vertices = allFloats;
            Indices = indexElements;
            MaterialName = p;
            UVs = uvs;
            World = world;
        }
       
    }

    public class SerialisableSceneVersionBinder : SerializationBinder
    {

        public override Type BindToType(string assemblyName, string typeName)
        {
            Type typeToDeserialize = null;

            // For each assemblyName/typeName that you want to deserialize to 
            // a different type, set typeToDeserialize to the desired type.
            String assemVer1 = Assembly.GetExecutingAssembly().FullName;


            if (typeName != "System.Collections.Generic.List`1[[DecoraCsycles.RenderMesh, Decora Studio Designer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]")
            {
                if (assemblyName != assemVer1)
                {
                    // To use a type from a different assembly version,  
                    // change the version number. 
                    // To do this, uncomment the following line of code. 
                    // assemblyName = assemblyName.Replace("1.0.0.0", "2.0.0.0");

                    // To use a different type from the same assembly,  
                    // change the type name.

                    assemblyName = assemVer1;
                }
            }
            else
                typeName = string.Format("System.Collections.Generic.List`1[[DecoraCsycles.RenderMesh, {0}]]", assemVer1);

            typeToDeserialize = Type.GetType(String.Format("{0}, {1}",
                typeName, assemblyName));

            return typeToDeserialize;
        }
    }
}
