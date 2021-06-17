using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Text;

namespace NodeDirectedFuelMap
{
    public class SimpleWindow : GameWindow
    {
        public SimpleWindow(int width, int height, string title) 
            : base(
                  new GameWindowSettings() { RenderFrequency = 60 }, 
                  new NativeWindowSettings() { 
                      Size = new OpenTK.Mathematics.Vector2i(width, height), 
                      Title = title 
                  }) { 

        }
        protected override void OnLoad()
        {
            int nrAttributes = 0;
            GL.GetInteger(GetPName.MaxVertexAttribs, out nrAttributes);
            Console.WriteLine("Maximum number of vertex attributes supported: " + nrAttributes);
            base.OnLoad();
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height);
            base.OnResize(e);
        }


    }
}
