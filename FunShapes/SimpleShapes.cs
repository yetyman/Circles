using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace funShapes
{
    public class SimpleShapes : SimpleWindow
    {
        int PointVertexArrayBuffer;
        int PointArrayObject;
        int TwoTriangleElementBuffer;

        int rows = 5;
        int columns = 4;

        PointShader shader;
        float[] squareCorners = {
               0,    0,   0f,
            -.5f,  .5f, 0.0f,     //Top-left vertex
            -.5f, -.5f, 0.0f,     //Bottom-left vertex
            -.5f, -.5f, 0.0f,     //Bottom-left vertex
             .5f, -.5f, 0.0f,     //Bottom-right vertex
             .5f, -.5f, 0.0f,     //Bottom-right vertex
             .5f,  .5f, 0.0f,     //Top-right vertex
             .5f,  .5f, 0.0f,     //Top-right vertex
            -.5f,  .5f, 0.0f,     //Top-left vertex
        };
        float[] hexCorners = {
               0,    0,   0f,
            -.5f,  .2887f, 0.0f,     //Top-left vertex
            -.5f, -.2887f, 0.0f,     //Bottom-left vertex
            -.5f, -.2887f, 0.0f,     //Bottom-left vertex
             -0f, -.5774f, 0.0f,     //Bottom-left vertex
             .5f, -.2887f, 0.0f,     //Bottom-right vertex
             .5f,  .2887f, 0.0f,     //Bottom-right vertex
              0f,  .5774f, 0.0f,     //Top-right vertex
            -.5f,  .2887f, 0.0f,     //Top-left vertex
        };
        float[] octCorners = {
               0,     0,   0f,
            -.5f,  .2071f, 0.0f,     //Top-left vertex
            -.5f, -.2071f, 0.0f,     //Bottom-left vertex
           -.2071f,  -.5f, 0.0f,     //Bottom-left vertex
            .2071f,  -.5f, 0.0f,     //Bottom-right vertex
             .5f, -.2071f, 0.0f,     //Bottom-right vertex
             .5f,  .2071f, 0.0f,     //Top-right vertex
            .2071f,   .5f, 0.0f,     //Top-right vertex
           -.2071f,   .5f, 0.0f,     //Top-left vertex
        };






        float[] vertices = {
               0,    0,   0f,
            -.5f,  .5f, 0.0f,     //Top-left vertex
            -.5f, -.5f, 0.0f,     //Bottom-left vertex
            -.5f, -.5f, 0.0f,     //Bottom-left vertex
             .5f, -.5f, 0.0f,     //Bottom-right vertex
             .5f, -.5f, 0.0f,     //Bottom-right vertex
             .5f,  .5f, 0.0f,     //Top-right vertex
             .5f,  .5f, 0.0f,     //Top-right vertex
            -.5f,  .5f, 0.0f,     //Top-left vertex
             
             // .45f, .75f,//position
             //.4f, .5f, .12f,//sizes

             // .6f, .8f,//position
             //.2f, .5f, .2f,//sizes

             // .5f, .5f,//position
             //.4f, 1f, .12f,//sizes

             // .9f, .15f,//position
             //.2f, .7f, .2f,//sizes

             // .7f, .8f,//position
             //.2f, .5f, .2f,//sizes

        };

        uint[] indices = {  // note that we start from 0!
            0, 1, 2,   // first triangle
            0, 2, 3,    // second triangle
            0, 3, 4,    // second triangle
            0, 4, 5,    // second triangle
            0, 5, 6,    // second triangle
            0, 6, 7,    // second triangle
            0, 7, 8,     
            0, 8, 1,
        };
        public SimpleShapes(int width, int height, string title) : base(width, height, title)
        {
        }
        Random rand = new Random();
        
        private void UpdateLocations()
        {
            for(int i = 0; i <squareCorners.Length; i++)
            {
                var transition = (float)MathHelper.Sin(_timer.ElapsedMilliseconds / 200f) / 2 + .5f;
                shader.OddRowHeightScale = new Vector3(1, MathHelper.Lerp(1, 0.8661f, transition), 1);
                shader.OddRowOffset = new Vector3(MathHelper.Lerp(0, -.3333f,transition), 0, 0);
                vertices[i] = MathHelper.Lerp(octCorners[i], hexCorners[i], transition);
            }

            //for (int i = 0; i < 1; i++)
            //{
            //    //update all kinds of values
            //    int vertexIndex = rand.Next(0, (vertices.Length - 27) / 5) * 5 + 27;
            //    for (int v = 0; v < 5; v++)
            //    {
            //        var randomValue = (float)rand.NextDouble() * .002f - .001f;
            //        if (vertices[vertexIndex + v] + randomValue <= 1 && vertices[vertexIndex + v] + randomValue >= 0)
            //            vertices[vertexIndex + v] += randomValue;
            //    }
            //}
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
        }
        Stopwatch _timer = new Stopwatch();
        private void ProcessingLoopFake()
        {
            var rand = new Random();
            long timeStart = _timer.ElapsedMilliseconds;
            long timeEnd = _timer.ElapsedMilliseconds;
            long frameTime = 16;
            while (true)
            {
                timeStart = _timer.ElapsedMilliseconds;

                UpdateLocations();

                timeEnd = _timer.ElapsedMilliseconds;
                Thread.Sleep(Math.Max((int)(frameTime - (timeEnd - timeStart)),0));//roughly sync updates to frame speed adjusting for this thread's processing time
            }

        }
        int count => rows * columns;
        protected override void OnLoad()
        {
            var verts = vertices.ToList();
            var rand = new Random();

            var xRatio = columns / (float)ClientSize.X;
            var yRatio = rows / (float)ClientSize.Y;
            for(float y = 0; y < rows; y++)
                for (float x = 0; x < columns; x++)
                {
                    verts.Add(x/(columns-1));//(float)rand.NextDouble());
                    verts.Add(y/(rows-1)/ (columns-1)*(float)(rows-1));//(float)rand.NextDouble());
                    verts.Add(0);// (float)rand.NextDouble());
                    verts.Add(0);// (float)rand.NextDouble());
                    verts.Add(0);// (float)rand.NextDouble());
                }
            vertices = verts.ToArray();

            GL.ClearColor(0f, 0f, 0f, 0.0f);
            PointVertexArrayBuffer = GL.GenBuffer();//make triangle object

            shader = new PointShader(ClientSize, "Shape.vert", "Shape.frag");
            shader.PointSize = 2/(float)(columns-1);
            shader.ColumnCount = columns;

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
            GL.VertexAttribPointer(shader.PositionLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), squareCorners.Length * sizeof(float));
            GL.EnableVertexAttribArray(shader.PositionLocation);
            //GL.VertexAttribPointer(shader.SizeLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (squareCorners.Length+2) * sizeof(float));
            //GL.EnableVertexAttribArray(shader.SizeLocation);
            GL.VertexAttribPointer(shader.SquareCornerLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(shader.SquareCornerLocation); 

            GL.VertexAttribDivisor(shader.PositionLocation, 1);//use from start to end, based on instance id instead of vertex index
            GL.VertexAttribDivisor(shader.SquareCornerLocation, 0);//use from start to end, based on vertex index within instance


            TwoTriangleElementBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, TwoTriangleElementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            CheckGPUErrors("Error Loading before Float Buffer");//just in case

            //new Thread(ProcessingLoopFake).Start();
            _timer.Start();
            base.OnLoad();
        }

        private void CheckGPUErrors(string errorPrefix)
        {
            ErrorCode err;

            while ((err = GL.GetError()) != ErrorCode.NoError)
            {
                // Process/log the error.
                Console.WriteLine(errorPrefix + err);
            }
        }
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            UpdateLocations();
            base.OnUpdateFrame(args);
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            
            GL.Clear(ClearBufferMask.ColorBufferBit);
            DrawShapes();
            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }
        public void DrawShapes()
        {
            //draw opacities to float buffer
            shader.Use();

            GL.DrawElementsInstanced(PrimitiveType.TriangleFan, indices.Length, DrawElementsType.UnsignedInt ,(IntPtr)0,(vertices.Length - 27) / 5);

            CheckGPUErrors("Error rendering to back buffer:");
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
