// MIT License
// 
// Copyright (c) 2016 Yve Verstrepen
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.IO;
using EMU6502;

namespace Simple64
{
    class Program
    {
        static void Main(string[] args) { (new Program()).Run(); }

        MOS6502 _cpu;
        RAM64K _ram;

        void Run()
        {
            _ram = new RAM64K();
            _cpu = new MOS6502(_ram);
            
            Console.Title = "6502";
            Console.BackgroundColor = (ConsoleColor)6;
            Console.ForegroundColor = (ConsoleColor)14;
            Console.SetWindowSize(40, 26);
            Console.SetBufferSize(40, 26);
            Console.CursorSize = 100;

            //TestB();
            //return;

            //_ram.Write(0x0000, 0x2F);
            //_ram.Write(0x0001, 0x37);

            //_ram.Write(0xD018, 21);

            using (FileStream file = new FileStream("BASIC.ROM", FileMode.Open, FileAccess.Read))
                _ram.Load(file, 0xA000, 8192);

            using (FileStream file = new FileStream("CHAR.ROM", FileMode.Open, FileAccess.Read))
                _ram.Load(file, 0xD000, 4096);

            using (FileStream file = new FileStream("KERNAL.ROM", FileMode.Open, FileAccess.Read))
                _ram.Load(file, 0xE000, 8192);

            byte[] screenbuffer = new byte[1000];

            byte raster = 0;
            while (true)
            {
                _cpu.Process();
                //Console.WriteLine(cpu.PC.ToString("X4"));
                _ram.Write(0xD012, raster);
                raster++;// if (raster == 312) raster = 0;
                //Console.Clear();
                if (_cpu.Cycles % 10000 == 0)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(true);
                        _ram.Write16(0xDC00, (ushort)key.Key);
                    }
                    else
                    {
                        _ram.Write16(0xDC00, 0x0);
                    }

                    //Console.Title = _cpu.PC.ToString("X4");
                    Console.Title = _ram.Read16(0xDC00).ToString();

                    // Address where the C64 character screen buffer is located
                    ushort screenAddress = (ushort)(_ram[0x0288] << 8);

                    for (ushort i = 0; i < 1000; i++)
                    {
                        byte data = _ram.Read((ushort)(i + screenAddress));
                        if (data < 0x20)
                            data += 0x40;
                        //data &= 0x7F; // Reverse
                        if (data != screenbuffer[i])
                        {
                            Console.CursorVisible = false;
                            if ((data & 0x80) != 0)
                            {
                                Console.BackgroundColor = (ConsoleColor)14;
                                Console.ForegroundColor = (ConsoleColor)6;
                            }
                            Console.SetCursorPosition(i % 40, i / 40);
                            Console.Write((char)(data));
                            if ((data & 0x80) != 0)
                            {
                                Console.BackgroundColor = (ConsoleColor)6;
                                Console.ForegroundColor = (ConsoleColor)14;
                            }
                        }
                        screenbuffer[i] = data;
                    }
                    if (_ram[0x00CC] == 0) // Draw cursor when visible
                    {
                        int x = _ram[0x00CA];
                        int y = _ram[0x00C9];
                        if (Console.CursorLeft != x || Console.CursorTop != y)
                            Console.SetCursorPosition(x, y);
                        if (!Console.CursorVisible)
                            Console.CursorVisible = true;
                    }
                }
                //System.Threading.Thread.Sleep(50);

                /*
                cpu.Op();
                Console.WriteLine("test00: " + ram.Read(0x022A).ToString("X2"));
                Console.WriteLine("test01: " + ram.Read(0xA9).ToString("X2"));
                Console.WriteLine("test02: " + ram.Read(0x71).ToString("X2"));
                System.Threading.Thread.Sleep(50);
                if (cpu.PC == 0x45C0)
                {
                    
                    Console.WriteLine("FINAL: " + ram.Read(0x0210).ToString("X2"));
                    System.Threading.Thread.Sleep(50);
                }
                 */
            }
        }
    }
}
