using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace FuelMap
{
    class SomeMinAndDivideShader
    {
        int Handle;
        Stopwatch _timer;
        public int VertexShader { get; private set; }
        public int FragmentShader { get; private set; }
        public int OpacityTextureLocation { get; private set; }
        public int ColorLocation { get; private set; }

        public int FromTextureLocation { get; private set; }
        public int SubtractTextureLocation { get; private set; }
        public int FromTexture { get; private set; }
        public int SubtractTexture { get; private set; }
        public SomeMinAndDivideShader(string vertexPath, string fragmentPath, int fromTexture, int subtractTexture)
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



            ColorLocation = GL.GetUniformLocation(Handle, "LayerColor");
            OpacityTextureLocation = GL.GetUniformLocation(Handle, "opacityMap");
            FromTextureLocation = GL.GetUniformLocation(Handle, "fromMap");
            SubtractTextureLocation = GL.GetUniformLocation(Handle, "subtractMap");
            FromTexture = fromTexture;
            SubtractTexture = subtractTexture;


            _timer.Start();

        }
        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }
        public void Use()
        {
            GL.UseProgram(Handle);
            GL.BlendFunc(BlendingFactor.Zero, BlendingFactor.One);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            //double timeValue = _timer.Elapsed.TotalSeconds;
            //float greenValue = (float)Math.Sin(timeValue) / 4.0f + 0.5f;
            GL.Uniform4(ColorLocation, 0, 1, 0f, 1f);

            CheckGPUErrors("Error setting color:");

            GL.ActiveTexture(TextureUnit.Texture0); //select texture unit(hardware) slot
            GL.BindTexture(TextureTarget.Texture2D, FromTexture); //set the slot to the pointer to texture in gpu memory

            GL.ActiveTexture(TextureUnit.Texture1); //select texture unit(hardware) slot
            GL.BindTexture(TextureTarget.Texture2D, SubtractTexture); //set the slot to the pointer to texture in gpu memory

            GL.Uniform1(FromTextureLocation, 0);
            GL.Uniform1(SubtractTextureLocation, 1);

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

        ~SomeMinAndDivideShader()
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
