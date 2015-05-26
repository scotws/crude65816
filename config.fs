\ Configuration file for A Crude 65816 Emulator
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ This version: 26. May 2015

00e000 s" testrom.bin" loadrom   \ test programs created with hex2rom.fs

\  00e000 s" rom65816.bin" loadrom   \ operating system, BIOS 
\  010000 s" math8x8.bin" loadrom    \ multiplication tables 8 bit x 8 bit

\  -- DEFINITIONS -- 

   00ff00 value putchr
   00ff01 value getchr   \ blocks until character received


