\ Configuration file for A Crude 65816 Emulator
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ This version: 10. Aug 2015

\ This file must be loaded before io.fs

\  00e000 s" testrom.bin" loadrom   \ test programs created with hex2rom.fs
\  00e000 s" ../tasm65816/rom.bin" loadrom   \ test program from assembler
\  010000 s" math8x8.bin" loadrom    \ multiplication tables 8 bit x 8 bit

   00e000 s" rom65816.bin" loadrom   \ operating system, BIOS 


\  -- DEFINITIONS -- 

\ I/O stuff. These are referenced by io.fs

   00ff00 value putchr
   00ff01 value getchr   \ blocks until character received


