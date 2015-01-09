\ Configuration file for the Crude 65816 Emulator
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ This version: 8. Jan 2015

   00ff00 value putchr
   00ff01 value getchr   \ blocks until character received

   00e000 s" rom65816.bin" loadrom   \ operating system, BIOS 
\  010000 s" math8x8.bin" loadrom    \ multiplication tables 8 bit x 8 bit

