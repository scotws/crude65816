\ Configuration file for A Crude 65816 Emulator
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ This version: 13. Jun 2015

   00e000 s" testrom.bin" loadrom   \ test programs created with hex2rom.fs
\  00e000 s" ../tasm65816/rom.bin" loadrom   \ test program from tasm65816

\  00e000 s" rom65816.bin" loadrom   \ operating system, BIOS 
\  010000 s" math8x8.bin" loadrom    \ multiplication tables 8 bit x 8 bit

\  -- DEFINITIONS -- 
\ See about moving I/O stuff to own file

   00ff00 value putchr
   00ff01 value getchr   \ blocks until character received


