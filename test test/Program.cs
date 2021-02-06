using System;
using System.Threading;

namespace test_test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //var game = new openTKCircleThin.SimpleTriangles(500, 500, "Learning OpenTK");
            //var game = new openTKCircleThin.SimpleCircles(500, 500, "Learning points in OpenTK");
            var game = new FunShapes.SimpleShapes(500, 500, "Learning fans in OpenTK");
            game.Run();

            //var gfx = new openTKCircleThin.CircleLayer();
            //Timer t = new Timer((a) => {
            //    gfx.addCircle(10, 20, 5, .5, 10000);
            //}, null, 0, 10000); 
            //Timer t = new Timer((a) => {
            //    gfx.addCircle(8, 15, 10, .7, 1000);
            //}, null, 0, 1000); 
            //Timer t = new Timer((a) => {
            //    gfx.addCircle(12, 17, 7, .2, 2500);
            //}, null, 0, 5000);
        }
    }
}
