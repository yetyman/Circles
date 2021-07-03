using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NodeDirectedFuelMap
{
    public class RandomHelper
    {
        int bufindex = 0;
        private byte[] fbuffer = new byte[sizeof(UInt32)*10000];//random decently large number
        public float Rand()
        {
            FillBuffer(fbuffer, 0, sizeof(UInt32));
            return BitConverter.ToUInt32(fbuffer, bufindex++)/(float)UInt32.MaxValue;
        }

        public unsafe void Rand(float[] randomValues)
        {
            var fs = new Span<float>(randomValues);
            var bs = MemoryMarshal.Cast<float, byte>(fs);
            var us = MemoryMarshal.Cast<byte, UInt32>(bs);

            FillBuffer(bs, 0, bs.Length);
            for (int i = 0; i < us.Length; i++)
                fs[i] = us[i] / (float)UInt32.MaxValue;
        }
        private ulong SplitMix64(ulong? nseed = null)
        {
            var seed = nseed ?? (ulong)DateTime.Now.Ticks;

            ulong result = seed += 0x9E3779B97f4A7C15;
            result = (result ^ (result >> 30)) * 0xBF58476D1CE4E5B9;
            result = (result ^ (result >> 27)) * 0x94D049BB133111EB;
            return result ^ (result >> 31);
        }
        public RandomHelper(ulong? seed = null)
        {
            InitXorShift(seed);
        }
        private void InitXorShift(ulong? seed = null)
        {
            ulong t = SplitMix64(seed);
            _x = (uint)t;
            _y = (uint)(t >> 32);

            seed += 0x9E3779B97f4A7C15;

            t = SplitMix64(seed);
            _z = (uint)t;
            _w = (uint)(t >> 32);

        }
        private uint _x, _y, _z, _w;
        private unsafe void FillBuffer(Span<byte> buf, int offset, int offsetEnd)
        {
            uint x = _x, y = _y, z = _z, w = _w; // copy the state into locals temporarily
            fixed (byte* pbytes = buf)
            {
                uint* pbuf = (uint*)(pbytes + offset);
                uint* pend = (uint*)(pbytes + offsetEnd);
                while (pbuf < pend)
                {
                    uint tx = x ^ (x << 11);
                    uint ty = y ^ (y << 11);
                    uint tz = z ^ (z << 11);
                    uint tw = w ^ (w << 11);
                    *(pbuf++) = x = w ^ (w >> 19) ^ (tx ^ (tx >> 8));
                    *(pbuf++) = y = x ^ (x >> 19) ^ (ty ^ (ty >> 8));
                    *(pbuf++) = z = y ^ (y >> 19) ^ (tz ^ (tz >> 8));
                    *(pbuf++) = w = z ^ (z >> 19) ^ (tw ^ (tw >> 8));
                }
            }
            _x = x; _y = y; _z = z; _w = w;

            bufindex = 0;
        }
    }
}
