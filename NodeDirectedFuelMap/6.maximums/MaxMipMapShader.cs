using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NodeDirectedFuelMap
{
    class MaxMipMapShader
    {
        int Handle;
        Stopwatch _timer;
        public int ComputeShader { get; private set; }
        public int InputImageLocation { get; private set; }
        public int InputImageLocation2 { get; private set; }
        public int MipMapImageLocation { get; private set; }
        public int InputImageHandle { get; private set; }
        public int InputImageHandle2 { get; private set; }
        public int MipMapImageHandle { get; private set; }
        public int MipMapFrameBufferHandle { get; private set; }
        public int MipLevelLocation { get; private set; }
        public uint MipLevel { get; set; }
        public float MipScale { get; set; }
        private int InputImageWidth = 0;
        private int InputImageHeight = 0;
        public int[] pbo = new int[2];
        public int pboIndex = 0;
        public int DrawCalls = 0;
        public int FrameReadLag = 1;
        public MaxMipMapShader(string computePath, int inputImageHandle, int inputImageHandle2)
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
            InputImageLocation2 = GL.GetUniformLocation(Handle, "img_input_2");
            MipMapImageLocation = GL.GetUniformLocation(Handle, "img_mipmaps");
            MipLevelLocation = GL.GetUniformLocation(Handle, "mip_level");
            InputImageHandle = inputImageHandle;
            InputImageHandle2 = inputImageHandle2;

            GL.BindTexture(TextureTarget.Texture2D, InputImageHandle);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out InputImageHeight);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out InputImageWidth);
            CheckGPUErrors("Error Load8ing compute shader Float Texture:");
            
            MipMapImageHandle = InitializeRGBATexture(InputImageWidth / 2, InputImageHeight);//instantiate this here, should match the format in the shader and be half the width of inputImageHandle
            CheckGPUErrors("Error initializing compute shader3");//just in case


            pbo[0] = GL.GenBuffer();
            pbo[1] = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.PixelPackBuffer, pbo[0]);
            GL.BufferData(BufferTarget.PixelPackBuffer, 3 * 4 * sizeof(float), (IntPtr)0, BufferUsageHint.StreamRead);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, pbo[1]);
            GL.BufferData(BufferTarget.PixelPackBuffer, 3 * 4 * sizeof(float), (IntPtr)0, BufferUsageHint.StreamRead);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            CheckGPUErrors("Error creating pixel pack buffers:");

            _timer.Start();

        }
        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }
        public void Use()
        {
            GL.UseProgram(Handle);

            CheckGPUErrors("Error setting compute shader to current program:");

            GL.BindImageTexture(0, InputImageHandle, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.R16f);
            GL.BindImageTexture(1, InputImageHandle2, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.R16f);

            GL.BindImageTexture(2, MipMapImageHandle, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba16f);

            CheckGPUErrors("Error setting up compute shader:");
            
            //execute the shader once for each mipmap level
            MipLevel = 0;
            MipScale = (float)Math.Pow(2, MipLevel);
            while (MipScale*2 < InputImageHeight)
            {
                GL.Uniform1(MipLevelLocation, MipLevel+1);
                //GL.DispatchCompute(InputImageWidth/(int)MipScale, InputImageHeight/(int)MipScale, 1);
                GL.DispatchCompute(InputImageWidth, InputImageHeight, 1);
                GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);//should block until the compute finishes?
                CheckGPUErrors("Error executing compute shader at mip level "+MipLevel+":");
                MipLevel++;
                MipScale = (float)Math.Pow(2, MipLevel);
            }
            MipLevel--;

            CheckGPUErrors("Error executing compute shader:");
            if(DrawCalls < pbo.Length)
                DrawCalls++;
        }


        float[] ret = new float[3];
        internal float[] ReadMaximum()
        {
            //set the pixelpackbuffer to last unread pbo
            GL.BindBuffer(BufferTarget.PixelPackBuffer, pbo[pboIndex]);
            pboIndex = ++pboIndex % pbo.Length;
            
            //read the pbo since its been read to before
            if (DrawCalls > FrameReadLag)
            {
                var memloc = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);

                unsafe
                {
                    if (IntPtr.Zero != memloc)
                    {
                        Marshal.Copy(memloc, ret, 0, 3);
                    }
                    else
                    {
                        Console.WriteLine("ERROR-reading pixels failed");
                    }
                }

                GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            }
            else
                ret[0] = ret[1] = ret[2] = 0;

            //queue up a read on the current pbo
            GL.ReadPixels(0, 1, 1, 1, PixelFormat.Rgba, PixelType.Float, (IntPtr)0);// x=original_X, y=original_Y, z=original_value
            CheckGPUErrors("Error queueing pixel read to pbo:");

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            CheckGPUErrors("Error resetting pixelPackBuffer:");

            return ret;
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
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            CheckGPUErrors("Error Load3ing compute shader Float Texture:");
            //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, requestTexture, 0);

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
