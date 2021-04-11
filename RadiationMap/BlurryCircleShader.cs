using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RadiationMap
{
    class BlurryCircleShader
    {
        int Handle;
        Stopwatch _timer = new Stopwatch();
        public int VertexShader { get; private set; }
        public int FragmentShader { get; private set; }
        public int PositionLocation { get; private set; }
        public int SizeLocation { get; private set; }
        public int OpacityLocation { get; private set; }
        public int SquareCornerLocation { get; private set; }
        public int LayerLocation { get; private set; }
        public int ViewportSizeLocation { get; private set; }
        public int ColorLocation { get; private set; }
        public Vector2i ViewPortSize { get; internal set; }

        public BlurryCircleShader(Vector2i viewPortSize, string vertexPath, string fragmentPath)
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



            LayerLocation = GL.GetUniformLocation(Handle, "layer");
            ColorLocation = GL.GetUniformLocation(Handle, "aColor");
            ViewportSizeLocation = GL.GetUniformLocation(Handle, "viewPortSize");

        }
        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }
        public void Use()
        {
            GL.UseProgram(Handle);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.Uniform4(ColorLocation, 1f, 0,0,1);
            GL.Uniform2(ViewportSizeLocation, ViewPortSize);
            GL.Uniform1(LayerLocation, 1);
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

        ~BlurryCircleShader()
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