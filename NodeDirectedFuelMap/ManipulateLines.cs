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

        public float[] lines;
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

                lines = new float[actualLength];

                firstOpenSpace = bufferSpace;
            }
        }

        /// <summary>
        /// just wrapping for reference passing
        /// </summary>
        public class LineIndex
        {
            public int lineIndex;
            public int references = 1;
            public object secondaryLock = new object();
            public override int GetHashCode()
            {
                return lineIndex.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                return obj is LineIndex p && p.lineIndex == lineIndex || obj is int i && i + bufferSpace == lineIndex;
            }
        }

        public LineLockPool LineLocks = new LineLockPool();
        public class LineLockPool
        {
            public Dictionary<int, LineIndex> lineLocks = new Dictionary<int, LineIndex>();
            List<LineIndex> LinePool = new List<LineIndex>();

            public LineLockPool()
            {
            }

            public LineIndex GetOne(int index)
            {
                LineIndex p = null;
                try
                {
                    //Console.WriteLine(Thread.CurrentThread.Name + " finding line lock for index " + index);
                    bool b = false;
                    try
                    {
                        Monitor.Enter(lineLocks);
                        //Console.WriteLine(Thread.CurrentThread.Name + " locked linelocks 1");
                        b = lineLocks.ContainsKey(index + bufferSpace);
                        if (b)
                        {
                            p = lineLocks[index + bufferSpace];
                            Monitor.Enter(p);
                            //Console.WriteLine(Thread.CurrentThread.Name + " line lock found for index " + index);
                            p.references++;
                        }


                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine("GetOne has failed for index "+index);
                        //Console.WriteLine(ex);
                    }
                    finally
                    {
                        Monitor.Exit(lineLocks);
                    }
                    //Console.WriteLine(Thread.CurrentThread.Name + " released linelocks 1");

                    if (!b)
                    {
                        //Console.WriteLine(Thread.CurrentThread.Name + " line lock not found for index " + index);
                        lock (LinePool)
                        {
                            //Console.WriteLine(Thread.CurrentThread.Name + " line pool not locked. obtaining lock");
                            p = LinePool.FirstOrDefault(x => x.references == 0 && x.lineIndex == -1);
                            if (p == null)
                            {
                                //Console.WriteLine(Thread.CurrentThread.Name + " pooled line lock not found. creating new one");
                                LinePool.Add(p = new LineIndex() { references = 1 });
                                Monitor.Enter(p);

                            }
                            else
                            {
                                Monitor.Enter(p);
                                p.references = 1;//set reference count before line pool releases.
                                //Console.WriteLine(Thread.CurrentThread.Name + " pooled line lock found. ");
                            }
                        }

                        p.lineIndex = index + bufferSpace;
                        //Console.WriteLine(Thread.CurrentThread.Name + " about to lock linelocks");
                        try
                        {
                            Monitor.Enter(lineLocks);
                            //Console.WriteLine(Thread.CurrentThread.Name + " adding new line lock to active list. index " + index);
                            if (lineLocks.ContainsKey(p.lineIndex))
                            {
                                //Console.WriteLine(Thread.CurrentThread.Name + " apparently two threads are using the same line index " + index);
                                p = lineLocks[p.lineIndex];
                                p.references++;
                            }
                            else
                                lineLocks.Add(p.lineIndex, p);
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine("GetOne has failed for index "+index);
                            //Console.WriteLine(ex);
                        }
                        finally
                        {
                            Monitor.Exit(lineLocks);
                        }

                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("GetOne has failed for index "+index);
                    //Console.WriteLine(ex);
                }
                finally
                {
                    if (Monitor.IsEntered(p))
                        Monitor.Exit(p);
                }

                if (p == null)
                    ;
                else if (p.lineIndex == -1)
                    ;
                return p;
            }

            public void ReleaseOne(LineIndex p)
            {
                //Console.WriteLine(Thread.CurrentThread.Name + " linelocks about to be locked 1 for index " + p.lineIndex);
                try
                {
                    Monitor.Enter(lineLocks);
                    //Console.WriteLine(Thread.CurrentThread.Name + " linelocks lock obtained 1. removing index " + p.lineIndex);
                    //Console.WriteLine(Thread.CurrentThread.Name + " line about to be locked 1 for index " + p.lineIndex);
                    try
                    {
                        Monitor.Enter(p);
                        //Console.WriteLine(Thread.CurrentThread.Name + " line lock obtained 1 for index " + p.lineIndex);
                        p.references--;
                        if (p.references == 0)
                        {
                            if (!lineLocks.Remove(p.lineIndex))
                            {
                                throw new Exception("Wut");
                            }
                            p.lineIndex = -1;
                            p.references = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine("GetOne has failed for index " + p?.lineIndex ?? "null");
                        //Console.WriteLine(ex);
                    }
                    finally
                    {
                        if (p != null)
                            Monitor.Exit(p);
                    }
                    //Console.WriteLine(Thread.CurrentThread.Name + " line lock released");
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("GetOne has failed for index "+index);
                    //Console.WriteLine(ex);
                }
                finally
                {
                    Monitor.Exit(lineLocks);
                }
                //Console.WriteLine(Thread.CurrentThread.Name + " linelocks lock released 1");


            }
        }


        HashSet<int> indexes = new HashSet<int>();
        void WaitforDuplicates(int index)
        {
            while (indexes.Contains(index)) ;
            lock(indexes)
                indexes.Add(index);
        }
        void RemoveFromDuplicates(int index)
        {
            lock(indexes)
                indexes.Remove(index);
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
            int lp;
            lock (firstOpenLock)
            {
                lp = firstOpenSpace;
                firstOpenSpace += lineSize;
                //Console.WriteLine(Thread.CurrentThread.Name + " Adding line index " + lineLock.lineIndex);
            }

            WaitforDuplicates(lp);

            lines[lp + 0] = pointIndex1;//p sition1
            lines[lp + 1] = pointIndex2;//position2

            RemoveFromDuplicates(lp);
        }

        /// <summary>
        /// rare, but occasionally done. given that this is presumed rare, it might also be a good place to consolidate memory space
        /// </summary>
        /// <param name="index"></param>
        public void RemoveLine(int index)
        {
            index += bufferSpace;
            
            WaitforDuplicates(index);

            lines[index + 0] = 0;//p sition1
            lines[index + 1] = 0;//position2

            //consolidate into continuous memory

            lock (firstOpenLock)//lock end lineer
            {
                int lp = firstOpenSpace - lineSize;
                if (lp == index + bufferSpace)
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
                                                       //Console.WriteLine(Thread.CurrentThread.Name + " line index " + lp.lineIndex + " moved to line index " + lineLock.lineIndex);
                    firstOpenSpace -= lineSize;
                }
                //normally i would worry about cached references to the old removed line interfering with this new line if they had not yet been locked, but line removal should only happen to lines that aren't touched at all for a long time. so no such interaction should occur. may require more consideration later
            }

            RemoveFromDuplicates(index);
        }

        public void UpdateLine(int index, int pointIndex1, int pointIndex2)
        {
            index += bufferSpace;
            
            WaitforDuplicates(index);

            lines[index + 0] = pointIndex1;
            lines[index + 1] = pointIndex2;

            RemoveFromDuplicates(index);
        }
    }
}
