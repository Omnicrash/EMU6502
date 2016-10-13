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
using System.Threading;
using System.Threading.Tasks;
using EMU6502;

namespace TEST
{
    class Program
    {
        #region Suite

        RAM64K _ram;
        MOS6502 _cpu;

        static void Main(string[] args) { (new Program()).Run(); }
        void Run()
        {
            _ram = new RAM64K();
            _cpu = new MOS6502(_ram);

            _updateThread = new Thread(Updater);
            _updateThread.Start();

            Console.Clear();
            Console.CursorVisible = false;
            Console.WriteLine("6502 Test Suite");
            Console.WriteLine("---------------\n");

            /*
            _ram.Write(0x1000, 0x58); //CLI
            _ram.Write(0x1001, 0xF8); //SED
            _ram.Write(0x1002, 0x18); //CLC
            _ram.Write(0x1003, 0xA9); //LDA
            _ram.Write(0x1004, 0x99); //    #$99
            _ram.Write(0x1005, 0x69); //ADC
            _ram.Write(0x1006, 0x01); //    #$01

            _ram.Write16(0xFFFC, 0x1000); //RESET
            _cpu.Reset();

            //Display = true;
            while (_cpu.PC != 0x1007)
            {
                _cpu.Process();
                Console.WriteLine(_cpu.Opcode.ToString("X2"));
            }
            UpdateDisplay();
            //Display = false;
            */
            if (!TestQuick()) goto END;
            if (!TestFull()) goto END;
            if (!TestDecimalQuick()) goto END;
            if (!TestDecimalFull()) goto END;

        END:
            _updateThread.Abort();
            Console.WriteLine("\nPress any key to exit...");
            Console.CursorVisible = true;
            Console.ReadKey();
        }

        #endregion


        #region Display

        const int DISPLAY_POS = 22;

        Thread _updateThread;

        int _cyclesX;
        int _cyclesY;

        bool _display = false;
        bool Display
        {
            set
            {
                _display = value;
                if (_display)
                    StorePos();
                else
                {
                    Thread.Sleep(100);
                    UpdateDisplay();
                }
            }
        }

        void UpdateDisplay()
        {
            Console.SetCursorPosition(0, DISPLAY_POS);
            byte status = _cpu.Status;
            Console.WriteLine("PC: " + _cpu.PC.ToString("X4") + " OP: " + _cpu.Opcode.ToString("X2") + " DATA: " + _cpu.OpcodeData.ToString("X2") + " ADDRESS:" + _cpu.OpcodeAddress.ToString("X2"));
            Console.WriteLine("FLAGS: "
                + ((status & 0x80) != 0 ? "N" : " ")
                + ((status & 0x40) != 0 ? "V" : " ")
                + ((status & 0x8) != 0 ? "D" : " ")
                + ((status & 0x4) != 0 ? "I" : " ")
                + ((status & 0x2) != 0 ? "Z" : " ")
                + ((status & 0x1) != 0 ? "C" : " ") + " SP: " + _cpu.SP.ToString("X2") + " A: " + _cpu.A.ToString("X2") + " X: " + _cpu.X.ToString("X2") + " Y: " + _cpu.Y.ToString("X2"));

            Console.SetCursorPosition(_cyclesX, _cyclesY);
            Console.Write(_cpu.Cycles.ToString() + " ");
        }

        void Updater()
        {
            while (true)
            {
                if (_display)
                    UpdateDisplay();
                Thread.Sleep(10);
            }
        }

        void StorePos()
        {
            _cyclesX = Console.CursorLeft;
            _cyclesY = Console.CursorTop;
        }

        #endregion


        #region Tests

        bool TestQuick()
        {
            Console.Write("Running general quick tests... ");
            
            using (FileStream file = new FileStream("quick.bin", FileMode.Open, FileAccess.Read))
                _ram.Load(file, 0x4000);

            // Set reset vector to the start of the code
            _ram.Write16(0xFFFC, 0x4000);
            // Set IRQ vector to test BRK
            _ram.Write16(0xFFFE, 0x45A4); // IRQ
            _cpu.Reset();

            Display = true;
            while (_cpu.PC != 0x45CA)
                _cpu.Process();
            Display = false;

            byte result = _ram.Read(0x0210);
            //Console.SetCursorPosition(x, y);
            if (result == 0xFF)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK!");
                Console.ResetColor();
                return true;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("FAIL: " + result.ToString("X2"));
            Console.ResetColor();
            return false;
        }

        bool TestFull()
        {
            Console.Write("Running general full tests... ");
            StorePos();

            using(FileStream file = new FileStream("full.bin", FileMode.Open, FileAccess.Read))
                _ram.Load(file);

            // Set reset vector to the start of the code
            _ram.Write16(0xFFFC, 0x1000);
            _cpu.Reset();

            bool trapped = false;

            Display = true;
            while(_cpu.PC != 0x3B1C)
            {
                _cpu.Process();
                if(_cpu.Cycles > 81000000)
                {
                    trapped = true;
                    break;
                }
            }
            Display = false;

            if(!trapped)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK!");
                Console.ResetColor();
                return true;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("FAIL: " + _cpu.PC.ToString("X4"));
            Console.ResetColor();
            return false;
        }

        bool TestDecimalQuick()
        {
            Console.Write("Running decimal quick tests... ");

            using (FileStream file = new FileStream("simpledecimal.bin", FileMode.Open, FileAccess.Read))
                _ram.Load(file, 0x1000);

            // Set reset vector to the start of the code
            _ram.Write16(0xFFFC, 0x1000);

            // Set brk to end address
            //_ram.Write16(0xFFFE, 0x1157);
            _cpu.Reset();

            bool trapped = false;

            Display = true;
            while (_cpu.PC != 0x1123)
            {
                _cpu.Process();
                if (_cpu.Cycles > 500)
                {
                    trapped = true;
                    break;
                }
            }
            Display = false;

            if (!trapped)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK!");
                Console.ResetColor();
                return true;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("FAIL: " + _cpu.PC.ToString("X4"));
            Console.ResetColor();
            return false;
            /*
            byte result = _ram.Read(0x0);
            if (result == 0x0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK!");
                Console.ResetColor();
                return true;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("FAIL");
            Console.ResetColor();
            return false;
            */
        }

        bool TestDecimalFull()
        {
            Console.Write("Running decimal full tests... ");

            using(FileStream file = new FileStream("decimal.bin", FileMode.Open, FileAccess.Read))
                _ram.Load(file, 0x1000);

            // Set reset vector to the start of the code
            _ram.Write16(0xFFFC, 0x1000);
            _cpu.Reset();

            Display = true;
            while(_cpu.PC != 0x104B)
                _cpu.Process();

            byte result = _ram.Read(0x10);
            if(result == 0x0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK!");
                Console.ResetColor();
                return true;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("FAIL");
            Console.ResetColor();
            return false;
        }

        bool TestIllegal()
        {
            Console.Write("Running illegal opcode tests... ");

            using (FileStream file = new FileStream("illegal.bin", FileMode.Open, FileAccess.Read))
                _ram.Load(file, 0x4000);

            // Set reset vector to the start of the code
            _ram.Write16(0xFFFC, 0x4000);
            // Set IRQ vector to test BRK
            _ram.Write16(0xFFFE, 0x45A4); // IRQ
            _cpu.Reset();

            Display = true;
            while (_cpu.PC != 0x45CA)
                _cpu.Process();
            Display = false;

            byte result = _ram.Read(0x0210);
            //Console.SetCursorPosition(x, y);
            if (result == 0xFF)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK!");
                Console.ResetColor();
                return true;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("FAIL: " + result.ToString("X2"));
            Console.ResetColor();
            return false;
        }

        #endregion

    }
 
}
