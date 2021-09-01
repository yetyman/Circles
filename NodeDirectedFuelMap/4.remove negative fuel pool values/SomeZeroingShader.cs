using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace NodeDirectedFuelMap
{
    class SomeZeroingShader
    {
        int Handle;
        Stopwatch _timer;
        public int VertexShader { get; private set; }
        public int FragmentShader { get; private set; }

        public int FromTextureLocation { get; private set; }
        public int FromTexture { get; private set; }
        public float Average { get; private set; }
        public SomeZeroingShader(string vertexPath, string fragmentPath, int fromTexture)
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



            FromTextureLocation = GL.GetUniformLocation(Handle, "fromMap");
            FromTexture = fromTexture;


            _timer.Start();

        }
        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }
        public void Use()
        {
            GL.UseProgram(Handle);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);

            CheckGPUErrors("Error setting color:");

            GL.ActiveTexture(TextureUnit.Texture0); //select texture unit(hardware) slot
            GL.BindTexture(TextureTarget.Texture2D, FromTexture); //set the slot to the pointer to texture in gpu memory

            GL.Uniform1(FromTextureLocation, 0);//because its texture0

            CheckGPUErrors("Error setting texture:");
        }
        public void CheckAverage(float w, float h)
        {
            GL.GenerateTextureMipmap(FromTexture);
            CheckGPUErrors("Error generating mipmap:");

            float fPixel = new float();

            GL.BindTexture(TextureTarget.Texture2D, FromTexture);
            CheckGPUErrors("Error binding texture:");

            var level = (int)(Math.Floor(Math.Log2(Math.Max(w, h))));
            try
            {
                GL.GetTexImage<float>(TextureTarget.Texture2D, level, PixelFormat.Red, PixelType.Float, ref fPixel);
            }
            catch (Exception ex)
            {
                //this catch isnt doing anything. app still crashes. figure this out then get the lines to show up
                CheckGPUErrors("Error calculating average:");
            }
            Average = fPixel;//single channel texture
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

        ~SomeZeroingShader()
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
