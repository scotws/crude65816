\ Configuration file for A Crude 65816 Emulator
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ This version: 22. Dec 2016

\ This file must be loaded before io.fs

\ --- ROMS ---

\ Pick one as the operating system. 00ff00 is putchr, 00ff01 is getchar
\ 00e000 s" rom65c02.bin" loadrom  \ test program for the 65c02
\  00e000 s" rom65816.bin" loadrom  \ operating system, BIOS, *DEFAULT*

\ Example for testing file with TinkAsm assembler
008000 s" tests/tink.bin" loadrom

\ Dummy file to show how ROM data is loaded. Later, these can be program
\ libraries or other ROM chips.
\ 800000 s" data.bin"  loadrom


\  --- I/O ADRESSES --- 
\  These are referenced by io.fs

\ generic 
\ 00ff00 value putchr
\ 00ff01 value getchr   \ blocks until character received

\ For Liara Forth testing (268SBX Mensch Monitor addresses)
00e036 value getchr   \ blocks until character received
00e04b value putchr

