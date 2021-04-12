using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace FuelMap
{
    public class SimpleFuelBuffer : SimpleWindow
    {
        int PointVertexArrayBuffer;
        int PointArrayObject;
        int TwoTriangleElementBuffer;//squares

        int FuelPoolBuffer;
        int FuelRequestBuffer;
        int FuelUsedBuffer;
        int FuelPoolTexture;
        int FuelRequestTexture;
        int FuelUsedTexture;
        BlurryCircleShader CreateFuelRequestShader;
        SomeSubtractAndAddShader RequestFuelShader;//take pool and request, regen pool?, subtract request from pool. 
        SomeMinAndDivideShader FuelUsedShader;//take request and pool, generate used amount from request and pool.
        SomeZeroingShader FuelZeroingShader;//take pool, set negatives to zero
        MultiViewShader texShader;
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
        public SimpleFuelBuffer(int width, int height, string title) : base(width, height, title)
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
        static int count = 10000;
        static float overlap = 6;
        static float threshold = .1f;
        static float RefuelRate = .01f;
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

            GL.ClearColor(0f, 0f, 0f, 0.0f);
            
            FuelUsedBuffer = InitializeFrameBuffer();
            FuelUsedTexture = InitializeFrameBufferTexture();
            CheckFramebufferStatus(FuelUsedBuffer);

            FuelPoolBuffer = InitializeFrameBuffer();
            FuelPoolTexture = InitializeFrameBufferTexture();

            GL.ClearColor(1f, 1f, 1f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckFramebufferStatus(FuelPoolBuffer);
            GL.ClearColor(0f, 0f, 0f, 0.0f);

            FuelRequestBuffer = InitializeFrameBuffer();
            FuelRequestTexture = InitializeFrameBufferTexture();
            CheckFramebufferStatus(FuelRequestBuffer);


            PointVertexArrayBuffer = GL.GenBuffer();//make triangle object

            CreateFuelRequestShader = new BlurryCircleShader(ClientSize, "CircleLayerShader.vert", "CircleShader.frag");
            RequestFuelShader = new SomeSubtractAndAddShader("ScreenTriangle.vert", "SomeSubtractAndAddFrag.frag", FuelPoolTexture, FuelRequestTexture, .01f);
            FuelUsedShader = new SomeMinAndDivideShader("ScreenTriangle.vert", "SomeMinFrag.frag", FuelPoolTexture, FuelRequestTexture);
            FuelZeroingShader = new SomeZeroingShader("ScreenTriangle.vert", "SomeZeroingFrag.frag", FuelPoolTexture);
            texShader = new MultiViewShader("ScreenTriangle.vert", "MultiViewTexture.frag");


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
            GL.VertexAttribPointer(CreateFuelRequestShader.PositionLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 12 * sizeof(float));
            GL.EnableVertexAttribArray(CreateFuelRequestShader.PositionLocation);
            GL.VertexAttribPointer(CreateFuelRequestShader.SizeLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 14 * sizeof(float));
            GL.EnableVertexAttribArray(CreateFuelRequestShader.SizeLocation);
            GL.VertexAttribPointer(CreateFuelRequestShader.OpacityLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 17 * sizeof(float));
            GL.EnableVertexAttribArray(CreateFuelRequestShader.OpacityLocation);
            GL.VertexAttribPointer(CreateFuelRequestShader.SquareCornerLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(CreateFuelRequestShader.SquareCornerLocation); 

            GL.VertexAttribDivisor(CreateFuelRequestShader.PositionLocation, 1);//use from start to end, based on instance id instead of vertex index
            GL.VertexAttribDivisor(CreateFuelRequestShader.SizeLocation, 1);
            GL.VertexAttribDivisor(CreateFuelRequestShader.OpacityLocation, 1);
            GL.VertexAttribDivisor(CreateFuelRequestShader.SquareCornerLocation, 0);//use from start to end, based on vertex index within instance


            TwoTriangleElementBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, TwoTriangleElementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            CheckGPUErrors("Error Loading before Float Buffer");//just in case

            //new Thread(ProcessingLoopFake).Start();
            base.OnLoad();
        }

        private void CheckFramebufferStatus(int requestBuffer)
        {
            var status = GL.CheckNamedFramebufferStatus(requestBuffer, FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
                Console.WriteLine("FBO not complete:" + status);

            CheckGPUErrors("Error completing frame buffer:");
        }

        private int InitializeFrameBufferTexture()
        {
            var requestTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, requestTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);
            CheckGPUErrors("Error Loading Float Texture:");

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, requestTexture, 0);

            CheckGPUErrors("Error Binding Float Texture to Buffer:");
            return requestTexture;
        }

        private int InitializeFrameBuffer()
        {
            var requestBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, requestBuffer);

            CheckGPUErrors("Error Loading Float Buffer");
            return requestBuffer;
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
            RequestFuel();

            if(FuelZeroingShader.Average < .5f - threshold)
                RefuelRate += .001f;
            else if (FuelZeroingShader.Average > .5f + threshold)
                RefuelRate -= .001f;
            RequestFuelShader.AddValue = RefuelRate;

            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }
        public RectangleF PoolBounds = new RectangleF(.01f, .01f, .48f, .48f);
        public RectangleF FuelRequestBounds = new RectangleF(.01f, .51f, .48f, .48f);
        public RectangleF FuelUsedBounds = new RectangleF(.51f, .26f, .48f, .48f);

        public float[] PoolMinMax = new float[2] { 0, 1 };
        public float[] FuelRequestMinMax = new float[2] { 0, 150*overlap / count };
        public float[] FuelUsedMinMax = new float[2] { 0, 1 };
        public void RequestFuel()
        {
            //create request pool buffer
            //-already handled by updateLocations loop
            //-draw opacities to request pool buffer
            CreateFuelRequestShader.Use();
            CheckGPUErrors("Error using opacity shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelRequestBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawElementsInstanced(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt ,(IntPtr)0,(vertices.Length - 12) / 8);
            CheckGPUErrors("Error rendering to activation request float buffer:");

            //process fuel pool regen and request subtraction
            RequestFuelShader.Use();
            CheckGPUErrors("Error using addsubtract shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelPoolBuffer);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            CheckGPUErrors("Error rendering to subtract from fuel pool float buffer:");

            //process fuel pool zeroing and produce used fuel buffer.
            FuelUsedShader.Use(RefuelRate);
            CheckGPUErrors("Error using min shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelUsedBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            CheckGPUErrors("Error rendering to divide from fuel pool and fuel request float buffer:");

            //process fuel pool regen and request subtraction
            FuelZeroingShader.Use();
            CheckGPUErrors("Error using zeroing shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelPoolBuffer);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            CheckGPUErrors("Error rendering to min fuel pool float buffer:");

            FuelZeroingShader.CheckAverage(ClientSize.X, ClientSize.Y);

            //draw float buffer from texture to back buffer. one call for each one we'd like to display
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            texShader.Use(FuelPoolTexture, PoolBounds, PoolMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(FuelRequestTexture, FuelRequestBounds, FuelRequestMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(FuelUsedTexture, FuelUsedBounds, FuelUsedMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);


            CheckGPUErrors("Error rendering to back buffer:");
        }
        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(PointVertexArrayBuffer);
            GL.DeleteVertexArray(PointArrayObject);
            GL.DeleteFramebuffer(FuelRequestBuffer);
            GL.DeleteFramebuffer(FuelUsedBuffer);
            GL.DeleteFramebuffer(FuelPoolBuffer);
            GL.DeleteTexture(FuelRequestTexture);
            CreateFuelRequestShader.Dispose();
            texShader.Dispose();
            base.OnUnload();
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            CreateFuelRequestShader.ViewPortSize = ClientSize;
            GL.BindTexture(TextureTarget.Texture2D, FuelPoolTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);

            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelPoolBuffer);
            //GL.ClearColor(1f, 1f, 1f, 1f);
            //GL.Clear(ClearBufferMask.ColorBufferBit);
            //GL.ClearColor(0f, 0f, 0f, 0.0f);
            //CheckGPUErrors("Error resetting fuel pool:");

            GL.BindTexture(TextureTarget.Texture2D, FuelRequestTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, FuelUsedTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);

            base.OnResize(e);
        }
    }
}
