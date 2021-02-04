using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace funShapes
{
    class PointShader
    {
        int Handle;
        Stopwatch _timer = new Stopwatch();
        public int VertexShader { get; private set; }
        public int FragmentShader { get; private set; }
        public int PositionLocation { get; private set; }
        public int SizeLocation { get; private set; }
        public int ZoomLocation { get; private set; }
        public int RowOffsetLocation { get; private set; }
        public int RowScaleLocation { get; private set; }
        public int ColumnCountLocation { get; private set; }
        public int SquareCornerLocation { get; private set; }
        public int LayerLocation { get; private set; }
        public int ViewportSizeLocation { get; private set; }
        public int ColorLocation { get; private set; }
        public float PointSize { get; set; }
        public Vector3 OddRowOffset { get; set; }
        public Vector3 OddRowHeightScale { get; set; }
        public int ColumnCount { get; set; }
        public Vector2i ViewPortSize { get; internal set; }

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

            ///.vert implementation specific
            PositionLocation = 0;
            SquareCornerLocation = 1;

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



            SizeLocation = GL.GetUniformLocation(Handle, "aSize");
            ZoomLocation = GL.GetUniformLocation(Handle, "aZoom");
            RowOffsetLocation = GL.GetUniformLocation(Handle, "aRowOffset");
            RowScaleLocation = GL.GetUniformLocation(Handle, "aRowScale");
            ColumnCountLocation = GL.GetUniformLocation(Handle, "aColumnCount");
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
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            GL.Uniform4(ColorLocation, 0, 0,1f,.5f);
            GL.Uniform2(ViewportSizeLocation, ViewPortSize);
            GL.Uniform1(ZoomLocation, 1f);
            GL.Uniform1(SizeLocation, PointSize);
            GL.Uniform3(RowOffsetLocation, OddRowOffset);
            GL.Uniform3(RowScaleLocation, OddRowHeightScale);
            GL.Uniform1(ColumnCountLocation, ColumnCount);
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