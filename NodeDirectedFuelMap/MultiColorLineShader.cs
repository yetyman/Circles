﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NodeDirectedFuelMap
{
    class MultiColorLineShader
    {
        int Handle;
        Stopwatch _timer = new Stopwatch();
        public int VertexShader { get; private set; }
        public int FragmentShader { get; private set; }
        public int PositionLocation { get; private set; }
        public int LayerLocation { get; internal set; }
        public int ViewPortSizeLocation { get; internal set; }
        public Vector2i ViewPortSize { get; internal set; }
        public int Layer { get; set; }

        public MultiColorLineShader(Vector2i viewPortSize, string vertexPath, string fragmentPath)
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


            PositionLocation = GL.GetAttribLocation(Handle, "aPosition");

            LayerLocation = GL.GetUniformLocation(Handle, "layer");
        }
        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }
        public void Use()
        {
            GL.UseProgram(Handle);
            //GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);//sooo much prettier
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.Uniform1(LayerLocation, Layer);
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

        ~MultiColorLineShader()
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