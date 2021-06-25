using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeDirectedFuelMap
{
    public class SimpleNetworkFuelBuffer : SimpleWindow
    {
        int PointVertexArrayBuffer;
        int PointArrayObject;
        int TwoTriangleElementBuffer;//squares
        int LineIndexesElementBuffer;//line indexes

        int FuelPoolBuffer;
        int FuelRequestBuffer;
        int FuelUsedBuffer;
        int LinesBuffer;
        int FuelPoolTexture;
        int FuelRequestTexture;
        int FuelUsedTexture;
        int LinesTexture;
        BlurryCircleShader CreateFuelRequestShader;
        MultiColorLineShader RenderLinesShader;
        SomeSubtractAndAddShader RequestFuelShader;//take pool and request, regen pool?, subtract request from pool. 
        SomeMinAndDivideShader FuelUsedShader;//take request and pool, generate used amount from request and pool.
        SomeZeroingShader FuelZeroingShader;//take pool, set negatives to zero

        MultiViewShader texShader;

        ManipulatePoints points = new ManipulatePoints();
        ManipulateLines ManipulatedLines = new ManipulateLines();
        uint[] indices = {  // note that we start from 0!
            0, 1, 3,   // first triangle
            0, 2, 3    // second triangle
        };

        public SimpleNetworkFuelBuffer(int width, int height, string title) : base(width, height, title)
        {
            points.Allocate(count);
            ManipulatedLines.Allocate(count*9);
        }

        private int threadNo = 0;
        object lockObj = new object();
        Dictionary<Random, bool> RandPool = new Dictionary<Random, bool>();
        static int count = 10000;
        static float overlap = 6;
        static float threshold = .2f;
        static float RefuelRate = .01f;
        protected override void OnLoad()
        {
            var rand = new Random();
            //for(int i = 0; i<140000; i++)
            
            for (int i = 0; i < count; i++)
            {
                points.AddPoint(
                    (float)rand.NextDouble() * 2 - .5f,
                    (float)rand.NextDouble() * 2 - .5f,
                    (float)rand.NextDouble(),
                    (float)rand.NextDouble(),
                    (float)rand.NextDouble(),
                    (float)rand.NextDouble() * overlap / count,
                    (float)rand.NextDouble() * overlap / count,
                    (float)rand.NextDouble() * overlap / count
                );

            }
            for (int i = 0; i < count; i++)
            {
                //just guesstimating an average of 9 children per node. completely random
                for(int x = 0; x < 9; x++)
                    ManipulatedLines.AddLine(
                        rand.Next(0, points.PointCount),
                        rand.Next(0, points.PointCount)
                    );
            }

            GL.ClearColor(0f, 0f, 0f, 0.0f);
            
            FuelUsedBuffer = InitializeFrameBuffer();
            FuelUsedTexture = InitializeFloatFrameBufferTexture();
            CheckFramebufferStatus(FuelUsedBuffer);

            FuelPoolBuffer = InitializeFrameBuffer();
            FuelPoolTexture = InitializeFloatFrameBufferTexture();

            GL.ClearColor(1f, 0f, 0f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckFramebufferStatus(FuelPoolBuffer);
            GL.ClearColor(0f, 0f, 0f, 0.0f);

            FuelRequestBuffer = InitializeFrameBuffer();
            FuelRequestTexture = InitializeFloatFrameBufferTexture();
            CheckFramebufferStatus(FuelRequestBuffer);

            LinesBuffer = InitializeFrameBuffer();
            LinesTexture = InitializeRGBAFrameBufferTexture();
            CheckFramebufferStatus(LinesBuffer);


            PointVertexArrayBuffer = GL.GenBuffer();//make triangle object

            CreateFuelRequestShader = new BlurryCircleShader(ClientSize, "CircleLayerShader.vert", "CircleShader.frag");
            RequestFuelShader = new SomeSubtractAndAddShader("ScreenTriangle.vert", "SomeSubtractAndAddFrag.frag", FuelPoolTexture, FuelRequestTexture, .01f);
            FuelUsedShader = new SomeMinAndDivideShader("ScreenTriangle.vert", "SomeMinFrag.frag", FuelPoolTexture, FuelRequestTexture);
            FuelZeroingShader = new SomeZeroingShader("ScreenTriangle.vert", "SomeZeroingFrag.frag", FuelPoolTexture);
            RenderLinesShader = new MultiColorLineShader(ClientSize, "LineLayerShader.vert", "LineShader.frag");
            texShader = new MultiViewShader("ScreenTriangle.vert", "MultiViewTexture.frag");

            //TODO: setup line buffer data arrays, vertex shaders, and line shaders

            PointArrayObject = GL.GenVertexArray();

            // ..:: Initialization code (done once (unless your object frequently changes)) :: ..
            // 1. bind Vertex Array Object
            GL.BindVertexArray(PointArrayObject);

            // 2. copy our vertices array in a buffer for OpenGL to use
            GL.BindBuffer(BufferTarget.ArrayBuffer, PointVertexArrayBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, points.allocatedSpace * sizeof(float), points.points, BufferUsageHint.DynamicDraw);

            TwoTriangleElementBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, TwoTriangleElementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            
            LineIndexesElementBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, LineIndexesElementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, ManipulatedLines.LineCount*2 * sizeof(uint), ManipulatedLines.lines, BufferUsageHint.DynamicDraw);
            
            CheckGPUErrors("Error Loading before Float Buffer");//just in case

            //new Thread(ProcessingLoopFake).Start();
            base.OnLoad();
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
                Thread.Sleep(Math.Max((int)(frameTime - (timeEnd - timeStart)), 0));//roughly sync updates to frame speed adjusting for this thread's processing time
            }

        }
        private void UpdateLocations()
        {
            //Console.WriteLine("frame");
            var loop = Parallel.For(0, 4, (index) =>
            {
                try
                {
                    Random rand = null;
                    lock (RandPool)
                    {
                        rand = RandPool.FirstOrDefault(x => !x.Value).Key;
                        if (rand == null) RandPool.Add(rand = new Random(), true);
                        RandPool[rand] = true;
                    }
                    if (string.IsNullOrWhiteSpace(Thread.CurrentThread.Name))
                    {
                        lock (lockObj)
                            Thread.CurrentThread.Name = "Thread " + threadNo++;
                    }
                    for (int i = 0; i < 250; i++)
                    {
                        //update all kinds of values

                        int vertexIndex = rand.Next(2, points.PointCount) * 8;
                        points.RemovePoint(vertexIndex);

                        vertexIndex = rand.Next(0, points.PointCount) * 8;
                        points.UpdatePoint(vertexIndex,
                            (float)rand.NextDouble() * 2 - .5f,//position1
                            (float)rand.NextDouble() * 2 - .5f,//position2
                            (float)rand.NextDouble(),//size1
                            (float)rand.NextDouble(),//size2
                            (float)rand.NextDouble(),//size3
                            (float)rand.NextDouble() * overlap / count,//opacity1
                            (float)rand.NextDouble() * overlap / count,//opacity2                         
                            (float)rand.NextDouble() * overlap / count//opacity3
                        );

                        vertexIndex = rand.Next(0, points.PointCount) * 8;
                        points.AddPoint(
                            (float)rand.NextDouble() * 2 - .5f,//position1
                            (float)rand.NextDouble() * 2 - .5f,//position2
                            (float)rand.NextDouble(),//size1
                            (float)rand.NextDouble(),//size2
                            (float)rand.NextDouble(),//size3
                            (float)rand.NextDouble() * overlap / count,//opacity1
                            (float)rand.NextDouble() * overlap / count,//opacity2                         
                            (float)rand.NextDouble() * overlap / count//opacity3);
                        );



                        for (int x = 0; x < 9; x++)
                        {
                            int lineIndex = rand.Next(0, ManipulatedLines.LineCount) * 2;
                            ManipulatedLines.RemoveLine(lineIndex);
                            ManipulatedLines.AddLine(
                                rand.Next(0, points.PointCount),
                                rand.Next(0, points.PointCount)
                            );
                        }
                    }

                    lock (RandPool)
                    {
                        RandPool[rand] = false;
                    }
                }
                catch (Exception ex)
                {
                    ;
                }
            });
            while (!loop.IsCompleted) ;
            //GL.BufferSubData //eventually.
            GL.BufferData(BufferTarget.ArrayBuffer, points.allocatedSpace * sizeof(float), points.points, BufferUsageHint.DynamicDraw);


            GL.BindBuffer(BufferTarget.ElementArrayBuffer, LineIndexesElementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, ManipulatedLines.LineCount * 2 * sizeof(uint), ManipulatedLines.lines, BufferUsageHint.DynamicDraw);
        }
        private void CheckFramebufferStatus(int requestBuffer)
        {
            var status = GL.CheckNamedFramebufferStatus(requestBuffer, FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
                Console.WriteLine("FBO not complete:" + status);

            CheckGPUErrors("Error completing frame buffer:");
        }

        private int InitializeFloatFrameBufferTexture()
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

        private int InitializeRGBAFrameBufferTexture()
        {
            var requestTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, requestTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
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

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            UpdateLocations();
            base.OnUpdateFrame(args);
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            
            GL.Clear(ClearBufferMask.ColorBufferBit);
            RequestFuel();
            RenderNodeLines();
            RenderInstrumentation();

            //if(Math.Abs(FuelZeroingShader.Average - .5f) > threshold)
            RefuelRate = (.5f - FuelZeroingShader.Average)/1.1f;

            RequestFuelShader.AddValue = RefuelRate;

            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }
        public RectangleF PoolBounds = new RectangleF(.01f, .01f, .48f, .48f);
        public RectangleF FuelRequestBounds = new RectangleF(.01f, .51f, .48f, .48f);
        public RectangleF FuelUsedBounds = new RectangleF(.51f, .51f, .48f, .48f);
        public RectangleF LinesBounds = new RectangleF(.51f, .01f, .48f, .48f);

        public float[] PoolMinMax = new float[2] { 0, 1 };
        public float[] FuelRequestMinMax = new float[2] { 0, overlap*overlap/count*6};
        public float[] FuelUsedMinMax = new float[2] { 0, 1 };
        public float[] LinesMinMax = new float[2] { 0, 1 };
        public void RequestFuel()
        {

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



            GL.BindBuffer(BufferTarget.ElementArrayBuffer, TwoTriangleElementBuffer);
            FuelZeroingShader.Use();
            FuelZeroingShader.CheckAverage(ClientSize.X, ClientSize.Y);
            //PoolMinMax[1] = FuelZeroingShader.Average;

            //create request pool buffer
            //-already handled by updateLocations loop
            //-draw opacities to request pool buffer
            CreateFuelRequestShader.Use();
            CheckGPUErrors("Error using opacity shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelRequestBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawElementsInstanced(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt ,(IntPtr)0,points.PointCount);
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

        }
        public void RenderNodeLines()
        {


            // 3. then set our vertex attributes pointers
            //which attribute are we settings, how many is it, what is each it?, should it be normalized?, what's the total size?,
            //TODO: i may not want to normalize

            GL.VertexAttribPointer(RenderLinesShader.PositionLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 12 * sizeof(float));
            GL.EnableVertexAttribArray(RenderLinesShader.PositionLocation);

            GL.VertexAttribDivisor(RenderLinesShader.PositionLocation, 0);//use from start to end, based on instance id instead of vertex index



            GL.BindBuffer(BufferTarget.ElementArrayBuffer, LineIndexesElementBuffer);
            //process fuel pool regen and request subtraction
            RenderLinesShader.Use();
            CheckGPUErrors("Error using lines shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, LinesBuffer);
            CheckGPUErrors("Error binding to lines fbo:");
            GL.DrawElements(BeginMode.Lines, ManipulatedLines.lines.Length, DrawElementsType.UnsignedInt, 0);
            CheckGPUErrors("Error rendering to line buffer:");
        }

        public void RenderInstrumentation()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, TwoTriangleElementBuffer);
            //draw float buffer from texture to back buffer. one call for each one we'd like to display
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            texShader.Use(FuelPoolTexture, PoolBounds, PoolMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(FuelRequestTexture, FuelRequestBounds, FuelRequestMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(FuelUsedTexture, FuelUsedBounds, FuelUsedMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(LinesTexture, LinesBounds, LinesMinMax);
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
            GL.DeleteTexture(LinesTexture);
            CreateFuelRequestShader.Dispose();
            texShader.Dispose();
            base.OnUnload();
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            CreateFuelRequestShader.ViewPortSize = ClientSize;

            //create 2D tecture for fuel remaining projection
            GL.BindTexture(TextureTarget.Texture2D, FuelPoolTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);

            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelPoolBuffer);
            //GL.ClearColor(1f, 1f, 1f, 1f);
            //GL.Clear(ClearBufferMask.ColorBufferBit);
            //GL.ClearColor(0f, 0f, 0f, 0.0f);
            //CheckGPUErrors("Error resetting fuel pool:");

            //create 2D tecture for fuel requested projection
            GL.BindTexture(TextureTarget.Texture2D, FuelRequestTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            
            //create 2D texture for fuel used projection
            GL.BindTexture(TextureTarget.Texture2D, FuelUsedTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            
            //create 2D texture for lines to be rendered onto
            GL.BindTexture(TextureTarget.Texture2D, LinesTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, ClientSize.X, ClientSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

            base.OnResize(e);
        }
    }
}
