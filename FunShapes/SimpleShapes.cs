using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace FunShapes
{
    public class SimpleShapes : SimpleWindow
    {
        int PointVertexArrayBuffer;
        int PointArrayObject;
        int ElementBufferObject;

        int rows = 30;
        int columns = 10;

        PointShader shader;
        TextureStrip TexStrip;
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





        float[] corners = {
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
        float[] vertices = {
            .5f, 0,///0
            0, 1,//1
            1, 1,//2
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
            //0, 2, 3,    // second triangle
            //0, 3, 4,    // second triangle
            //0, 4, 5,    // second triangle
            //0, 5, 6,    // second triangle
            //0, 6, 7,    // second triangle
            //0, 7, 8,     
            //0, 8, 1,
        };
        public SimpleShapes(int width, int height, string title) : base(width, height, title)
        {
        }
        Random rand = new Random();
        
        private void UpdateLocations()
        {

            CheckGPUErrors("Error after last frame to back buffer:");
            for (int i = 0; i <squareCorners.Length; i++)
            {
                var time = _timer.ElapsedMilliseconds / 8000f;
                time = time % 1f;
                if (time <= .5f) 
                {
                    time = time * 2;
                    var transition = (float)MathHelper.Cos(2*Math.PI * time) / 2 + .5f;//one cycle every second
                    shader.OddRowHeightScale = new Vector3(1, MathHelper.Lerp(1, 0.8661f, transition), 1);
                    shader.OddRowOffset = new Vector3(MathHelper.Lerp(0, -1 / (columns - 1f), transition), 0, 0); 
                    corners[i] = MathHelper.Lerp(squareCorners[i], hexCorners[i], transition);
                }
                else
                {
                    time = time * 2-.5f;
                    var transition = (float)MathHelper.Cos(2*Math.PI * time) / 2 + .5f;//one cycle every second

                    shader.OddRowHeightScale = new Vector3(1, MathHelper.Lerp(0.8661f, 1, transition), 1);
                    shader.OddRowOffset = new Vector3(MathHelper.Lerp(-1 / (columns - 1f), 0, transition), 0, 0); 
                    corners[i] = MathHelper.Lerp(hexCorners[i], octCorners[i], transition);
                }
            }

            GL.Uniform3(shader.CornersLocation,squareCorners.Length/3, corners);
            CheckGPUErrors("Error updating location:");
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
            TexStrip = new TextureStrip("textureStrip.png", 96+2);//plus two yes. i make the textures just a pixel wider in the middle than they'll appear so that sampling never bleeds. stop gap? idk. hey me!, go into photoshop tomorrow and add one more pixel in the middle of each texture. thanks.
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
            shader.TextureFrame = new Vector2(TexStrip.TextureWidth/ (float)TexStrip.Width,1);
            shader.TextureOnePixelWidth = new Vector2(1/ (float)TexStrip.Width,0);


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
            GL.VertexAttribPointer(shader.TexCoordLocation, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.VertexAttribPointer(shader.PositionLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 6 * sizeof(float));
            GL.EnableVertexAttribArray(shader.PositionLocation);
            GL.EnableVertexAttribArray(shader.TexCoordLocation); 

            GL.VertexAttribDivisor(shader.PositionLocation, 1);//use from start to end, based on instance id instead of vertex index
            GL.VertexAttribDivisor(shader.TexCoordLocation, 0);//use from start to end, based on vertex index within instance

            ElementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, 3 * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            CheckGPUErrors("Error Loading before Float Buffer");//just in case

            
            shader.Use();
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
            
            CheckGPUErrors("Error last frame:");
            GL.Clear(ClearBufferMask.ColorBufferBit);
            DrawShapes();
            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }
        public void DrawShapes()
        {
            //draw opacities to float buffer
            shader.Use();
            var vertexCount = 3; ;// indices.Length;

            GL.Uniform1(shader.TextureIdLocation, 3);
            for (int i = 0; i < corners.Length / 3; i++)
            {
                GL.Uniform1(shader.CornerOffsetLocation, i);
                GL.DrawElementsInstanced(PrimitiveType.Triangles, vertexCount, DrawElementsType.UnsignedInt, (IntPtr)0, (vertices.Length - 6) / 5);
            }
            GL.Uniform1(shader.TextureIdLocation, 2);
            for (int i = 0; i < corners.Length / 3; i++)
            {
                GL.Uniform1(shader.CornerOffsetLocation, i);
                GL.DrawElementsInstanced(PrimitiveType.Triangles, vertexCount, DrawElementsType.UnsignedInt, (IntPtr)0, (vertices.Length - 6) / 5);
            }
            CheckGPUErrors("Error rendering shapes to back buffer:");
        }
        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(PointVertexArrayBuffer);
            GL.DeleteVertexArray(PointArrayObject);
            GL.DeleteBuffer(ElementBufferObject);

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
