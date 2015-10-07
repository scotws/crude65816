\ Configuration file for A Crude 65816 Emulator
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ This version: 07. Oct 2015

\ This file must be loaded before io.fs

\  00e000 s" testrom.bin" loadrom   \ test programs created with hex2rom.fs
   00e000 s" ../tasm65816/rom.bin" loadrom   \ test program from assembler
\  010000 s" data.bin"  loadrom     \ dummy file to show how ROM data is loaded
\  00e000 s" rom65c02.bin" loadrom  \ test program for the 65c02

\  00e000 s" rom65816.bin" loadrom   \ operating system, BIOS, *DEFAULT*


\  -- DEFINITIONS -- 

\ I/O addresses. These are referenced by io.fs

   00ff00 value putchr
   00ff01 value getchr   \ blocks until character received


