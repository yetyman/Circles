﻿using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace NodeDirectedFuelMap
{
    class MaxMipMapShader
    {
        int Handle;
        Stopwatch _timer;
        public int ComputeShader { get; private set; }
        public int InputImageLocation { get; private set; }
        public int MipMapImageLocation { get; private set; }
        public int InputImageHandle { get; private set; }
        public int MipMapImageHandle { get; private set; }
        public int MipLevelLocation { get; private set; }
        public float MipLevel { get; set; }
        public MaxMipMapShader(string computePath, int inputImageHandle)
        {
            _timer = new Stopwatch();

            string ComputeShaderSource;

            using (StreamReader reader = new StreamReader(computePath, Encoding.UTF8))
            {
                ComputeShaderSource = reader.ReadToEnd();
            }

            ComputeShader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(ComputeShader, ComputeShaderSource);

            GL.CompileShader(ComputeShader);

            string infoLogFrag = GL.GetShaderInfoLog(ComputeShader);

            if (infoLogFrag != System.String.Empty)
                System.Console.WriteLine(infoLogFrag);

            //create a program pointer
            Handle = GL.CreateProgram();

            //slot in the shaders for this program
            GL.AttachShader(Handle, ComputeShader);

            //compile a program with these settings at this pointer
            GL.LinkProgram(Handle);
            //remove the settings, the program is already compiled
            GL.DetachShader(Handle, ComputeShader);
            GL.DeleteShader(ComputeShader);

            InputImageLocation = GL.GetUniformLocation(Handle, "img_input");
            MipMapImageLocation = GL.GetUniformLocation(Handle, "img_mipmaps");
            MipLevelLocation = GL.GetUniformLocation(Handle, "miplevel");
            InputImageHandle = inputImageHandle;

            GL.BindTexture(TextureTarget.Texture2D, InputImageHandle);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int height);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int width);
            CheckGPUErrors("Error Load8ing compute shader Float Texture:");
            MipMapImageHandle = InitializeRGBATexture(width/2, height);//instantiate this here, should match the format in the shader and be half the width of inputImageHandle

            _timer.Start();

        }
        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }
        public float[] Use()
        {
            GL.UseProgram(Handle);

            CheckGPUErrors("Error setting compute shader to current program:");

            GL.ActiveTexture(TextureUnit.Texture0); //select texture unit(hardware) slot
            GL.BindTexture(TextureTarget.Texture2D, InputImageHandle); //set the slot to the pointer to texture in gpu memory

            GL.ActiveTexture(TextureUnit.Texture1); //select texture unit(hardware) slot
            GL.BindTexture(TextureTarget.Texture2D, MipMapImageHandle); //set the slot to the pointer to texture in gpu memory

            GL.Uniform1(InputImageLocation, 0);
            GL.Uniform1(MipMapImageLocation, 1);

            CheckGPUErrors("Error setting up compute shader:");
            
            //execute the shader once for each mipmap level
            MipLevel = 0;
            var inputImageWidth = 0;//get input image width
            var inputImageHeight = 0;//get input image height
            while (Math.Pow(2, MipLevel + 1) < inputImageHeight)
            {
                GL.Uniform1(MipLevelLocation, MipLevel);
                GL.DispatchCompute(inputImageWidth, inputImageHeight, 1);
                GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);//should block until the compute finishes?
                MipLevel++;
            }
            MipLevel--;

            CheckGPUErrors("Error executing compute shader:");

            //get the values at (0,1) from the mipmap image, should be the final 1x1 mipmap
            var output = new float[4];
            GL.ReadPixels(0,1,1,1,PixelFormat.Rgba,PixelType.Float, output);// x=original_X, y=original_Y, z=original_value

            return output[0..2];
        }

        private int InitializeRGBATexture(int width, int height)
        {
            var requestTexture = GL.GenTexture();
            CheckGPUErrors("Error Load4ing compute shader Float Texture:");
            GL.BindTexture(TextureTarget.Texture2D, requestTexture);
            CheckGPUErrors("Error Loadsing compute shader Float Texture:");
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            CheckGPUErrors("Error Load1ing compute shader Float Texture:");
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            CheckGPUErrors("Error Load2ing compute shader Float Texture:");
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);
            CheckGPUErrors("Error Load3ing compute shader Float Texture:");

            return requestTexture;
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
                GL.DeleteTexture(MipMapImageHandle);

                disposedValue = true;
            }
        }

        ~MaxMipMapShader()
        {
            GL.DeleteProgram(Handle);
            GL.DeleteTexture(MipMapImageHandle);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
