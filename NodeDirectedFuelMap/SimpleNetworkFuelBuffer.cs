using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static NodeDirectedFuelMap.ManipulatePoints;

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

        BlurryCircleShader Step1CreateFuelRequestShader;
        MultiColorLineShader RenderLinesShader;
        SomeSubtractAndAddShader Step2TakeFuelShader;//take pool and request, regen pool?, subtract request from pool. 
        SomeMinAndDivideShader Step3PercentFuelTakenShader;//take request and pool, generate used amount from request and pool.
        SomeZeroingShader Step4FuelPoolZeroingShader;//take pool, set negatives to zero
        MaxMipMapShader Step5CalculateActivationsShader;//find the maximum activation level. well actually max activation optimized with max available fuel

        MultiViewShader texShader;
       
        ManipulatePoints points = new ManipulatePoints();
        ManipulateLines ManipulatedLines = new ManipulateLines();
        uint[] indices = {  // note that we start from 0!
            0, 1, 3,   // first triangle
            0, 2, 3    // second triangle
        };

        public SimpleNetworkFuelBuffer(int width, int height, string title) : base(width, height, title)
        {
            points.Allocate(count*100);
            ManipulatedLines.Allocate(count*9);
        }

        static int count = 10000;
        static float overlap = 9f;
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

            Step1CreateFuelRequestShader = new BlurryCircleShader(ClientSize, "1.DrawCircles\\CircleLayerShader.vert", "1.DrawCircles\\CircleShader.frag");
            Step2TakeFuelShader = new SomeSubtractAndAddShader("ScreenTriangle.vert", "2.subtract fuel\\SomeSubtractAndAddFrag.frag", FuelPoolTexture, FuelRequestTexture, .01f);
            Step3PercentFuelTakenShader = new SomeMinAndDivideShader("ScreenTriangle.vert", "3.fuel taken percent(create activation pool)\\SomeMinFrag.frag", FuelPoolTexture, FuelRequestTexture);
            Step4FuelPoolZeroingShader = new SomeZeroingShader("ScreenTriangle.vert", "4.remove negative fuel pool values\\SomeZeroingFrag.frag", FuelPoolTexture);
            Step5CalculateActivationsShader = new MaxMipMapShader("6.maximums\\MaxMipMap.compute", FuelPoolTexture, FuelRequestTexture);//TODO: this should actually be the growth potential texture, which is activation level * remainingfuel(fuelpool) where activation level = fuelused/fuelrequested*circleActivationEnergyLevel... but for testing this can be an already rendered image first to make sure the mipmap looks right
            CheckGPUErrors("Error initializing compute shader");//just in case
            RenderLinesShader = new MultiColorLineShader(ClientSize, "5.1.render lines\\LineLayerShader.vert", "5.1.render lines\\LineShader.frag");
            CheckGPUErrors("Error initializing line shader");//just in case
            texShader = new MultiViewShader("ScreenTriangle.vert", "5.2.render fuel textures\\MultiViewTexture.frag");

            CheckGPUErrors("Error initializing shader classes");//just in case
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

        
        RandomHelper86 Rand = new RandomHelper86();
        float[] randomValues = new float[50000 * 50];
        int[] randomInts = new int[2+4*50000];

        private void UpdateLocationRandomTestData()
        {
            try
            {
                int x = 0;
                int iIndex = 0;
                int fIndex = 0;
                //separating for profiling

                randomValues = Rand.Rand(50000 * 50);
                //Rand.RandDirect(randomValues);//booo
                //hi me. time to look at optimizing the point CRUD functions

                //separating out all the random integer casting for profiling and optimization
                var pointCount = points.PointCount;
                var lineCount = ManipulatedLines.LineCount;
                for (int i = 0; i < 2; i++)
                {
                    randomInts[iIndex++] = ((int)(randomValues[x++] * (pointCount - 1) + 1)) * 8 + ManipulatePoints.cornerSpace;//deactivate point. lowers pointcount by one
                }
                for (int i = 0; i < 50000; i++)
                {
                    randomInts[iIndex++] = ((int)(randomValues[x++] * (pointCount - 1) + 1)) * 8 + ManipulatePoints.cornerSpace;//deactivate. lowers point count by one
                    randomInts[iIndex++] = ((int)(randomValues[x++] * (pointCount - 1))) * 8 + ManipulatePoints.cornerSpace;//update. no effect on point count
                    randomInts[iIndex++] = ((int)(randomValues[x++] * (pointCount - 1))) * 8 + ManipulatePoints.cornerSpace;//update. no effect on point count
                    //activate points called here. increases point count by one
                }

                for (int i = 0; i < 5000; i++)
                { 
                    randomInts[iIndex++] = ((int)(randomValues[x++] * (pointCount - 2))) * 8+ ManipulatePoints.cornerSpace;
                }

                iIndex = 0;

                for (int i = 0; i < 2; i++)
                {


                    //cause inactive neurons to slowly fill up
                    points.DeactivatePoint(randomInts[iIndex++]);

                    points.AddPoint(
                        randomValues[x++] * 2 - .5f,//positionX
                        randomValues[x++] * 2 - .5f,//positionY
                        randomValues[x++],//size1
                        randomValues[x++],//size2
                        randomValues[x++],//size3
                        randomValues[x++] * overlap / count,//opacity1
                        randomValues[x++] * overlap / count,//opacity2                         
                        randomValues[x++] * overlap / count//opacity3);
                    );

                    //TODO: next step. lets keep lines around. adding them and removing them based on the currently active neurons. should be fast since we already eat the speed of adding and removing lines.
                }

                //if (points.InactiveNeurons.Count > 0)
                //{
                //    var neuronindex = (int)(randomValues[x++] * (points.InactiveNeurons.Count - 1));
                //    points.DeleteNeuron((Neuron)points.InactiveNeurons[neuronindex]);
                //}


                for (int i = 0; i < 50000; i++)
                {

                    //next cache all the random values first. then use them. so that we can profile. i bet the slow part is actually the calls to pointcount. its a math operation we're doing constantly.
                    //update all kinds of values

                    points.DeactivatePoint(randomInts[iIndex++]);

                    points.UpdatePoint(randomInts[iIndex++],
                        randomValues[x++],//size1
                        randomValues[x++],//size2
                        randomValues[x++],//size3
                        randomValues[x++] * overlap / count,//opacity1
                        randomValues[x++] * overlap / count,//opacity2                         
                        randomValues[x++] * overlap / count//opacity3
                    );

                    points.MovePoint(randomInts[iIndex++],
                        randomValues[x++] * 2 - .5f,//position1
                        randomValues[x++] * 2 - .5f//position2
                    );


                    points.ActivatePoint(points.InactiveNeurons.First().Value);

                }

                //add some lines to the neurons
                for (int i = 0; i < 3; i++)
                {
                    var a = points.ActiveNeurons[randomInts[iIndex++]];
                    var b = points.ActiveNeurons[randomInts[iIndex++]];
                    a.To.Add(b);
                }
                //remove some lines from the neurons to keep it even
                if (ManipulatedLines.LineCount > 50000)
                    for (int i = 0; i < 3;)
                        if (randomInts.Length > ++iIndex && points.ActiveNeurons[randomInts[iIndex]].To.Any())
                        {
                            points.ActiveNeurons[randomInts[iIndex]].To.RemoveAt(0);
                            i++;
                        }

                //set all the lines
                var g = ManipulatedLines.LineCount;
                int j = 0;
                for (int i = 0; i < points.ActiveNeurons.Count(); i++)
                {
                    if (points.ActiveNeurons[i] == null) continue;
                    for (int h = 0; h < points.ActiveNeurons[i].To.Count(); h++)
                    {
                        if (j < g)
                            ManipulatedLines.UpdateLine(j, points.ActiveNeurons[i].pointIndex, points.ActiveNeurons[i].To[h].pointIndex);//oops what if the point it points to isnt active, that wont make much sense. i think i need to cache ALL the point's locations, active or not on the gfx card, then just update the active list with its extra data often. or could all the points go on the gfx card? if 1 million of them would fit on it no problem in a single managable array then i'm game
                        else
                            ManipulatedLines.AddLine(points.ActiveNeurons[i].pointIndex, points.ActiveNeurons[i].To[h].pointIndex);
                        j++;
                    }
                }

                //clear remaining old lines
                g = ManipulatedLines.LineCount;
                for (; j < g; g--)
                    ManipulatedLines.RemoveLine(g);


            }
            catch (Exception ex)
            {
                ;
            }
            //update point data set
            GL.BindBuffer(BufferTarget.ArrayBuffer, PointVertexArrayBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, points.allocatedSpace * sizeof(float), points.points, BufferUsageHint.DynamicDraw);

            //update line data set
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
            Console.WriteLine($"Frame time: {args.Time * 1000:0.000} ms"); 
            UpdateLocationRandomTestData();
            base.OnUpdateFrame(args);
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            
            GL.Clear(ClearBufferMask.ColorBufferBit);
            RequestFuel();
            RenderNodeLines();
            RenderInstrumentation();

            //if(Math.Abs(FuelZeroingShader.Average - .5f) > threshold)
            RefuelRate = (.5f - Step4FuelPoolZeroingShader.Average)/1.1f;

            Step2TakeFuelShader.AddValue = RefuelRate;

            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }
        /// <summary>
        /// one frame essentially of the neural net
        /// what's it doing shader wise? well basically...
        /// -CreateFuelRequestShader - drawing energy requirements of newly activated neurons
        /// -RequestFuelShader - subtracting them from a pool of "fuel" and refilling the pool at an adaptive rate
        /// -FuelUsedShader - set intermediate texture to some 0-1 values of what percent of requested fuel neurons should receive
        /// -FuelZeroingShader - cut off anything in the fuel pool texture that is above or below 0-1, keeping it in bounds
        /// </summary>
        public void RequestFuel()
        {

            // 3. then set our vertex attributes pointers
            //which attribute are we settings, how many is it, what is each it?, should it be normalized?, what's the total size?,
            //TODO: i may not want to normalize

            GL.VertexAttribPointer(Step1CreateFuelRequestShader.PositionLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 12 * sizeof(float));
            GL.EnableVertexAttribArray(Step1CreateFuelRequestShader.PositionLocation);
            GL.VertexAttribPointer(Step1CreateFuelRequestShader.SizeLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 14 * sizeof(float));
            GL.EnableVertexAttribArray(Step1CreateFuelRequestShader.SizeLocation);
            GL.VertexAttribPointer(Step1CreateFuelRequestShader.OpacityLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 17 * sizeof(float));
            GL.EnableVertexAttribArray(Step1CreateFuelRequestShader.OpacityLocation);
            GL.VertexAttribPointer(Step1CreateFuelRequestShader.SquareCornerLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(Step1CreateFuelRequestShader.SquareCornerLocation);

            GL.VertexAttribDivisor(Step1CreateFuelRequestShader.PositionLocation, 1);//use from start to end, based on instance id instead of vertex index
            GL.VertexAttribDivisor(Step1CreateFuelRequestShader.SizeLocation, 1);
            GL.VertexAttribDivisor(Step1CreateFuelRequestShader.OpacityLocation, 1);
            GL.VertexAttribDivisor(Step1CreateFuelRequestShader.SquareCornerLocation, 0);//use from start to end, based on vertex index within instance
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, TwoTriangleElementBuffer);

            //PoolMinMax[1] = FuelZeroingShader.Average;

            //create request pool buffer
            //-already handled by updateLocations loop
            //-draw opacities to request pool buffer
            //generate requested fuel usage buffer. so many circles
            Step1CreateFuelRequestShader.Use();
            CheckGPUErrors("Error using opacity shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelRequestBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawElementsInstanced(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt ,(IntPtr)0,points.PointCount);
            //hi me. pull up renderdoc and diagnose this stupid outofmemory exception!
            CheckGPUErrors("Error rendering to activation request float buffer:");


            //process fuel pool regen and request subtraction
            //subtract from fuel pool
            Step2TakeFuelShader.Use();
            CheckGPUErrors("Error using addsubtract shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelPoolBuffer);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            CheckGPUErrors("Error rendering to subtract from fuel pool float buffer:");

            //zero out negative fuel areas(in the future we may divide remaining fuel by the negative values or apply a division to the strength of any neurons in the negative vicinity if we aren't already, but for now
            Step3PercentFuelTakenShader.Use();
            CheckGPUErrors("Error using min shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelUsedBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            CheckGPUErrors("Error rendering to divide from fuel pool and fuel request float buffer:");

            //draw negative areas of fuel usage(overdrawing from the pool). this buffer is an intermediate for dividing activation energy in the next frame by negative valued area's deficit
            Step4FuelPoolZeroingShader.Use();
            CheckGPUErrors("Error using zeroing shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FuelPoolBuffer);
            CheckGPUErrors("Error binding to opacity fbo:");
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            CheckGPUErrors("Error rendering to min fuel pool float buffer:");

            //get average value in the fuel pool, used to set refill rate for next frame
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, TwoTriangleElementBuffer);
            Step4FuelPoolZeroingShader.Use();
            Step4FuelPoolZeroingShader.CheckAverage(ClientSize.X, ClientSize.Y);

            //gets the level of activation to be added for the next pass's activation propogation
            var maximumValues = Step5CalculateActivationsShader.Use();//later this will be used for new neuron creation
            CheckGPUErrors("Error rendering to activation buffer:");

            ////get the highest points in the activation pool. should it be the highest or random high ones? i prefer less random, so lets go highest and see if it doesn't backfire completely
            //Step6FindMaximumsShader.Use();
            //CheckGPUErrors("Error using maximums compute shader:");
            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, ActivationBuffer);//finding maximums of this, the activation buffer... fuel used isnt activation... its fuel used. activation is another blurry circle buffer i haven't made yet...
            //GL.Clear(ClearBufferMask.ColorBufferBit);//clear 
            //CheckGPUErrors("Error binding to opacity fbo:");
            //GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            //CheckGPUErrors("Error finding maximums:");
            
            //Step7FindMaximumSourcesShader.Use();
            //CheckGPUErrors("Error using maximums compute shader:");
            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, ActivationBuffer);
            //GL.Clear(ClearBufferMask.ColorBufferBit);//clear 
            //CheckGPUErrors("Error binding to opacity fbo:");
            //GL.DrawArrays(PrimitiveType.Triangles, 0, 3);//should iterate every frag/pixel
            //CheckGPUErrors("Error finding maximums:");

            //two more steps.
            //8. create new neurons from maximums. kind of already handled
            //9. go from current neurons to child neurons. including both activated energy and activation energy buffer (paren'ts energy/default level)*requestfulfilledpercentage*default activation level+activation energy buffer)
            //    -resolve neuron's child connections with some fancy math(fast math is fine for now)
            //    -should activation level radiation be subtracted from action energy overall? make some falloff function for this if it starts overflowing
            //10. rinse and repeat
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
            //process active connection array.
            RenderLinesShader.Use();
            CheckGPUErrors("Error using lines shader:");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, LinesBuffer);
            CheckGPUErrors("Error binding to lines fbo:");
            GL.DrawElements(BeginMode.Lines, ManipulatedLines.lines.Length, DrawElementsType.UnsignedInt, 0);
            CheckGPUErrors("Error rendering to line buffer:");
        }


        public RectangleF FuelRequestBounds = new RectangleF(.01f, .34f, .32f, .32f);
        public RectangleF FuelUsedBounds = new RectangleF(.34f, .34f, .32f, .32f);
        public RectangleF MipMapBounds = new RectangleF(.67f, .34f, .16f, .32f);//half width
        public RectangleF PoolBounds = new RectangleF(.01f, .01f, .32f, .32f);
        public RectangleF LinesBounds = new RectangleF(.34f, .01f, .32f, .32f);

        public float[] FuelRequestMinMax = new float[2] { 0, overlap * overlap / count * 6 };
        public float[] FuelUsedMinMax = new float[2] { 0, 1 };
        public float[] MipMapMinMax = new float[2] { 0, 1 };
        public float[] PoolMinMax = new float[2] { 0, 1 };
        public float[] LinesMinMax = new float[2] { 0, 1 };

        public void RenderInstrumentation()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, TwoTriangleElementBuffer);
            //draw float buffer from texture to back buffer. one call for each one we'd like to display
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            texShader.Use(FuelRequestTexture, FuelRequestBounds, FuelRequestMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(FuelUsedTexture, FuelUsedBounds, FuelUsedMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(Step5CalculateActivationsShader.MipMapImageHandle, MipMapBounds, MipMapMinMax);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            texShader.Use(FuelPoolTexture, PoolBounds, PoolMinMax);
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
            Step1CreateFuelRequestShader.Dispose();
            Step5CalculateActivationsShader.Dispose();
            texShader.Dispose();
            base.OnUnload();
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            Step1CreateFuelRequestShader.ViewPortSize = ClientSize;

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
