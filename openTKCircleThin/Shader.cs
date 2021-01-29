using OpenTK.Graphics.ES30;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace openTKCircleThin
{
class Shader
{
    int Handle;
    Stopwatch _timer = new Stopwatch();
    public int VertexShader { get; private set; }
    public int FragmentShader { get; private set; }
    public Shader(string vertexPath, string fragmentPath)
    {
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
    }
    public int GetAttribLocation(string attribName)
    {
        return GL.GetAttribLocation(Handle, attribName);
    }
    public void Use()
    {
        GL.UseProgram(Handle);
        //double timeValue = _timer.Elapsed.TotalSeconds;
        //float greenValue = (float)Math.Sin(timeValue) / (2.0f + 0.5f);
        //int vertexColorLocation = GL.GetUniformLocation(Handle, "ourColor");
        //GL.Uniform4(vertexColorLocation, 0.0f, greenValue, 0.0f, 1.0f);
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

        ~Shader()
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
