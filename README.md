# EMU6502
A simple MOS6502 interpreter implementation in C#

EMU6502 is a simple MOS6502 interpreter with accurate cycle counting, full decimal support. With the exception of most illegal opcodes, all opcodes are fully implemented and tested.

# TEST
A simple testsuite to test every valid opcode and decimal mode.
The binaries used by the test suite are not mine, they are property of their respective authors. Source code included where available.

# Simple64
Simple64 is a quick 5-minute implementation of a C64 emulator. It loads the kernal, character and basic rom, starts execution, and converts the ram buffer containing PETSCII to ASCII and writes it to the console. Except for the blinking cursor nothing else is emulated, no CIA, VIC-II or SID, so don't expect this to run any C64 software.
For some reason, memory count is off (shows '51217 BASIC BYTES FREE').

Everything is licensed under the MIT license, except for the source code and binaries of the test suite blobs which are only modified by me specifically for the test suite.
