\ Example 8 KB ROM System for 
\ A Crude 65816 Emulator (crude65816)
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ This version: 09. Jan 2015

\ After assembly, this creates an 8 kb binary file that can be 
\ loaded to $E000 in a simulator. Currently, this is all 6502 
\ 8-bit code. 

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

   hex
   cr .( Starting assembly ... )

\ --- DEFINITIONS ---

   0e000 origin  \ required  

   0ff00 value putchr  \ py65mon address for character output
   0ff01 value getchr  \ py65mon address to receive character input

\ --- STRINGS ---
\ Life is easier if you put these at the beginning of the text: It's
\ tricky to put unresolved forward references in macros

   -> intro
      s" ------------------------------------------------" strlf, 
      s" Test ROM for The Crude 65816 Emulator " strlf, 
      s" Scot W. Stevenson <scot.stevenson@gmail.com>" strlf, 
      s" ------------------------------------------------" str0, 


\ --- SUBROUTINES --- 
\ These, too, should go before the main code if at all possible

   \ Print a zero-terminated string. Assumes address in $00, $01
   -> prtstr
                  phy 
               00 ldy.#
   -> nxtchr
               00 lda.ziy
         b>  fini beq
           putchr sta
                  iny
           nxtchr bra
   -> fini 
                  ply
                  rts

\ --- MACROS ---
\ In contrast to normal assemblers, our macros don't do so well if 
\ they are first in the file. In this case, putting .STR here lets 
\ us access the strings and call the prtstr subroutine without 
\ much hassle

   \ Macro to print one linefeed
   : .linefeed  ( -- )   0a lda.#   putchr sta ; 

   \ Macro to print a string. Note this doesn't work with strings
   \ that were defined lower down because it gets tricky with
   \ unresolved links. Gforth already uses .STRING 
   : .str ( link -- ) 
      dup lsb  lda.#   00 sta.z
          msb  lda.#   01 sta.z
        prtstr jsr ; 


\ --- MAIN CODE --- 

   \ All of our vectors go here because we're cheap 
   -> vectors 

   \ Print the intro string
   intro .str  .linefeed 



\ --- INTERRUPT VECTORS --- 
   
   \ skip to interrupt vectors, filling rest of the image with zeros
   0ffe4 advance 

   vectors w, \ ffe4  COP   (native mode) 
   vectors w, \ ffe6  BRK   (native mode) 
   vectors w, \ ffe8  ABORT (native mode) 
   vectors w, \ ffea  NMI   (native mode) 
   0000    w, \ ffec  -- unused -- 
   vectors w, \ ffee  IRQ   (native mode) 
   0000    w, \ fff1  -- unused -- 
     00    b, \ fff3  -- unused -- 
   vectors w, \ fff4  COP   (emulation mode) 
   0000    w, \ fff6  -- unused -- 
   vectors w, \ fff8  ABORT (emulation mode) 
   vectors w, \ fffa  NMI   (emulation mode)
   vectors w, \ fffc  RESET (emulation mode) 
   vectors w, \ fffe  IRQ   (emulation mode) 
   
   end            

\ ----------------------------------- 
   cr .( ... assembly finished. ) 

   \ uncomment next line to save the hex dump to the file "rom.bin"
   2dup save rom65816.bin 
