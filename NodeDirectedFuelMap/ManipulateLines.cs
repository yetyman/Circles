using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NodeDirectedFuelMap
{
    public class ManipulateLines
    {
        //i believe that most functionality here may eventually be parallized into a shader, compute shader or otherwise.
        //for now, for clarity, i'll use c# and update the buffered data as i have been, 
        //this will like be the first bottleneck to overcome as the graph becomes functional

        public int[] lines;
        public int firstOpenSpace = 0;
        public object firstOpenLock = new object();

        public const int lineSize = 2;
        public const int bufferSpace = 0;
        public int allocatedSpace => lines.Length;
        public int LineCount => (firstOpenSpace - bufferSpace - 1) / lineSize;
        /// <summary>
        /// allocate total possible memory from the get go for all lines
        /// </summary>
        /// <param name="maxLineCount"></param>
        public void Allocate(int maxLineCount)
        {
            lock (firstOpenLock)
            {
                //include the corners array in buffered data
                var actualLength = maxLineCount * lineSize;

                lines = new int[actualLength];

                firstOpenSpace = bufferSpace;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="r1">impulse radiation radius</param>
        /// <param name="r2">fuel usage radiation radius</param>
        /// <param name="r3">node creation radiation radius</param>
        /// <param name="o1">impulse intensity</param>
        /// <param name="o2">fuel usage intensity</param>
        /// <param name="o3">node creation intensity</param>
        public void AddLine(int pointIndex1, int pointIndex2)
        {
            int lp = firstOpenSpace;
            firstOpenSpace += lineSize;

            lines[lp + 0] = pointIndex1;//position1
            lines[lp + 1] = pointIndex2;//position2

        }

        /// <summary>
        /// rare, but occasionally done. given that this is presumed rare, it might also be a good place to consolidate memory space
        /// </summary>
        /// <param name="index"></param>
        public void RemoveLine(int index)
        {
            index += bufferSpace;

            lines[index + 0] = 0;//p sition1
            lines[index + 1] = 0;//position2

            //consolidate into continuous memory

            int lp = firstOpenSpace - lineSize;
            if (lp == index)
            {
                //here i am removing the highest line and placing it at the newly removed location. IF the removed line is the last line then this logical path

                //i bet this fringe case comes with weird locking implications in the first half of this method, but i haven't considered them yet.
                //fringe case but basically never
                firstOpenSpace -= lineSize;
                return;
            }
            else
            {
                lines[index + 0] = lines[lp + 0];//p sition1
                lines[index + 1] = lines[lp + 1];//position2
                //the last line has been moved back to somewhere else in memory
                firstOpenSpace -= lineSize;
            }
            //normally i would worry about cached references to the old removed line interfering with this new line if they had not yet been locked, but line removal should only happen to lines that aren't touched at all for a long time. so no such interaction should occur. may require more consideration later

        }

        public void UpdateLine(int index, int pointIndex1, int pointIndex2)
        {
            index += bufferSpace;
            
            lines[index + 0] = pointIndex1;
            lines[index + 1] = pointIndex2;
        }
    }
}
