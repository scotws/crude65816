\ hex2rom.fs - Store hex numbers in file to test ROM
\ Tool for A Crude 65816 Emulator 
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ First version: 26. Mai 2015
\ This version: 26. Mai 2015

\ Call with "gforth hex2rom.fs", on Linux, check results with 
\ "hexdump -C testrom.bin". Load that file via config.fs. In
\ crude65816, set the PC to the first instruction (usuall e000) 
\ before calling "run" or "step"

hex

variable fileid

: makefile ( addr u -- )  w/o create-file drop ;  

create rombyhand

\ =================================

0a9 c,  \ 0ff lda.#
0ff c,  \
 85 c,  \ 00  sta.d
 00 c,  \
0aa c,  \     tax

0a8 c,  \     tay
0db c,  \     stp

\ =================================

s" testrom.bin" makefile   ( -- fileid ) 
fileid ! 
rombyhand 7  fileid @  write-line
fileid @ close-file 

bye   

