# EMU6502
A simple MOS6502 interpreter implementation in C# I wrote in 2014.

It features accurate cycle counting, full decimal support. With the exception of most illegal opcodes, all opcodes are fully implemented and tested.

Real-world bugs are only emulated when the BUG symbol is defined.

# TEST
A simple testsuite to test every valid opcode and decimal mode.
The binaries used by the test suite are not mine, they are property of their respective authors. Source code included where available.

# Simple64
Simple64 is a quick 5-minute implementation of a C64 emulator. It loads the kernal, character and basic rom, starts execution, and converts the ram buffer containing PETSCII to ASCII and writes it to the console.
Except for the blinking cursor nothing else is emulated, no CIA, VIC-II or SID, so don't expect this to run any C64 software.

To run, it requires the following C64 rom files in the output folder: basic.rom, char.rom, kernal.rom.

Everything is licensed under the MIT license, except for the binaries (and source code where available) of the test suite assembly blobs which are only modified by me specifically for the test suite.
