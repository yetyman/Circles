using OpenTK.Graphics.ES30;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace openTKCircleThin
{
    public class SimpleTriangles : SimpleWindow
    {
        int TriangleVertexArrayBuffer;
        int TriangleArrayObject;
        int TwoTriangleElementBuffer;
        Shader shader;
        float[] vertices = {
            -.8f, -.8f, 0.0f,  1f, 0f, 0f, //Bottom-left vertex, color
             -1f, 1f, 0.0f,    0f, 1f, 0f, //Top-left vertex, color
             .8f,  .8f, 0.0f,  0f, 0f, 1f,  //Top-right vertex, color
             1f, -1f, 0.0f,    1f, 0f, 0f, //Bottom-right vertex, color
        };
        uint[] indices = {  // note that we start from 0!
            0, 1, 3,   // first triangle
            1, 2, 3    // second triangle
        };
        public SimpleTriangles(int width, int height, string title) : base(width, height, title)
        {
        }
        protected override void OnLoad()
        {
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            TriangleVertexArrayBuffer = GL.GenBuffer();//make triangle object
            //GL.BindBuffer(BufferTarget.ArrayBuffer, TriangleVertexArrayBuffer);///set triangle object to be current object
            //GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);//tell card how big the data of the current object is, in this case the triangle
            
            shader = new Shader("shader.vert", "shader.frag");


            TriangleArrayObject = GL.GenVertexArray();

            // ..:: Initialization code (done once (unless your object frequently changes)) :: ..
            // 1. bind Vertex Array Object
            GL.BindVertexArray(TriangleArrayObject);

            // 2. copy our vertices array in a buffer for OpenGL to use
            GL.BindBuffer(BufferTarget.ArrayBuffer, TriangleVertexArrayBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            // 3. then set our vertex attributes pointers
            //which attribute are we settings, how many is it, what is each it?, should it be normalized?, what's the total size?, 
            GL.VertexAttribPointer(shader.GetAttribLocation("aPosition"), 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(shader.GetAttribLocation("aColor"), 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);


            TwoTriangleElementBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, TwoTriangleElementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            //DrawTriangle();
            base.OnLoad();
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit); 
            DrawTriangle();
            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }
        public void DrawTriangle()
        {
            shader.Use();



            //GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            GL.BindVertexArray(TriangleArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, (IntPtr)0);
        }
        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(TriangleVertexArrayBuffer);
            GL.DeleteVertexArray(TriangleArrayObject);
            shader.Dispose();
            base.OnUnload();
        }
    }
}
