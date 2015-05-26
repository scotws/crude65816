\ hex2rom.fs - Store hex numbers in file to test ROM
\ Tool for A Crude 65816 Emulator 
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ First version: 26. Mai 2015
\ This version: 27. Mai 2015

\ Edit program between the two lines by hand, and remember to 
\ update the number of bytes to write to file in the 
\ "write-file" line at the bottom. Then, call with 
\ "gforth hex2rom.fs", and on Linux, check results with 
\ "hexdump -C testrom.bin". Load that file via config.fs. In
\ crude65816, set the PC to the first instruction (usually e000) 
\ before calling "run" or "step"

hex

variable fileid

: makefile ( addr u -- )  w/o create-file drop ;  

create rombyhand

\ =================================

   0a9 c,  \ 0aa lda.#
   0aa c,  \     
   0a8 c,  \     tay

    18 c,  \     clc    \ switch to native mode 
   0fb c,  \     xce 

   0a2 c,  \ 0aa ldx.#
    9b c,  \     txy 

   0ea c,  \     nop 
   0ea c,  \     nop 

   0a0 c,  \ 0bb ldy.#
   0bb c,  \     tyx

    9b c,  \     txy

    4c c,  \     jmp    \ should BRK at 00eeee
   0ee c, 
   0ee c, 

   0db c,  \     stp

\ =================================

s" testrom.bin" makefile   ( -- fileid ) 
fileid ! 
rombyhand 10  fileid @  write-file
fileid @ close-file 

bye   

