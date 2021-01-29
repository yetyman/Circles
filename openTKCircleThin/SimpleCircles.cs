using OpenTK.Graphics.ES30;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace openTKCircleThin
{
    public class SimpleCircles : SimpleWindow
    {
        int PointVertexArrayBuffer;
        int PointArrayObject;
        int TwoTriangleElementBuffer;
        PointShader shader;
        float[] vertices = {
            -.5f,  .5f, 0.0f,     //Top-left vertex
            -.5f, -.5f, 0.0f,   //Bottom-left vertex
             .5f,  .5f, 0.0f,  //Top-right vertex
             .5f, -.5f, 0.0f,    //Bottom-right vertex
             
              -.5f, .5f,//position
             .4f, .5f, .12f,//sizes
             .4f, .1f, .12f,//opacities

              -.3f, .3f,//position
             .2f, .7f, .2f,//sizes
             .4f, .2f, .12f,//opacities

              0f, 0f,//position
             .4f, 1f, .12f,//sizes
             .4f, .3f, .12f,//opacities

              .4f, -.7f,//position
             .2f, .7f, .2f,//sizes
             .4f, .4f, .12f,//opacities

              .2f, .3f,//position
             .2f, .5f, .2f,//sizes
             .4f, .8f, .12f,//opacities

        };

        uint[] indices = {  // note that we start from 0!
            0, 1, 3,   // first triangle
            0, 2, 3    // second triangle
        };
        public SimpleCircles(int width, int height, string title) : base(width, height, title)
        {
        }
        protected override void OnLoad()
        {
            var verts = vertices.ToList();
            var rand = new Random();
            //for(int i = 0; i<140000; i++)
            for (int i = 0; i<10000; i++)
            {
                verts.Add((float)rand.NextDouble()*2-1);
                verts.Add((float)rand.NextDouble()*2- 1);
                verts.Add((float)rand.NextDouble());
                verts.Add((float)rand.NextDouble());
                verts.Add((float)rand.NextDouble());
                verts.Add((float)rand.NextDouble());
                verts.Add((float)rand.NextDouble());
                verts.Add((float)rand.NextDouble());
            }
            vertices = verts.ToArray();
            GL.ClearColor(0f, 0f, 0f, 0.0f);
            PointVertexArrayBuffer = GL.GenBuffer();//make triangle object

            shader = new PointShader(ClientSize, "CircleLayerShader.vert", "CircleShader.frag");


            PointArrayObject = GL.GenVertexArray();

            // ..:: Initialization code (done once (unless your object frequently changes)) :: ..
            // 1. bind Vertex Array Object
            GL.BindVertexArray(PointArrayObject);

            // 2. copy our vertices array in a buffer for OpenGL to use
            GL.BindBuffer(BufferTarget.ArrayBuffer, PointVertexArrayBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

            // 3. then set our vertex attributes pointers
            //which attribute are we settings, how many is it, what is each it?, should it be normalized?, what's the total size?,
            //TODO: i may not want to normalize
            GL.VertexAttribPointer(shader.PositionLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 12 * sizeof(float));
            GL.EnableVertexAttribArray(shader.PositionLocation);
            GL.VertexAttribPointer(shader.SizeLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 14 * sizeof(float));
            GL.EnableVertexAttribArray(shader.SizeLocation);
            GL.VertexAttribPointer(shader.OpacityLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 17 * sizeof(float));
            GL.EnableVertexAttribArray(shader.OpacityLocation);
            GL.VertexAttribPointer(shader.SquareCornerLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(shader.SquareCornerLocation); 

            GL.VertexAttribDivisor(shader.PositionLocation, 1);//use from start to end, based on instance id instead of vertex index
            GL.VertexAttribDivisor(shader.SizeLocation, 1);
            GL.VertexAttribDivisor(shader.OpacityLocation, 1);
            GL.VertexAttribDivisor(shader.SquareCornerLocation, 0);//use from start to end, based on vertex index within instance


            TwoTriangleElementBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, TwoTriangleElementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            base.OnLoad();
        }
       
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit); 
            DrawCircles();
            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }
        public void DrawCircles()
        {
            shader.Use();


            GL.DrawElementsInstanced(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt ,(IntPtr)0,(vertices.Length - 12) / 8);
            //GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, (vertices.Length - 12) / 5);

            ErrorCode err;
            while ((err = GL.GetError()) != ErrorCode.NoError)
            {
                // Process/log the error.
                Console.WriteLine("Error rendering:" + err);
            }
        }
        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(PointVertexArrayBuffer);
            GL.DeleteVertexArray(PointArrayObject);
            shader.Dispose();
            base.OnUnload();
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            shader.ViewPortSize = ClientSize;
            base.OnResize(e);
        }
    }
}
