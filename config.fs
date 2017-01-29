\ Configuration file for A Crude 65816 Emulator
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ This version: 25. Dec 2016

\ This file must be included in crude65816.fs before io.fs

\ --- ROMS ---

\ Pick one as the operating system. 00ff00 is putchr, 00ff01 is getchar

\ NOTE: THE FOLLOWING TWO ROMS ARE CURRENTLY BROKEN
\ 00e000 s" roms/rom65816.bin" loadrom  \ operating system, BIOS, *DEFAULT*
\ 00e000 s" roms/rom65c02.bin" loadrom  \ test program for the 65c02

\ Dummy file to show how other ROM data is loaded. Later, these can be program
\ libraries or other ROM chips.
\ 800000 s" data.bin"  loadrom

\ Tests. Unless your name is Scot, you probably don't want to touch these
\ 008000 s" tests/tink.bin" loadrom \ LiaraForth test for 265sxb Flash RAM
006000 s" tests/tink.bin" loadrom   \ LiaraForth test for 265sxb RAM

\  --- I/O ADRESSES --- 

\  These are referenced by io.fs These I/O addresses represent where (say)
\  a 6522 would be in real hardware to write to and read from. The routines
\  themselves are located in io.fs . If you do not need the default values,
\  uncomment those you do.

\ Default I/O addresses - use these when you don't care
\ 00ff00 value putchr
\ 00ff01 value getchr   \ blocks until character received


\ ** Stuff for the W65C265SXB MMM ROM ** 
\ These are for use with the Mock Mensch Monitor emulation ROM for the 265SXB.
\ We're so crude in this version that we just need an address as a hook,
\ regardless if it is where the real 265SXB writes to or not. These are subject
\ to change as the emulation gets better.
00df75 value getchr   \ actually the data register of UART 2
00df77 value putchr   \ actually the data register of UART 3 
\ 008000 s" roms/mmm/test_mmm.bin" loadrom  \ Test suite for the MMM ROM
\ 00e000 s" roms/mmm/mmm.bin" loadrom       \ Mock Mensch Monitor W65C265SXB ROM
