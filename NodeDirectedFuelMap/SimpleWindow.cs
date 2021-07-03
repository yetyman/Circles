using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace NodeDirectedFuelMap
{
    public class SimpleWindow : GameWindow
    {

        private static DebugProc openGLDebugDelegate;


        public SimpleWindow(int width, int height, string title) 
            : base(
                  new GameWindowSettings() { RenderFrequency = 60 }, 
                  new NativeWindowSettings() { 
                      Size = new OpenTK.Mathematics.Vector2i(width, height), 
                      Title = title 
                  }) {

            //setupDebugOutput();

        }
        protected override void OnClosing(CancelEventArgs e)
        {
            //openGLDebugCallback(DebugSource.DontCare, DebugType.DontCare, 0, DebugSeverity.DontCare, 0, IntPtr.Zero, IntPtr.Zero);
            base.OnClosing(e);
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


        protected void CheckGPUErrors(string errorPrefix)
        {
            ErrorCode err;

            while ((err = GL.GetError()) != ErrorCode.NoError)
            {
                // Process/log the error.
                Console.WriteLine(errorPrefix + err);
            }
        }


        private void setupDebugOutput()
        {
#if !DEBUG
            return;
#endif
            Console.WriteLine("\nenabling openGL debug output");

            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            CheckGPUErrors("enabling debug output");//just in case

            openGLDebugDelegate = openGLDebugCallback;

            GL.DebugMessageCallback(openGLDebugDelegate, IntPtr.Zero);
            GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[0], true);
            CheckGPUErrors("setting up debug output");

            GL.DebugMessageInsert(DebugSourceExternal.DebugSourceApplication, DebugType.DebugTypeMarker, 0, DebugSeverity.DebugSeverityNotification, -1, "Debug output enabled");
            CheckGPUErrors("testing debug output");
        }
        private static void openGLDebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            if (id == 0 && length == 0)
                Console.WriteLine("ending console application");
            else
            {
                Console.WriteLine(source == DebugSource.DebugSourceApplication ?
                    $"openGL - {Marshal.PtrToStringAnsi(message, length)}" :
                    $"openGL - {Marshal.PtrToStringAnsi(message, length)}\n\tid:{id} severity:{severity} type:{type} source:{source}\n");
            }
        }

    }
}
