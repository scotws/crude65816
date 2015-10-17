\ Configuration file for A Crude 65816 Emulator
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ This version: 17. Oct 2015

\ This file must be loaded before io.fs

\ --- ROMS ---

\ Pick one as the operating system. 00ff00 is putchr, 00ff01 is getchar
\  00e000 s" rom65c02.bin" loadrom  \ test program for the 65c02
\  00e000 s" rom65816.bin" loadrom  \ operating system, BIOS, *DEFAULT*

\ Test of 8-bit Forth system as demo. Putchr must be f001, getchr f004
\ 00c000 s" tests/taliforth.bin" loadrom     

\ Dummy file to show how ROM data is loaded. Later, these can be program
\ libraries or other ROM chips.
\ 800000 s" data.bin"  loadrom

\ Klaus2m5's 6502 function tests, ALPHA only  TODO remove this 
\ 00000 s" tests/6502_functional_test.bin" loadrom
\
\ Test program from assembler, for ALPHA only TODO remove this 
00e000 s" ../tasm65816/rom.bin" loadrom   


\  --- DEFINITIONS --- 

\ I/O addresses. These are referenced by io.fs

\  00ff00 value putchr
\  00ff01 value getchr   \ blocks until character received

  00f001 value putchr
  00f004 value getchr   \ blocks until character received


