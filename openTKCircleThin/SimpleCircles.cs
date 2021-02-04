using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        int FloatMapBuffer;
        int FloatMapTexture;
        PointShader shader;
        TextureShader texShader;
        float[] vertices = {
            -.5f,  .5f, 0.0f,     //Top-left vertex
            -.5f, -.5f, 0.0f,   //Bottom-left vertex
             .5f,  .5f, 0.0f,  //Top-right vertex
             .5f, -.5f, 0.0f,    //Bottom-right vertex
             
             // .45f, .75f,//position
             //.4f, .5f, .12f,//sizes
             //.4f, .5f, .12f,//opacities

             // .6f, .8f,//position
             //.2f, .5f, .2f,//sizes
             //.4f, 1f, .12f,//opacities

             // .5f, .5f,//position
             //.4f, 1f, .12f,//sizes
             //.4f, .3f, .12f,//opacities

             // .9f, .15f,//position
             //.2f, .7f, .2f,//sizes
             //.4f, .2f, .12f,//opacities

             // .7f, .8f,//position
             //.2f, .5f, .2f,//sizes
             //.4f, .8f, .12f,//opacities

        };

        uint[] indices = {  // note that we start from 0!
            0, 1, 3,   // first triangle
            0, 2, 3    // second triangle
        };
        public SimpleCircles(int width, int height, string title) : base(width, height, title)
        {
        }
        Random rand = new Random();
        
        private void UpdateLocations()
        {
            
            for (int i = 0; i < 1000; i++)
            {
                //update all kinds of values
                int vertexIndex = rand.Next(0, (vertices.Length - 12) / 8) * 8 + 12;
                vertices[vertexIndex + 0] = (float)rand.NextDouble();//position1
                vertices[vertexIndex + 1] = (float)rand.NextDouble();//position2
                vertices[vertexIndex + 2] = (float)rand.NextDouble();//size1
                vertices[vertexIndex + 3] = (float)rand.NextDouble();//size2
                vertices[vertexIndex + 4] = (float)rand.NextDouble();//size3
                vertices[vertexIndex + 5] = (float)rand.NextDouble() * overlap / count;//opacity1
                vertices[vertexIndex + 6] = (float)rand.NextDouble() * overlap / count;//opacity2
                vertices[vertexIndex + 7] = (float)rand.NextDouble() * overlap / count;//opacity3
            }
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
        }
        private void ProcessingLoopFake()
        {
            var rand = new Random();
            Stopwatch _timer = new Stopwatch();
            _timer.Start();
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
        int count = 10000;
        float overlap = 60;
        protected override void OnLoad()
        {
            var verts = vertices.ToList();
            var rand = new Random();
            //for(int i = 0; i<140000; i++)
            
            for (int i = 0; i < count; i++)
            {
                verts.Add((float)rand.NextDouble());
                verts.Add((float)rand.NextDouble());
                verts.Add((float)rand.NextDouble());
                verts.Add((float)rand.NextDouble());
                verts.Add((float)rand.NextDouble());
                verts.Add((float)rand.NextDouble() * overlap / count);
                verts.Add((float)rand.NextDouble() * overlap / count);
                verts.Add((float)rand.NextDouble() * overlap / count);
            }
            vertices = verts.ToArray();

            FloatMapBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FloatMapBuffer);

            CheckGPUErrors("Error Loading Float Buffer");

            FloatMapTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, FloatMapTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);
            CheckGPUErrors("Error Loading Float Texture:");

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, FloatMapTexture, 0);

            CheckGPUErrors("Error Binding Float Texture to Buffer:");

            var status = GL.CheckNamedFramebufferStatus(FloatMapBuffer, FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
                Console.WriteLine("FBO not complete:" + status);

            CheckGPUErrors("Error completing frame buffer:");



            GL.ClearColor(0f, 0f, 0f, 0.0f);
            PointVertexArrayBuffer = GL.GenBuffer();//make triangle object

            shader = new PointShader(ClientSize, "CircleLayerShader.vert", "CircleShader.frag");
            texShader = new TextureShader("ScreenTriangle.vert", "Texture.frag");


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

            CheckGPUErrors("Error Loading before Float Buffer");//just in case

            //new Thread(ProcessingLoopFake).Start();
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
            DrawCircles();
            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }
        public void DrawCircles()
        {
            //draw opacities to float buffer
            shader.Use();
            CheckGPUErrors("Error using opacity shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FloatMapBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckGPUErrors("Error binding to opacity fbo:");

            GL.DrawElementsInstanced(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt ,(IntPtr)0,(vertices.Length - 12) / 8);

            CheckGPUErrors("Error rendering to float buffer:");

            //draw float buffer from texture to back buffer
            texShader.Use(FloatMapTexture);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0); 
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            CheckGPUErrors("Error rendering to back buffer:");
        }
        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(PointVertexArrayBuffer);
            GL.DeleteVertexArray(PointArrayObject);
            GL.DeleteFramebuffer(FloatMapBuffer);
            GL.DeleteTexture(FloatMapTexture);
            shader.Dispose();
            texShader.Dispose();
            base.OnUnload();
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            shader.ViewPortSize = ClientSize;
            base.OnResize(e);
        }
    }
}
