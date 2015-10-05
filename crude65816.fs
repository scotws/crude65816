\ A Crude 65816 Emulator 
\ Copyright 2015 Scot W. Stevenson <scot.stevenson@gmail.com>
\ Written with gforth 0.7
\ First version: 08. Jan 2015
\ This version: 05. Oct 2015 

\ This program is free software: you can redistribute it and/or modify
\ it under the terms of the GNU General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program.  If not, see <http://www.gnu.org/licenses/>.

cr .( A Crude 65816 Emulator in Forth)
cr .( Version pre-ALPHA  05. Oct 2015)  
cr .( Copyright 2015 Scot W. Stevenson <scot.stevenson@gmail.com> ) 
cr .( This program comes with ABSOLUTELY NO WARRANTY) cr


\ ---- DEFINITIONS ----
cr .( Defining general stuff ...)
hex

\ TODO make sure we need anything execpt 16M 
400 constant 1k       1k 8 * constant 8k      8k 2* constant 16k
16k 8 * constant 64k  64k 100 * constant 16M


\ ---- HARDWARE: CPU ----
cr .( Setting up CPU ... ) 

\ Names follow the convention from the WDC data sheet. We use uppercase letters.
\ P is generated, not stored, as are A and the B register
variable PC    \ Program counter (16 bit) 
variable C     \ C register (16 bit); MSB is B, LSB is A
variable X     \ X register (8\16 bit)
variable Y     \ Y register (8\16 bit)
variable D     \ Direct register (Zero Page on 6502) (16 bit) 
variable S     \ Stack Pointer (8/16 bit)
variable DBR   \ Data Bank register ("B") (8 bit)
variable PBR   \ Program Bank register ("K") (8 bit)


\ ---- HELPER FUNCTIONS ----
cr .( Creating helper functions ...) 
\ These all assume HEX

\ mask addresses / hex numbers
defer mask.a  defer mask.xy
: mask8 ( u -- u8 ) 0ff and ; 
: mask16 ( u -- u16 ) 0ffff and ; 
: mask24 ( u -- u24 ) 0ffffff and ; 
: mask.B ( u -- u16 ) 0ff00 and ; \ used to protect B  TODO really need this?

\ return least, most significant byte of 16-bit number
: lsb ( u -- u8 )  mask8 ;
: msb ( u -- u8 )  0ff00 and  8 rshift ;
: bank ( u -- u8 )  10 rshift  mask8 ;

\ Extend the sign of an 8-bit/16-bit number in a way we don't have to care about
\ how large the cell size on the Forth machine is. Assumes that TRUE flag is
\ some form of FFFF. MASK8/MASK16 is paranoid. Assumes HEX.
: signextend ( u8 -- u )  mask8 dup  80 and 0<>  8 lshift  or ; 
: signextend.l ( u16 -- u ) mask16 dup  8000 and 0<> 10 lshift or ;

\ Accumulator manipulation
\ Because we are paranoid, we tend to MASK16 all registers before we store them
\ in their variables
: C>A ( u16 -- u8 ) lsb ;  \ gives us A from C during 8 to 16 bit switch
: C>B ( u16 -- u8 ) msb ;  \ gives us B from C during 8 to 16 bit switch
: A ( -- u8 ) C @  C>A ;  \ A is a word, not variable 
: B ( -- u8 ) C @  C>B ;  \ B is a word, not variable 
\ Save A into C, protecting B 
: A>C! ( u8 -- )  B  8 lshift or  mask16  C ! ;

\ Take values from TOS and store them in the Accumulator depending on their size
defer >C  \ TODO see if this should be >A|C
: 8>C! ( u8 -- )  mask8  B  8 lshift  or   C ! ; 
: 16>C! ( u16 -- ) mask16  C ! ; 

\ Takes C and puts it TOS depending on the size of the accumulator
defer C>  \ TODO see if this should be A|C>
: C>8 ( -- u8 ) A ; 
: C>16 ( -- u16 ) C @  mask16 ; \ MASK is paranoid 

\ 16 bit addresses and endian conversion
: 16>lsb/msb  ( u16 -- lsb msb )  dup lsb swap msb ; 
: lsb/msb>16  ( lsb msb -- u16 )  8 lshift or ; 
: msb/lsb>16  ( msb lsb -- u16 )  swap lsb/msb>16 ; 

\ 24 bit to three bytes
: 24>bank/msb/lsb  ( u24 -- bank msb lsb )  
   dup 16>lsb/msb      ( u24 lsb msb )  
   swap rot            ( msb lsb u24 ) 
   bank -rot ;         ( bank msb lsb ) 
: 24>lsb/msb/bank  ( u24 -- lsb msb bank ) 
   dup 16>lsb/msb      ( n lsb msb ) 
   rot bank ;          ( lsb msb bank) 

\ Program Counter
: PC+u ( u -- ) ( -- )  
   create ,  does> @  PC @  +  mask16  PC ! ;
1 PC+u PC+1   2 PC+u PC+2   3 PC+u PC+3

\ Make 24 bit value the new 24 bit address
: 24>PC24!  ( 65addr24 -- )  24>lsb/msb/bank  PBR !  lsb/msb>16  PC ! ; 

\ Convert various combinations to full 24 bit address. Assumes HEX 
: mem16/bank>24  ( 65addr16 bank -- 65addr24 )  10 lshift or ; 
: mem16/PBR>24  ( 65addr16 -- 65addr24 )  PBR @  mem16/bank>24 ; 
: mem16/DBR>24  ( 65addr16 -- 65addr24 )  DBR @  mem16/bank>24 ; 
: lsb/msb/bank>24  ( lsb msb bank -- 65addr24 )  
   -rot lsb/msb>16 swap mem16/bank>24 ; 

\ Advance PC depending on what size our registers are
defer PC+a  defer PC+xy

\ Get full 24 bit current address (PC plus PBR) 
: PC24 ( -- 65addr24)  PC @  PBR @  mem16/bank>24 ; 


\ ---- MEMORY ----

\ All accesses to memory are always full 24 bit. Stack follows little-endian
\ format with bank on top, then msb and lsb ( lsb msb bank -- ) 
cr .( Creating memory ...) 

\ We just allot the whole possible memory range. Note that this will fail unless
\ you called Gforth with "-m 1G" or something of that size like you were told in
\ the MANUAL.txt . You did read the manual, didn't you?
create memory 16M allot

: loadrom ( 65addr24 addr u -- )
   r/o open-file drop            ( 65addr fileid ) 
   slurp-fid                     ( 65addr addr u ) 
   rot  memory +  swap           ( addr 65addrROM u ) 
   move ;  

\ load ROM files into memory
cr .( Loading ROM files to memory ...) 
include config.fs  

\ set up I/O stuff. Must be loaded after config.fs
cr .( Setting up I/O system ...)
include io.fs

\ Fetch data from memory, depending on the size of the register in question 
defer fetch.a  defer fetch.xy

\ Get one byte, a double byte, or three bytes from any 24-bit memory address.
\ Double bytes assume  little-endian storage in memory but returns it to the
\ Forth data stack in "normal" big endian format. We don't advance the PC here
\ so we can use these routines with stuff like stack manipulations

\ FETCH8 includes the check for special addresses (I/O chips, etc) so all other
\ store words should be based on it
: fetch8  ( 65addr24 -- u8 )  
   special-fetch?  dup 0= if     ( 65addr24 0|xt)
      drop  memory +  c@  else   \ C@ means no MASK8 is required
      nip execute  then ; 
: fetch16  ( 65addr24 -- u16 )  dup fetch8  swap 1+ fetch8  lsb/msb>16 ; 
: fetch24  ( 65addr24 -- u24 )  
   dup fetch8  over 1+ fetch8   rot 2 + fetch8  lsb/msb/bank>24 ; 

\ Store registers to memory, depending on size of register 
defer store.a   defer store.xy

\ STORE8 includes the check for special addresses (I/O chips, etc) so all other
\ store words should be based on it
: store8 ( u8 65addr24 -- ) 
   special-store?  dup 0= if     ( u8 65addr24 0|xt)
      drop  memory +  c!  else   \ C! means that no MASK is required
      nip execute  then ; 
: store16 ( u16 65addr24 -- ) \ store LSB first
   2dup swap lsb swap store8  swap msb swap 1+ store8 ; 
: store24 ( u24 65addr24 -- ) 
\ TODO This is horrible, rewrite 
   >r               ( u24  R: 65addr ) 
   24>bank/msb/lsb  ( bank msb lsb  R: 65addr)
   r> dup 1+ >r     ( bank msb lsb 65addr  R: 65addr+1)
   store8           ( bank msb  R: 65addr+1) 
   r> dup 1+ >r     ( bank msb 65addr+1  R: 65addr+2)
   store8           ( bank  R: 65addr+2)
   r> store8 ; 

\ Read current bytes in stream. Note we use PBR, not DBR. These do not advance
\ the PC
: next1byte ( -- u8 )  PC24 fetch8 ; 
: next2bytes ( -- u16 )  PC24 fetch16 ; 
: next3bytes ( -- u24 )  PC24 fetch24 ; 


\ ---- FLAGS ----
cr .( Setting up flag routines ... ) 

\ make flag routines easier for humans to work with 
: set?  ( addr -- f )  @ ;  
: clear?  ( addr -- f )  @ invert ;
: set  ( addr -- )  true swap ! ; 
: clear  ( addr -- )  false swap ! ; 

\ All 65816 are fully-formed Forth flags, that is, one cell wide. There is no
\ flag in bit 5 in emulation mode. The convention is to use lowercase letters
\ for the flags to avoid confusion with the register names

create flags
   false , false , false , false , false , false , false , false , 

\ We start with n-flag, not c-flag, as first entry in the table to make
\ creating P with loops easier
: n-flag ( -- addr ) flags ;           \ bit 7 
: v-flag ( -- addr ) flags cell + ;    \ bit 6 
: m-flag ( -- addr ) flags 2 cells + ; \ bit 5 in native mode 
: x-flag ( -- addr ) flags 3 cells + ; \ bit 4 in native mode 
: b-flag ( -- addr ) flags 3 cells + ; \ bit 4 in emulated mode 
: d-flag ( -- addr ) flags 4 cells + ; \ bit 3 
: i-flag ( -- addr ) flags 5 cells + ; \ bit 2 
: z-flag ( -- addr ) flags 6 cells + ; \ bit 1 
: c-flag ( -- addr ) flags 7 cells + ; \ bit 0 

\ And then there's this guy: Emulation flag is not part of the status byte
variable e-flag

\ We don't use bit 5 in emulation mode, but it looks weird if it is set when we
\ switch from 16-bit A in native to emulation mode, so we take care of it
\ TODO check hardware to see what actually happens during these switches
: unused-flag ( -- addr ) flags 2 cells + ; 

\ These are used to make a flag reflect the set/clear status of a bit in a byte
\ or word provided. Mask byte or word with AND to isolate single bits and then
\ use there
: test&set-c ( u -- )  0<> c-flag ! ; 
: test&set-n ( u -- )  0<> n-flag ! ; 
: test&set-v ( u -- )  0<> v-flag ! ; 
: test&set-z ( u -- )  0= z-flag ! ; 

defer mask-N.a
: mask-N.8  ( u8 -- u8 ) 80 and ; 
: mask-N.16 ( u16 -- u16 ) 8000 and ; 

defer mask-V.a 
: mask-V.8 ( u8 -- u8 ) 40 and ; 
: mask-V.16 ( u16 -- u16 ) 4000 and ; 

\ ---- TEST AND SET FLAGS ----

\ The basic, unspecific routines consume TOS, the derived functions do not
\ TODO THIS IS A MESS, REWRITE
\ TODO Rewrite and combine these once we know what we are doing 
\ TODO Rewrite these so they all consume or don't consume TOS

\ Carry Flag
defer check-C.a  defer check-C.x  defer check-C.y
: check-C  ( n n -- )  < if  c-flag clear  else  c-flag set  then ; 

\ MASKs are paranoid 
\ TODO Replace with versions that use C>
: check-C.a8  ( n8 -- )  A  check-C ;
: check-C.a16  ( n16 -- )  C @  mask16 check-C ; 
: check-C.x8  ( n8 -- )  X @  mask8 check-C ; 
: check-C.x16  ( n16 -- )  X @  mask16 check-C ; 
: check-C.y8  ( n8 -- )  Y @  mask8 check-C ; 
: check-C.y16  ( n16 -- )  Y @  mask16 check-C ; 

\ Negative Flag
defer check-N.a  defer check-N.x  defer check-N.y
: check-N8 ( n -- )  mask-N.8  test&set-n ;
: check-N16 ( n -- )  mask-N.16 test&set-n ;

\ MASKs are paranoid
: check-N.a8 ( -- )  A check-N8 ;
: check-N.a16 ( -- )  C @ check-N16 ; 
: check-N.x8 ( -- )  X @  mask8 check-N8 ; 
: check-N.x16 ( -- )  X @  mask16 check-N16 ; 
: check-N.y8 ( -- )  Y @  mask8 check-N8 ; 
: check-N.y16 ( -- )  Y @ mask16 check-N16 ;   

\ Zero Flag
defer check-Z.a
: check-Z ( n -- )  test&set-z ; 

: check-Z.a8 ( -- )  A check-Z ;
: check-Z.a16 ( -- )  C @  check-Z ; 
: check-Z.x ( -- )  X @  check-Z ;
: check-Z.y ( -- )  Y @  check-Z ; 

\ Common combinations
defer check-NZ.TOS   \ Used for LSR and other instructions that don't work on C 
: check-NZ.8 ( n8 -- )  dup check-N8 check-Z ; 
: check-NZ.16 ( n16 -- )  dup check-N16 check-Z ; 
: check-NZ.a ( -- )  check-N.a  check-Z.a ; 
: check-NZ.x ( -- )  check-N.x  check-Z.x ; 
: check-NZ.y ( -- )  check-N.y  check-Z.y ; 
: check-NZC.a ( -- )  check-N.a  check-Z.a  check-C.a ; 
: check-NZC.x ( -- )  check-N.x  check-Z.x  check-C.x ; 
: check-NZC.y ( -- )  check-N.y  check-Z.y  check-C.y ; 
 
\ Routines to find out if addition produced a carry flag
defer carry?.a
: carry?.8 ( u -- f )  100 and 0<> ; 
: carry?.16 ( u -- f ) 10000 and 0<> ; 


\ --- BCD ROUTINES ---
cr .( Setting up BCD routines ...) 

\ BCD is required for decimal mode addition and subtraction operations. It is
\ also a pain in the rear. See http://www.6502.org/tutorials/decimal_mode.html
\ and https://en.wikipedia.org/wiki/Binary-coded_decimal for the background on
\ these routines. Check the Known Issues section of MANUAL.txt for known
\ problems with these routines

\ TODO We should be able to simplify and condense these once they are very,
\ very throughly tested

\ -- 8 bits -- 

\ Nine's complement of a nibble, for BCD subtraction
: 9s-comp ( u -- u ) 9 swap - ; 

: byte>nibbles ( u -- nh nl )  dup 0f0 and  4 rshift  swap 0f and ; 
: nibbles>byte ( nh nl -- u )  swap 4 lshift or ; 

\ Split up a byte into nibbles that are nine's complement, used for BCD
\ subtraction 
: byte>9s-nibbles ( u -- nh nl )
   dup 0f0 and  4 rshift 9s-comp 
   swap 0f and 9s-comp ;  

\ Split up two bytes and interweave their nibbles so they are ready for addition
: nibbleweave-add ( u1 u2 -- n2h n1h n1l n2l)  
   byte>nibbles rot byte>nibbles rot ;

\ Split up two bytes and interweave their nibbles so they are ready for
\ subtraction (more exactly, addition with nine's complement) 
: nibbleweave-sub ( u1 u2 -- n1h n2h n1l n2l)  
   byte>9s-nibbles rot byte>nibbles rot 
   >r -rot swap rot r> ;   \ order is important for subtraction

\ Add two nibbles in BCD style. Intialize the carry with zero. Results in the
\ sum of the two nibbles (nr) and the "carry nibble" (nc) that is reused
: bcd-add-nibble ( n1 n2 c -- nc nr)  + +  dup 9 > if 6 + then  byte>nibbles ; 

\ Add two nibbles in BCD style. Intialize the carry with zero. Results in the
\ sum of the two nibbles (nr) and the "carry nibble" (nc) that is reused
: bcd-sub-nibble ( n1 n2 c -- nc nr)  + +  dup 9 > if 6 + then byte>nibbles ; 

\ Add two bytes BCD style, including the c-flag. We use this routine for the
\ 8-bit ADC routine when the d-flag ist set
: bcd-add-bytes ( u1 u2 -- ur )
   nibbleweave-add  c-flag @  1 and 
   bcd-add-nibble >r  ( n2h n1h nc -- R: nl )
   bcd-add-nibble r> nibbles>byte    ( nc nr )
   swap test&set-c ; 
 
\ Subtract two bytes BCD style, including the c-flag. We use this routine 
\ for the 8-bit SBC routine when the d-flag ist set
: bcd-sub-bytes ( u1 u2 -- ur )
   swap             \ We fetch the operand before we get the accumulator
   nibbleweave-sub  c-flag @  1 and 
   bcd-sub-nibble >r  ( n2h n1h nc -- R: nl )
   bcd-sub-nibble r>  nibbles>byte    ( nc nr )
   swap test&set-c ; 

\ -- 16 bits -- 

: word>bytes ( w -- uh ul )  dup 0ff00 and  8 rshift  swap 0ff and ; 
: bytes>word ( uh ul -- w )  swap 8 lshift  or ; 

\ Split up two words and interweave their words so they are ready for addition
: byteweave-add ( w1 w2 -- u2h u1h u1l u2l)  word>bytes rot word>bytes rot ;

\ Add two words BCD style, including the c-flag. We use this routine for the
\ 16-bit ADC routine when the d-flag ist set
: bcd-add-words ( w1 w2 -- w2 ) 
   byteweave-add  bcd-add-bytes >r  bcd-add-bytes r> bytes>word ; 

\ Split up two words and interweave their words so they are ready for
\ subtraction (rather, addition with nine's complement) 
: byteweave-sub ( w1 w2 -- u1h u2h u1l u2l)  
   word>bytes rot word>bytes -rot swap rot ;  
\
\ Subtract two words BCD style, including the c-flag. We use this routine 
\ for the 16-bit SBC routine when the d-flag ist set
: bcd-sub-words ( w1 w2 -- w2 ) 
   byteweave-sub swap bcd-sub-bytes >r swap bcd-sub-bytes r> bytes>word ; 


\ --- COMPARE INSTRUCTIONS ---

\ See http://www.6502.org/tutorials/compare_beyond.html for discussion TODO see
\ if we need these or if we can use the CHECK-XX routines directly for the CMP
\ instructions
defer cmp.a  defer cmp.xy
: cmp8  ( AXY u8 -- )  2dup check-C  - check-NZ.8 ; 
: cmp16  ( CXY u16 -- )  2dup check-C  - check-NZ.16 ; 

\ --- BRANCHING --- 
cr .( Setting up branching ...) 

: takebranch ( -- )  next1byte signextend 1+  PC @ +  PC ! ;
: branch-if-true ( f -- )  if takebranch else PC+1 then ; 

\ --- STACK STUFF ----
cr .( Setting up stack ...)

\ increase stack pointer 
defer S++.a  defer S++.xy
: S++8 ( -- )  S @  1+  mask8  0100 OR  S ! ; 
: S++16 ( -- )  S @  1+  mask16  S ! ; 

\ decrease stack pointer, hardcoding 01 as MSB of pointer
defer S--.a  defer S--.xy
: S--8 ( -- )  S @  1-  mask8  0100 OR  S ! ; 
: S--16 ( -- )  S @  1-  mask16  S ! ; 

\ Push stuff to stack
defer push.a  defer push.xy
: push8 ( n8 -- )  S @  store8  S--8 ; 
: push16 ( n16 -- )  16>lsb/msb push8 push8 ; 
: push24 ( n24 -- )  24>bank/msb/lsb  rot push8  swap push8  push8 ; 

\ Pull stuff from stack 
defer pull.a  defer pull.xy
: pull8 ( -- n8 )  S++8  S @  fetch8 ; 
: pull16 ( -- n16 )  pull8 pull8 lsb/msb>16 ; 
: pull24 ( -- n24 )  pull8 pull8 pull8 lsb/msb/bank>24 ; 
   

\ ---- REGISTER MODE SWITCHES ----

\ We use two internal flags to remember the width of the registers. Don't use
\ the x and m flags directly because this can screw up the status byte P 
variable a16flag   a16flag clear 
variable xy16flag  xy16flag clear 

\ Switch accumulator 8<->16 bit (p. 51 in Manual)
\ TODO ADD MASK.A 
: a:16  ( -- )  
   ['] fetch16 is fetch.a
   ['] store16 is store.a
   ['] 16>C! is >C 
   ['] C>16 is C>
   ['] PC+2 is PC+a
   ['] check-N.a16 is check-N.a
   ['] check-Z.a16 is check-Z.a
   ['] check-NZ.8 is check-NZ.TOS
   ['] cmp16 is cmp.a
   ['] S++16 is S++.a
   ['] S--16 is S--.a
   ['] push16 is push.a 
   ['] pull16 is pull.a
   ['] mask16 is mask.a
   ['] mask-N.16 is mask-N.a
   ['] mask-V.16 is mask-V.a
   ['] carry?.16 is carry?.a
   a16flag set ; 

: a:8 ( -- )  
   ['] fetch8 is fetch.a
   ['] store8 is store.a
   ['] 8>C! is >C 
   ['] C>8 is C>
   ['] PC+1 is PC+a
   ['] check-N.a8 is check-N.a
   ['] check-Z.a8 is check-Z.a
   ['] check-NZ.8 is check-NZ.TOS
   ['] cmp8 is cmp.a
   ['] S++8 is S++.a
   ['] S--8 is S--.a
   ['] push8 is push.a 
   ['] pull8 is pull.a
   ['] mask8 is mask.a
   ['] mask-N.8 is mask-N.a
   ['] mask-V.8 is mask-V.a
   ['] carry?.8 is carry?.a
   a16flag clear ;

\ Switch X and Y 8<->16 bit (p. 51 in Manual) 
: xy:16  ( -- )  
   ['] fetch16 is fetch.xy 
   ['] store16 is store.xy
   ['] mask16 is mask.xy
   ['] PC+2 is PC+xy
   ['] check-N.x16 is check-N.x
   ['] check-N.y16 is check-N.y
   ['] cmp16 is cmp.xy
   ['] S++16 is S++.xy
   ['] S--16 is S--.xy
   ['] push16 is push.xy 
   ['] pull16 is pull.xy
   X @  00FF AND  X !   Y @  00FF AND  Y !  \ paranoid
   xy16flag set ; 

: xy:8 ( -- )  
   ['] fetch8 is fetch.xy
   ['] store8 is store.xy
   ['] mask8 is mask.xy
   ['] PC+1 is PC+xy
   ['] check-N.x8 is check-N.x
   ['] check-N.y8 is check-N.y
   ['] cmp8 is cmp.xy
   ['] S++8 is S++.xy
   ['] S--8 is S--.xy
   ['] push8 is push.xy 
   ['] pull8 is pull.xy
   X @  00FF AND  X !   Y @  00FF AND  Y !  
   xy16flag clear ; 

\ switch processor modes (native/emulated). There doesn't seem to be a good
\ verb for "native" like "emulate", so we're "going" 
: native ( -- )  
   e-flag clear
   m-flag set
   x-flag set
   \ TODO set direct page to 16 zero page
   ; 

: emulated ( -- )  \ p. 45
   e-flag set   
   b-flag clear      \ TODO Make sure this is really what happens
   unused-flag clear \ Make sure unused status bit 5 is not set 
   a:8   xy:8
   S @  00FF AND  0100 OR  S ! \ stack pointer to 0100
   0000 D !  \ direct page register initialized to zero 
   
   \ TODO Do what with status bit 5 ? 
   \ TODO PBR and DBR ?
   ; 

\ --- STATUS BYTE --- 
\ These routines have to come after mode switches for the registers 

\ Create status byte out of flag array. We don't care if we are in emulation or
\ native mode
: P ( -- u8 ) 
   00                      \ initialize P byte 
   8 0 ?do                 
      1 lshift             \ next bit; note first shift is a dummy 
      flags i cells +  @   \ loop thru flag table, from high bit to low
      1 and  +             \ get last bit of Forth flag
   loop ;

\ Create new flag array based on status byte. Used mainly for PLP instruction
\ TODO rewrite this once we know it works and REP and SEP are complete

\ In emulated mode, bit 5 is always zero 
\ TODO check hardware to see if this is true 
\ TODO see if we can use same routine for REP and SEP
: clear-unused ( -- ) e-flag set? if unused-flag clear then ; 

\ In native mode, changing m and x flags might change the size of these
\ registers 
\ TODO get rid of the IFs 
\ TODO see if we can use same routine for REP and SEP
: flag-modeswitch ( -- ) 
   e-flag clear? if
      m-flag set? if a:8  a16flag clear  else
                     a:16  a16flag set  then 
      x-flag set? if xy:8  xy16flag clear  else
                     xy:16  xy16flag set  then 
   then ;  

: >P ( u8 -- ) 
   0 7 ?do
      dup 1 and                  \ get lowest bit 
      0= if false else true then \ convert to Forth flag
      flags i cells +  !         \ store in flag array
      1 rshift
   -1 +loop 

   clear-unused
   flag-modeswitch ; 


\ ---- ADDRESSING MODES --- 
cr .( Defining addressing modes ...) 

\ Mode words leave the correct address as TOS before the PC. Note that the
\ mnemonics for Absolute Mode have no suffix, but we use MODE.ABS for clarity.
\ Not all modes are listed here, because some are easier to code by hand. Nodes
\ advance the PC so we don't have to include that in the operand code; since the
\ PC is usually TOS, this requires some stack manipulation.  Register
\ manipulation should come before the mode word (eg "Y @  MODE.ABS.DBR"), not
\ behind it. 

\ Examples for the modes are given for the traditional syntax and for Typist's
\ Assembler

: mode.d-core ( -- 65addr16 )  next1byte D @ + ; 

\ ## Absolute: "LDA $1000" / "lda 1000" ##
\ We need two different versions, one for instructions that affect data and take
\ the DBR, and one for instructions that affect programs and take the PBR
\ TODO handle page boundries / wrapping
: mode.abs.DBR ( -- 65addr24 )  next2bytes mem16/DBR>24 PC+2 ;
: mode.abs.PBR ( -- 65addr24 )  next2bytes mem16/PBR>24 PC+2 ;

\ ## Absolute Indirect: "JMP ($1000)" / "jmp.i 1000" ##
\ TODO handle page boundries / wrapping
: mode.i  ( -- 65addr24)  mode.abs.PBR  fetch16  mem16/PBR>24 ;

\ ## Absolute Indirect LONG (p. 293): "JMP [$1000]" / "jmp.il 1000" ##
\ TODO handle page boundries / wrapping
: mode.il  ( -- 65addr24)  next2bytes 00 mem16/bank>24 fetch24 ; 

\ ## Absolute Indexed X/Y (pp. 289-290): "LDA $1000,X" / "lda.x 1000" ##
\ Assumes that X will be the correct width (8 or 16 bit)
\ TODO handle page boundries / wrapping
: mode.x  ( -- 65addr24 )  mode.abs.DBR  X @  + ;
: mode.y  ( -- 65addr24 )  mode.abs.DBR  Y @  + ;

\ ## Absolute X Indexed Indirect (p. 291): "JMP ($1000,X)" / "jmp.xi 1000" ##
\ TODO handle page boundries / wrapping
: mode.xi  ( -- 65addr24 )  next2bytes  X @ +  
   mask16  PBR @  mem16/bank>24 fetch16 ; 

\ ## Absolute Long: "LDA $100000" / "lda.l 100000" ##
\ TODO handle page boundries / wrapping
: mode.l  ( -- 65addr24)  next3bytes PC+3 ;

\ ## Absolute Long X Indexed: "LDA $100000,X" / "lda.lx 100000" ##  
\ This assumes that X will be the correct width (8 or 16 bit) 
\ TODO handle page boundries / wrapping
: mode.lx ( -- 65addr24)  mode.l  X @  + ; 

\ ## Direct Page (DP) (pp. 94, 155, 278): "LDA $10" / "lda.d 10" ##
\ Note that D can be relocated in Emulated Mode as well, see
\ http://forum.6502.org/viewtopic.php?f=8&t=3459&p=40389#p40370 for details
\ TODO handle page boundries / wrapping
: mode.d  ( -- 65addr24)  mode.d-core   00  mem16/bank>24 PC+1 ;

\ ## DP Indexed X/Y (p. 299): "LDA $10,X" / "lda.dx 10" ##
\ TODO handle page boundries / wrapping
: mode.dx  ( -- 65addr24)  mode.d-core  X @ +  0 mem16/bank>24 PC+1 ; 
: mode.dy  ( -- 65addr24)  mode.d-core  Y @ +  0 mem16/bank>24 PC+1 ; 

\ ## DP Indirect  (p. 302):  "LDA ($10)" / "lda.di 10" ##
\ Note this uses the Data Bank Register DBR, not PBR
\ TODO handle page boundries / wrapping
: mode.di  ( -- 65addr24)  
   mode.d-core  00 mem16/bank>24  fetch16  DBR @  mem16/bank>24 PC+1 ;

\ ## DP Indirect X Indexed (p. 300): "LDA ($10,X)" / "lda.dxi 10" ##
\ TODO handle page boundries / wrapping
: mode.dxi  ( -- 65addr24)  mode.d-core  X @ +  00 mem16/bank>24 
   fetch16  DBR @ mem16/bank>24 PC+1 ; 

\ ## DP Indirect Y Indexed (p. 304): "LDA ($10),Y" / "lda.diy 10" ##
\ Does not need a "PC+1" because this is contained in MODE.DI
\ TODO handle page boundries / wrapping
: mode.diy  ( -- 65addr24)  mode.di  Y @ + ; 

\ ## DP Indirect Long: "LDA [$10]" / "lda.dil 10" ##
\ TODO handle page boundries / wrapping
: mode.dil  ( -- 65addr24) mode.d-core  00 mem16/bank>24 fetch24 PC+1 ; 
\
\ ## DP Indirect Long Y Addressing : "LDA [$10],y" / "lda.dily 10" ##
\ TODO handle page boundries / wrapping 
\ TODO KNOWN ISSUE: WRAPING BROKEN IF Y IN 16 BIT MODE
: mode.dily  ( -- 65addr24) mode.dil  Y @ + ; 

\ ## Immediate Mode: "LDA #$10" / "lda.# 10" ##
\ Note that this mode does not advance the PC as it is used with A and XY
\ TODO handle page boundries / wrapping
: mode.imm  ( -- 65addr24 )  PC24 ; 

\ ## Stack Relative (p. 324): "LDA $10,S" / "lda.s 10" ##
\ TODO handle page boundries / wrapping
: mode.s  ( -- 65addr24 ) next1byte  S @  +  0 mem16/bank>24 PC+1 ; 

\ ## Stack Relative Y Indexed: "LDA (10,S),Y" / "lda.siy 10" ##
\ No "PC+1" because this is handled by MODE.S
\ TODO handle page boundries / wrapping
: mode.siy  ( -- 65addr24 )  mode.s  Y @ +  DBR @  mem16/bank>24 ; 


\ ---- OUTPUT FUNCTIONS ----
cr .( Creating output functions ...) 

\ Print byte as bits, does not add space, returns as HEX
: .8bits ( u -- ) 
   2 base !  s>d <# # # # # # # # # #> type  hex ; 

\ Format numbers to two, four, and six places, assumes HEX
: .mask8 ( n -- )  s>d <# # # #> type space ; 
: .mask16 ( n -- )  s>d <# # # # # #> type space ; 
: .mask24 ( n -- )  s>d <# # # # # # # #> type space ; 

\ Print state of machine 
\ TODO rewrite this once we know what we really want to see
: .state ( -- )

   \ --- Print status line --- 

   \  ." xxxx xx "
   cr ."  PC   K "

   a16flag clear?  e-flag set?  or  if 
   \  ." xx xx "
      ."  B  A " else
   \  ." xxxx "
      ."   C  " then 
   
   xy16flag clear?  e-flag set?  or  if 
   \  ." xx xx "
      ."  X  Y " else
   \  ." xxxx xxxx "
      ."   X    Y  " then 
 
   e-flag set? if 
   \  ." xxxx xxxx xx xxxxxxxx" 
      ."   S    D   B NV-BDIZC" else
      ."   S    D   B NVMXDIZC" then cr 

   \ --- Print data ---

   PC @  .mask16  PBR @ .mask8   
   
   \ print BA or C
   a16flag clear?  e-flag set?  or  if  
      B .mask8  A .mask8 else
      C @ .mask16 then

   \ print X and Y
   Y @  X @   xy16flag clear? if  .mask8 .mask8  else  .mask16 .mask16  then 
   
   S @ .mask16   D @ .mask16   DBR @ .mask8
   P .8bits  space 
   e-flag set? if ." emulated" else ." native" then cr ; 

\ Print stack if we are in emulated mode
: stackempty? ( -- f )  S @  01ff  = ; 
: .stack ( -- )
   cr  e-flag clear? if
         ." Can't dump stack when in native mode"
      else
         stackempty? if  
            ." Stack is empty (S is 01FF in emulated mode)" cr  else
         0200  S @ 1+  ?do  i dup .  space  fetch8 .mask8  cr  loop 
      then then ; 

\ Print Direct Page contents. We use D as a base regardless of which mode we are
\ in; see MODE.D for discussion of what happens with D in emulation mode.
\ Assumes HEX.
: .direct ( -- ) 
   cr ."        0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F"
   100 0 ?do  cr  D @  i +  .mask16 ."  " 
      10 0 ?do   D @  i +  j +  fetch8 .mask8 loop 
   10 +loop cr ; 


\ ---- OPCODE CORE ROUTINES ----
cr .( Defining core routines for opcodes ) 
\ TODO Rewrite/optimize/refract these

\ These all work in both 8- and 16-bit modes
: and-core ( 65addr -- )  fetch.a mask.a C> and >C check-NZ.a ;
: eor-core ( 65addr -- )  fetch.a mask.a C> xor >C check-NZ.a ; 
: ora-core ( 65addr -- )  fetch.a mask.a C> or >C check-NZ.a ; 

\ ASL-CORE is used for all, ASL-MEM for memory shifts 
: asl-core ( u -- u )  dup mask-N.a test&set-c 1 lshift ; 
: asl-mem ( addr -- )  dup fetch.a asl-core dup check-NZ.TOS swap store.a ; 

\ LSR-CORE is used for all, LSR-MEM for memory shifts
: lsr-core ( u -- u )  dup 1 and test&set-c  1 rshift ; 
: lsr-mem ( addr -- )  dup fetch.a lsr-core dup check-NZ.TOS swap store.a ; 

\ ROL-CORE is used for all, ROL-MEM for memory shifts
: rol-core ( u -- u )  
   c-flag @  1 and swap  dup mask-N.a test&set-c  1 lshift or ;
: rol-mem ( addr -- )  dup fetch.a rol-core dup check-NZ.TOS swap store.a ; 

\ ROR-CORE is used for all, ROR-MEM for memory shifts
: ror-core ( u -- u )  
   c-flag @ mask-N.a swap  dup 1 and test&set-c  1 rshift or ; 
: ror-mem ( addr -- )  dup fetch.a ror-core dup check-NZ.TOS swap store.a ; 

: bit-core ( 65addr -- ) fetch.a 
   dup mask-N.a test&set-n  dup mask-V.a test&set-v  C> and  check-Z ;  

\ INC and DEC for the Accumulator
: inc.accu ( -- ) C> 1+ mask.a dup check-NZ.a >C ; 
: dec.accu ( -- ) C> 1- mask.a dup check-NZ.a >C ; 

\ INC and DEC for memory
: inc.mem  ( 65addr -- ) dup fetch.a 1+ mask.a dup check-NZ.TOS swap store.a ; 
: dec.mem  ( 65addr -- ) dup fetch.a 1- mask.a dup check-NZ.TOS swap store.a ; 

: cmp-core ( u 65addr -- ) fetch.a cmp.a ; 
: cpxy-core ( u 65addr -- ) fetch.xy cmp.xy ; 

: lda-core ( 65addr -- )  fetch.a  >C  check-NZ.a ;
: ldx-core ( 65addr -- )  fetch.xy  X !  check-NZ.x ;
: ldy-core ( 65addr -- )  fetch.xy  Y !  check-NZ.y ;

\ -- Addition routines -- 

\ Use this for both ADC and SBC
: adc-sbc-core ( u -- ) 
   dup >r      \ save operand for Overflow calculation
   C>  dup >r  \ save accumulator for Overflow calculation 
   +  c-flag @ 1 and +  dup >C  carry?.a test&set-c  check-NZ.a
   r> C> or  r> C> or  and  mask-N.a  0<> v-flag ! ;  \ calculate Overflow

\ Common routine for 8- and 16-bit binary addition 
: adc-bin ( addr -- ) fetch.a adc-sbc-core ; 
   
\ Routines for 8- and 16-bit BCD addition
\ WARNING: The v-flag is currently not correctly emulated in decimal mode, see
\ http://www.6502.org/tutorials/vflag.html for details 
\ TODO see if we can fold this into one routine to simplify table
: adc-bcd.8 ( addr -- ) fetch.a C> bcd-add-bytes >C ; 
: adc-bcd.16 ( addr -- ) fetch.a C> bcd-add-words >C ; 

create additions
   ' adc-bin ,    \  8 bit binary:  D clear, a16flag clear (00) 
   ' adc-bin ,    \ 16 bit binary:  D clear, a16flag set (01) 
   ' adc-bcd.8 ,  \ 16 bit decimal: D set, a16flag clear (10)
   ' adc-bcd.16 , \  8 bit decimal: D set, a16flag set (11)   

: adc-core ( 65addr -- ) 
   d-flag @ 2 and  a16flag @ 1 and  or  cells  \ calculate table index 
   additions +  @ execute ; 


\ --- Subtraction Routines ---

\ The 6502 and 65816 use the c-flag as an inverted borrow
: invert-borrow ( -- ) c-flag dup @ invert swap ! ; 

\ Common routine for 8- and 16-bit binary subtraction
: sbc-bin ( addr -- ) fetch.a invert adc-sbc-core invert-borrow ; 

\ Routines for 8- and 16-bit BCD subtraction
\ WARNING: The v-flag is currently not correctly emulated in decimal mode, see
\ http://www.6502.org/tutorials/vflag.html for details 
\ Also see http://visual6502.org/wiki/index.php?title=6502DecimalMode
\ TODO see if we can fold this into one routine to simplify table
: sbc-bcd.8 ( addr -- ) fetch.a C> bcd-sub-bytes >C ; 
: sbc-bcd.16 ( addr -- ) fetch.a C> bcd-sub-words >C ; 

create subtractions
   ' sbc-bin ,    \  8 bit binary:  D clear, a16flag clear (00) 
   ' sbc-bin ,    \ 16 bit binary:  D clear, a16flag set (01) 
   ' sbc-bcd.8 ,  \ 16 bit decimal: D set, a16flag clear (10)
   ' sbc-bcd.16 , \  8 bit decimal: D set, a16flag set (11)   

: sbc-core ( 65addr -- ) 
   d-flag @ 2 and  a16flag @ 1 and  or  cells  \ calculate table index 
   subtractions +  @ execute ; 



\ ---- OPCODE ROUTINES ----
cr .( Defining opcode routines themselves ... ) 

\ TODO change BRK so we drop into single-step mode, add stack effects
: opc-00 ( brk )   cr ." *** BRK encountered, halting CPU (ALPHA only) ***" 
   .state quit ; 

: opc-01 ( ora.dxi )  mode.dxi ora-core ; 

: opc-02 ( cop )  ." 02 not coded yet" ; 

: opc-03 ( ora.s )   mode.s ora-core ; 

: opc-04 ( tsb.d )  ." 04 not coded yet" ; 

: opc-05 ( ora.d )  mode.d ora-core ; 
: opc-06 ( asl.d )  mode.d asl-mem ; 
: opc-07 ( ora.dil )  mode.dil ora-core ; 
: opc-08 ( php )  P push8 ; 
: opc-09 ( ora.# )  mode.imm ora-core PC+a ;
: opc-0A ( asl.a )  C> asl-core >C check-NZ.a ;  
: opc-0B ( phd )  D @  mask16 push16 ;

: opc-0C ( tsb )   ." 0C not coded yet" ; 

: opc-0D ( ora )  mode.abs.DBR ora-core ; 
: opc-0E ( asl )  mode.abs.DBR asl-mem ;  
: opc-0F ( ora.l )  mode.l ora-core ; 
: opc-10 ( bpl )  n-flag clear?  branch-if-true ; 
: opc-11 ( ora.diy )  mode.diy ora-core ; 
: opc-12 ( ora.di )  mode.di ora-core ; 
: opc-13 ( ora.siy )  mode.siy ora-core ;  

: opc-14 ( trb.d )   ." 14 not coded yet" ; 

: opc-15 ( ora.dx )  mode.dx ora-core ; 
: opc-16 ( asl.dx )  mode.dx asl-mem ;  
: opc-17 ( ora.dily )  mode.dily ora-core ;  
: opc-18 ( clc )  c-flag clear ; 
: opc-19 ( ora.y )   mode.y ora-core ; 
: opc-1A ( inc.a )   inc.accu ;

\ Does not affect flags; compare TXS. In emulation mode, hi byte is paranoided
\ to 01, native mode always copies full C to S
: opc-1B ( tcs )  e-flag set?  if  0100 A or  else  C @  then  S ! ;

: opc-1C ( trb )   ." 1C not coded yet" ; 

: opc-1D ( ora.x )  mode.x ora-core ; 
: opc-1E ( asl.x )  mode.x asl-mem ; 
: opc-1F ( ora.lx )  mode.lx ora-core ; 

\ STEP already increases the PC by one, so we only need to add one byte because
\ the address pushed is the last byte of the instruction
: opc-20 ( jsr )  PC @ 1+  push16 next2bytes  PC ! ;
: opc-21 ( and.dxi )  mode.dxi and-core ; 
: opc-22 ( jsr.l )  PC24  2 +  push24 next3bytes 24>PC24! ;
: opc-23 ( and.s )  mode.s and-core ; 
: opc-24 ( bit.d )  mode.d bit-core ; 
: opc-25 ( and.d )  mode.d and-core ; 
: opc-26 ( rol.d )  mode.d rol-mem ;  
: opc-27 ( and.dil )  mode.dil and-core ;  
: opc-28 ( plp )  pull8 >P ; 
: opc-29 ( and.# )  mode.imm and-core PC+a ; 
: opc-2A ( rol.a )  C> rol-core >C check-NZ.a ;  
: opc-2B ( pld )  pull16 dup check-Z dup check-N16  D ! ;
: opc-2C ( bit )  mode.abs.DBR bit-core ;  
: opc-2D ( and )  mode.abs.DBR and-core ; 
: opc-2E ( rol )  mode.abs.DBR rol-mem ; 
: opc-2F ( and.l )  mode.l and-core ; 
: opc-30 ( bmi )  n-flag set? branch-if-true ; 
: opc-31 ( and.diy )   mode.diy and-core ;
: opc-32 ( and.di )  mode.di and-core ; 
: opc-33 ( and.siy )  mode.siy and-core ; 
: opc-34 ( bit.dx )  mode.dx bit-core ; 
: opc-35 ( and.dx )  mode.dx and-core ; 
: opc-36 ( rol.dx )  mode.dx rol-mem ; 
: opc-37 ( and.dily )  mode.dily and-core ; 
: opc-38 ( sec )  c-flag set ;  
: opc-39 ( and.y )  mode.y and-core ; 
: opc-3A ( dec.a )  dec.accu ;
: opc-3B ( tsc )  S @  mask16 check-NZ.a ; 
: opc-3C ( bit.x )  mode.x bit-core ; 
: opc-3D ( and.x )   mode.x and-core ; 
: opc-3E ( rol.x )  mode.x rol-mem ; 
: opc-3F ( and.lx )  mode.lx and-core ; 

: opc-40 ( rti )   ." 40 not coded yet" ; 

: opc-41 ( eor.dxi )  mode.dxi eor-core ; 
: opc-42 ( wdm ) cr cr ." WARNING: WDM executed at " 
   PBR @ .mask8  PC @ .mask16  PC+1 ; 
: opc-43 ( eor.s )  mode.s eor-core ; 

: opc-44 ( mvp )   ." 44 not coded yet" ; 

: opc-45 ( eor.d )  mode.d eor-core ; 
: opc-46 ( lsr.d )   mode.d lsr-mem ; 
: opc-47 ( eor.dil )  mode.dil eor-core ;  
: opc-48 ( pha )  C> push.a ; 
: opc-49 ( eor.# )  mode.imm eor-core PC+a ;
: opc-4A ( lsr.a )  C> lsr-core >C check-NZ.a ;  
: opc-4B ( phk )  PBR @  push8 ;
: opc-4C ( jmp )  next2bytes  PC ! ;
: opc-4D ( eor )  mode.abs.DBR eor-core ; 
: opc-4E ( lsr )  mode.abs.DBR lsr-mem ; 
: opc-4F ( eor.l )  mode.l eor-core ; 
: opc-50 ( bvc )  v-flag clear? branch-if-true ; 
: opc-51 ( eor.diy )  mode.diy eor-core ;  
: opc-52 ( eor.di )  mode.di eor-core ; 
: opc-53 ( eor.siy )  mode.siy eor-core ; 

: opc-54 ( mvn )   ." 54 not coded yet" ; 

: opc-55 ( eor.dx )   mode.dx eor-core ; 
: opc-56 ( lsr.dx ) mode.dx lsr-mem ; 
: opc-57 ( eor.dily )  mode.dily eor-core ; 
: opc-58 ( cli )  i-flag clear ;  
: opc-59 ( eor.y )  mode.y eor-core ; 
: opc-5A ( phy )  Y @ push.xy ; 
: opc-5B ( tcd )  C @  mask16 dup check-NZ.a  D ! ;
: opc-5C ( jmp.l )  next3bytes 24>PC24! ; 
: opc-5D ( eor.x )  mode.x eor-core ; 
: opc-5E ( lsr.x )  mode.x lsr-mem ; 
: opc-5F ( eor.lx )  mode.lx eor-core ; 
: opc-60 ( rts )  pull16 1+  PC ! ;
: opc-61 ( adc.dxi )  mode.dxi adc-core ; 

: opc-62 ( phe.r )   ." 62 not coded yet" ; 

: opc-63 ( adc.s )  mode.s adc-core ; 
: opc-64 ( stz.d )  0 mode.d store.a ; 
: opc-65 ( adc.d )  mode.d adc-core ;  
: opc-66 ( ror.d )  mode.d ror-mem ; 
: opc-67 ( adc.dil )  mode.dil adc-core ; 
: opc-68 ( pla )  pull.a >C check-NZ.a ; 
: opc-69 ( adc.# )  mode.imm adc-core PC+a ; 
: opc-6A ( ror.a )  C> ror-core >C check-NZ.a ;  
: opc-6B ( rts.l )  pull24 1+  24>PC24! ; 
: opc-6C ( jmp.i )  mode.i  PC ! ; 
: opc-6D ( adc )  mode.abs.DBR adc-core ; 
: opc-6E ( ror )   mode.abs.DBR ror-mem ; 
: opc-6F ( adc.l )  mode.l adc-core ; 
: opc-70 ( bvs )  v-flag set? branch-if-true ;  
: opc-71 ( adc.diy )  mode.diy adc-core ; 
: opc-72 ( adc.di )  mode.di  adc-core ; 
: opc-73 ( adc.siy )  mode.siy adc-core ; 
: opc-74 ( stz.dx )  0 mode.dx store.a ; 
: opc-75 ( adc.dx)  mode.dx adc-core ; 
: opc-76 ( ror.dx )  mode.dx ror-mem ;
: opc-77 ( adc.dily )  mode.dily adc-core ; 
: opc-78 ( sei ) i-flag set ; 
: opc-79 ( adc.y )  mode.y adc-core ; 
: opc-7A ( ply )  pull.xy  Y !  check-NZ.y ;
: opc-7B ( tdc )  D @  mask16 dup check-NZ.a >C ; 
: opc-7C ( jmp.xi )  mode.xi  PC ! ; 
: opc-7D ( adc.x )  mode.x adc-core ; 
: opc-7E ( ror.x )   mode.x ror-mem ; 
: opc-7F ( adc.lx ) mode.lx adc-core ; 
: opc-80 ( bra )  takebranch ;
: opc-81 ( sta.dxi )  C> mode.dxi store.a ; 
: opc-82 ( bra.l )  next2bytes signextend.l  2 +  PC ! ; 
: opc-83 ( sta.s )  C> mode.s store.a ;  
: opc-84 ( sty.d )  Y @  mode.d store.xy ;
: opc-85 ( sta.d )  C> mode.d store.a ; 
: opc-86 ( stx.d )  X @  mode.d store.xy ;  
: opc-87 ( sta.dil )  C> mode.dil store.a ; 
: opc-88 ( dey )  Y @  1- mask.xy  Y !  check-NZ.y ;
: opc-89 ( bit.# )  C> mode.imm fetch.a and check-Z ; 
: opc-8A ( txa )  X @  >C check-NZ.a ; 
: opc-8B ( phb )  DBR @  push8 ; 
: opc-8C ( sty )  Y @  mode.abs.DBR store.xy ;
: opc-8D ( sta )  C>  mode.abs.DBR store.a ; 
: opc-8E ( stx )  X @  mode.abs.DBR store.xy ;
: opc-8F ( sta.l ) C> mode.l store.a ; 
: opc-90 ( bcc )  c-flag clear? branch-if-true ; 
: opc-91 ( sta.diy )  C> mode.diy store.a ;  
: opc-92 ( sta.di ) C> mode.di store.a ;
: opc-93 ( sta.siy )  C> mode.siy store.a ;  
: opc-94 ( sty.dx )  Y @  mode.dx store.xy ; 
: opc-95 ( sta.dx )  C> mode.dx store.a ; 
: opc-96 ( stx.dy )  X @  mode.dy store.xy ; 
: opc-97 ( sta.dily )  C> mode.dily store.a ;  
: opc-98 ( tya )  Y @  >C check-NZ.a ; 
: opc-99 ( sta.y )  C> mode.y store.a ; 

\ Does not alter flags; compare TCS 
: opc-9A ( txs ) 
   X @  e-flag set? if  \ emulation mode, hi byte paranoided to 01
      mask8  0100 or else
      x-flag set? if mask8 then  \ native mode, 8 bit X; hi byte is 00
   then  S ! ; 
            
: opc-9B ( txy )  X @  Y !  check-NZ.y ;
: opc-9C ( stz )  0 mode.abs.DBR store.a ; 
: opc-9D ( sta.x ) C> mode.x store.a ; 
: opc-9E ( stz.x )  0 mode.x store.a ;
: opc-9F ( sta.lx ) C> mode.lx store.a ;
: opc-A0 ( ldy.# )  mode.imm ldy-core PC+xy ;
: opc-A1 ( lda.dxi )  mode.dxi lda-core ; 
: opc-A2 ( ldx.# )  mode.imm ldx-core PC+xy ;
: opc-A3 ( lda.s )  mode.s lda-core ; 
: opc-A4 ( ldy.d )  mode.d ldy-core ; 
: opc-A5 ( lda.d )  mode.d lda-core ; 
: opc-A6 ( ldx.d )  mode.d ldx-core ; 
: opc-A7 ( lda.dil )  mode.dil lda-core ; 
: opc-A8 ( tay )  C @  mask.xy  Y !  check-NZ.y ; 
: opc-A9 ( lda.# ) mode.imm lda-core PC+a ; 
: opc-AA ( tax )  C @  mask.xy  X !  check-NZ.x ; 
: opc-AB ( plb )  pull8 dup check-NZ.8 DBR ! ; \ CHECK-NZ.8 consumes TOS
: opc-AC ( ldy )  mode.abs.DBR ldy-core ; 
: opc-AD ( lda )  mode.abs.DBR lda-core ;
: opc-AE ( ldx )  mode.abs.DBR ldx-core ; 
: opc-AF ( lda.l ) mode.l lda-core ; 
: opc-B0 ( bcs )  c-flag set? branch-if-true ;  
: opc-B1 ( lda.diy )   mode.diy lda-core ; 
: opc-B2 ( lda.di )  mode.di lda-core ; 
: opc-B3 ( lda.siy )  mode.siy lda-core ;  
: opc-B4 ( ldy.dx )  mode.dx ldy-core ; 
: opc-B5 ( lda.dx )  mode.dx lda-core ;
: opc-B6 ( ldx.dy )  mode.dy ldx-core ; 
: opc-B7 ( lda.dily )  mode.dily lda-core ; 
: opc-B8 ( clv ) v-flag clear ; 
: opc-B9 ( lda.y )  mode.y fetch.a check-NZ.a ;
: opc-BA ( tsx )  S @  xy16flag clear? if mask8 then  X !  check-NZ.x ;  
: opc-BB ( tyx )  Y @  X !  check-NZ.x ;
: opc-BC ( ldy.x )  mode.x ldy-core ; 
: opc-BD ( lda.x )  mode.x lda-core ;
: opc-BE ( ldx.y )  mode.y ldx-core ; 
: opc-BF ( lda.lx )  mode.lx lda-core ;
: opc-C0 ( cpy.# )  Y @  mode.imm cpxy-core ; 
: opc-C1 ( cmp.dxi )  C> mode.dxi cmp-core ; 

: opc-C2 ( rep ) \ TODO crude testing version, complete this for all flags
   cr ." WARNING: REP is incomplete, works only on m and x flags" cr
   next1byte 
   dup  20 = if a:16 m-flag clear else 
   dup  10 = if xy:16 x-flag clear else 
   dup  30 = if a:16 xy:16  x-flag clear  m-flag clear  then then then drop 
   PC+1 ; 

: opc-C3 ( cmp.s )  C> mode.s cmp-core ; 
: opc-C4 ( cpy.d )  Y @  mode.d cpxy-core ; 
: opc-C5 ( cmp.d )   C> mode.d cmp-core ;
: opc-C6 ( dec.d )  mode.d dec.mem ;
: opc-C7 ( cmp.dil )  C> mode.dil cmp-core ;  
: opc-C8 ( iny )  Y @  1+  mask.xy  Y !  check-NZ.y ;
: opc-C9 ( cmp.# ) C> mode.imm cmp-core PC+a ; 
: opc-CA ( dex )  X @  1- mask.xy  X !  check-NZ.x ;

: opc-CB ( wai ) \ TODO crude testing version, complete this for i-flag
   cr cr ." *** WAI instruction, halting processor ***" .state quit 
   cr ." WARNING: WAI not fully implemented" ; 

: opc-CC ( cpy )  Y @  mode.abs.DBR cpxy-core ; 
: opc-CD ( cmp )  C> mode.abs.DBR cmp-core ; 
: opc-CE ( dec )  mode.abs.DBR dec.mem ;
: opc-CF ( cmp.l )  C> mode.l cmp-core ; 
: opc-D0 ( bne )  z-flag clear? branch-if-true ; 
: opc-D1 ( cmp.diy )  C> mode.diy cmp-core ; 
: opc-D2 ( cmp.di )  C> mode.di cmp-core ; 
: opc-D3 ( cmp.siy )  C> mode.siy cmp-core ; 
: opc-D4 ( phe.d )  mode.d fetch16 push16 ; \ pp. 169, 373
: opc-D5 ( cmp.dx )  C> mode.dx cmp-core ; 
: opc-D6 ( dec.dx )  mode.dx dec.mem ; 
: opc-D7 ( cmp.dily )  C> mode.dily cmp-core ; 
: opc-D8 ( cld )  d-flag clear ;  
: opc-D9 ( cmp.y )  C> mode.y cmp-core ; 
: opc-DA ( phx )  X @  push.xy ;

: opc-DB ( stp )  cr cr ." *** STP instruction, halting processor ***" cr
   .state quit ; 

: opc-DC ( jmp.il )  mode.il 24>PC24! ; 
: opc-DD ( cmp.x )  C> mode.x cmp-core ;  
: opc-DE ( dec.x )  mode.x dec.mem ; 
: opc-DF ( cmp.lx ) C> mode.lx cmp-core ;  
: opc-E0 ( cpx.# )  X @  mode.imm cpxy-core ; 
: opc-E1 ( sbc.dxi )  mode.dxi sbc-core ; 

: opc-E2 ( sep ) \ TODO crude testing version, complete this for all flags
   cr ." WARNING: SEP is incomplete, works only on m and x flags" cr
   next1byte 
   dup  20 = if a:8  a16flag set  else 
   dup  10 = if xy:8  xy16flag set  else 
   dup  30 = if a:8 xy:8  a16flag set  xy16flag set  then then then drop 
   PC+1 ; 

: opc-E3 ( sbc.s )  mode.s sbc-core ; 
: opc-E4 ( cpx.d )  X @  mode.d cpxy-core ; 
: opc-E5 ( sbc.d )  mode.d sbc-core ; 
: opc-E6 ( inc.d )  mode.d inc.mem ; 
: opc-E7 ( sbc.dil )  mode.dil sbc-core ; 
: opc-E8 ( inx )  X @  1+  mask.xy  X !  check-NZ.x ;
: opc-E9 ( sbc.# )  mode.imm sbc-core PC+a ; 
: opc-EA ( nop ) ;

\ N and Z depend only on value in (new) A, regardless if the register is in 8 or 
\ 16 bit mode # TODO rewrite with C> etc
: opc-EB ( xba )  
   C @ dup  mask8  8 lshift  swap mask.B  8 rshift  dup check-NZ.8  or  C ! ; 

: opc-EC ( cpx )  X @ mode.abs.DBR cpxy-core ; 
: opc-ED ( sbc )  mode.abs.DBR sbc-core ;  
: opc-EE ( inc )  mode.abs.DBR inc.mem ; 
: opc-EF ( sbc.l )  mode.l sbc-core ;  
: opc-F0 ( beq )  z-flag set? branch-if-true ; 
: opc-F1 ( sbc.diy )  mode.diy sbc-core ;  
: opc-F2 ( sbc.di )  mode.di sbc-core ;  
: opc-F3 ( sbc.siy )  mode.siy sbc-core ; 
: opc-F4 ( phe.# )  next2bytes push16 PC+2 ;
: opc-F5 ( sbc.dx )  mode.dx sbc-core ; 
: opc-F6 ( inc.dx )  mode.dx inc.mem ; 
: opc-F7 ( sbc.dily )  mode.dily sbc-core ; 
: opc-F8 ( sed )  d-flag set ; 
: opc-F9 ( sbc.y )  mode.y sbc-core ; 
: opc-FA ( plx )  pull.xy  X !  check-NZ.x ; 
: opc-FB ( xce )  c-flag @  e-flag @   c-flag !  dup e-flag !
   if emulated else native then ; 
: opc-FC ( jsr.xi )  PC @ 1+  push16 mode.xi  PC ! ;
: opc-FD ( sbc.x )  mode.x  sbc-core ; 
: opc-FE ( inc.x )  mode.x inc.mem ; 
: opc-FF ( sbc.lx )  mode.lx  sbc-core ; 


\ ---- GENERATE OPCODE JUMP TABLE ----
\ Routine stores xt in table, offset is the opcode of the word in a cell. Use
\ "opc-jumptable <opcode> cells + @ execute" to call the opcode's word.
\ Assumes HEX
cr .( Generating opcode jump table ... ) 

: make-opc-jumptable ( -- )
   100 0 do
      i s>d <# # # [char] - hold [char] c hold [char] p hold [char] o hold #>
      find-name name>int ,
   loop ;

create opc-jumptable   make-opc-jumptable 


\ ---- INTERRUPTS ---- 
\ All vectors are 16 bit, access with fetch16 when given full 24 bit address
\ n in name is for native mode, e is for emulated mode
cr .( Setting up interrupts ...)

0ffe4 constant cop-n-v
0ffe6 constant brk-n-v  \ no such vector in emulated mode
0ffe8 constant abort-n-v  
0ffea constant nmi-n-v
0ffee constant irq-n-v

0fff4 constant cop-e-v
0fff8 constant abort-e-v  
0fffa constant nmi-e-v
0fffc constant reset-v   \ same for emulated and native
0fffe constant irq-e-v

: poweron ( -- ) \ TODO not used yet
   0000 D !  \ intiate Direct Page to zero (p. 155)
   \ TODO add everything else
   ; 

: reset-i ( -- )  \ p.201
   emulated
   \ TODO Set flags
   00 C !  00 X !   00 Y !  00 PBR !  00 DBR !  0000 D ! 
   reset-v fetch16  PC !  \ PC16 is TOS 
   \ RESET and power on set the MSB of the S to $01 but don't put the LSB in
   \ a defined state (http://forum.6502.org/viewtopic.php?f=4&t=2258) We use
   \ a weird but famous number for the initial value to make this clear
   S @  mask8  012a OR  S ! ;

: irq-i ( -- ) ." IRQ routine not programmed yet" ; \ TODO 
: nmi-i ( -- ) ." NMI routine not programmed yet" ; \ TODO 
: abort-i ( -- ) ." ABORT routine not programmed yet" ; \ TODO 
: brk-i ( -- ) ." BREAK routine not programmed yet" ; \ TODO 
: cop-i ( -- ) ." COP routine not programmed yet" ; \ TODO 


\ ---- MAIN CONTROL ----
\ Single-step through the program, or run emulation. To start at a given
\ memory location, save the bank number to PBK and the address to PC, then
\ type 'run' or 'step'

\ Increase PC before executing instruction so we are pointing at the
\ operand (if available)
: step ( -- )  opc-jumptable next1byte cells +  @  PC+1 execute ; 
: run ( -- )  begin step again ; 


\ ---- START EMULATION ----
cr cr .( All done. Bringing up machine.) cr 

\ TODO replace RESET-I with POWERON 
reset-i 
.state 

cr ." Machine ready. Type 'run' to start emulation, 'step' for single-step."

