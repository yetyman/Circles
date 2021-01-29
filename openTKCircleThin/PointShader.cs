using OpenTK.Graphics.ES30;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace openTKCircleThin
{
    class PointShader
    {
        int Handle;
        Stopwatch _timer = new Stopwatch();
        public int VertexShader { get; private set; }
        public int FragmentShader { get; private set; }
        //public float MinPointSize { get; private set; }
        //public float MaxPointSize { get; private set; }
        public int PositionLocation { get; private set; }
        public int SizeLocation { get; private set; }
        public int OpacityLocation { get; private set; }
        public int SquareCornerLocation { get; private set; }
        public int LayerLocation { get; private set; }
        public int ViewportSizeLocation { get; private set; }
        public int ColorLocation { get; private set; }
        public Vector2i ViewPortSize { get; internal set; }

        //public int PointSizeMinLocation { get; private set; }
        //public int PointSizeMaxLocation { get; private set; }
        public PointShader(Vector2i viewPortSize, string vertexPath, string fragmentPath)
        {
            ViewPortSize = viewPortSize;
            _timer.Start();
            string VertexShaderSource;

            using (StreamReader reader = new StreamReader(vertexPath, Encoding.UTF8))
            {
                VertexShaderSource = reader.ReadToEnd();
            }

            string FragmentShaderSource;

            using (StreamReader reader = new StreamReader(fragmentPath, Encoding.UTF8))
            {
                FragmentShaderSource = reader.ReadToEnd();
            }

            //the only reason this is its own class...
            //GL.Enable((EnableCap)All.PointSize);
            //GL.GetFloat(GetPName.PointSizeRange, out Vector2 vec);
            //ErrorCode err;
            //while ((err = GL.GetError()) != ErrorCode.NoError)
            //{
            //    // Process/log the error.
            //    Console.WriteLine("Error loading:" + err);
            //}
            //MaxPointSize = vec[1];
            //MinPointSize = vec[0];

            ///.vert implementation specific
            PositionLocation = 0;
            SizeLocation = 1;
            OpacityLocation = 3;
            SquareCornerLocation = 2;

            VertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(VertexShader, VertexShaderSource);

            FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(FragmentShader, FragmentShaderSource);

            GL.CompileShader(VertexShader);

            string infoLogVert = GL.GetShaderInfoLog(VertexShader);
            if (infoLogVert != System.String.Empty)
                System.Console.WriteLine(infoLogVert);

            GL.CompileShader(FragmentShader);

            string infoLogFrag = GL.GetShaderInfoLog(FragmentShader);

            if (infoLogFrag != System.String.Empty)
                System.Console.WriteLine(infoLogFrag);

            //create a program pointer
            Handle = GL.CreateProgram();

            //slot in the shaders for this program
            GL.AttachShader(Handle, VertexShader);
            GL.AttachShader(Handle, FragmentShader);

            //compile a program with these settings at this pointer
            GL.LinkProgram(Handle);
            //remove the settings, the program is already compiled
            GL.DetachShader(Handle, VertexShader);
            GL.DetachShader(Handle, FragmentShader);
            GL.DeleteShader(FragmentShader);
            GL.DeleteShader(VertexShader);



            ColorLocation = GL.GetUniformLocation(Handle, "aColor");
            LayerLocation = GL.GetUniformLocation(Handle, "layer");
            ViewportSizeLocation = GL.GetUniformLocation(Handle, "viewPortSize");
            //PointSizeMinLocation = GL.GetUniformLocation(Handle, "pointSizeMin");
            //PointSizeMaxLocation = GL.GetUniformLocation(Handle, "pointSizeMax");

        }
        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }
        public void Use()
        {

            GL.UseProgram(Handle);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            double timeValue = _timer.Elapsed.TotalSeconds;
            float greenValue = (float)Math.Sin(timeValue) / 4.0f + 0.5f;
            GL.Uniform4(ColorLocation, 0.0f, greenValue, 0.0f, .01f);
            //GL.Uniform4(ColorLocation, 0.0f, greenValue, 0.0f, .007f);
            GL.Uniform2(ViewportSizeLocation, ViewPortSize);
            GL.Uniform1(LayerLocation, 1);
            //GL.Uniform1(PointSizeMinLocation, MinPointSize);
            //GL.Uniform1(PointSizeMaxLocation, MaxPointSize);
            
        }
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteProgram(Handle);

                disposedValue = true;
            }
        }

        ~PointShader()
        {
            GL.DeleteProgram(Handle);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}