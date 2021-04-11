﻿using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace FuelMap
{
    class MultiViewShader
    {
        int Handle;
        Stopwatch _timer;
        public int VertexShader { get; private set; }
        public int FragmentShader { get; private set; }
        public int OpacityTextureLocation { get; private set; }
        public int ColorLocation { get; private set; }
        public int MinMaxLocation { get; private set; }
        public int WindowBoundsLocation { get; private set; }
        public MultiViewShader(string vertexPath, string fragmentPath)
        {
            _timer = new Stopwatch();
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



            WindowBoundsLocation = GL.GetUniformLocation(Handle, "windowBounds");
            MinMaxLocation = GL.GetUniformLocation(Handle, "minmax");
            ColorLocation = GL.GetUniformLocation(Handle, "LayerColor");
            OpacityTextureLocation = GL.GetUniformLocation(Handle, "opacityMap");

            _timer.Start();

        }
        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }
        public void Use(int textureId, RectangleF windowBounds, float[] minmax)
        {
            GL.UseProgram(Handle);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            //double timeValue = _timer.Elapsed.TotalSeconds;
            //float greenValue = (float)Math.Sin(timeValue) / 4.0f + 0.5f;
            GL.Uniform4(ColorLocation, 1f, 1f, 1f, 1f);
            GL.Uniform4(WindowBoundsLocation, windowBounds.X, windowBounds.Y, windowBounds.Width, windowBounds.Height);
            GL.Uniform2(MinMaxLocation, minmax[0], minmax[1]);

            CheckGPUErrors("Error setting color:");

            GL.ActiveTexture(TextureUnit.Texture0); //select texture unit(hardware) slot
            GL.BindTexture(TextureTarget.Texture2D, textureId); //set the slot to the pointer to texture in gpu memory
            GL.Uniform1(OpacityTextureLocation, 0); //set sampler2D to Texture Processing Unit 0. sampler2D implicitly has a different set of locations, they are the Texture Units(hardware) in the GPU

            CheckGPUErrors("Error setting texture:");
        }

        private void CheckGPUErrors(string errorPrefix)
        {
            ErrorCode err;

            while ((err = GL.GetError()) != ErrorCode.NoError)
            {
                //Process/log the error.
                Console.WriteLine(errorPrefix + err);
            }
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

        ~MultiViewShader()
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
