\ A Crude 65816 Emulator 
\ Copyright 2015 Scot W. Stevenson <scot.stevenson@gmail.com>
\ Written with gforth 0.7
\ First version: 08. Jan 2015
\ This version: 27. May 2015  

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
cr .( Version pre-ALPHA  26. May 2015) 
cr .( Copyright 2015 Scot W. Stevenson <scot.stevenson@gmail.com> ) 
cr .( This program comes with ABSOLUTELY NO WARRANTY) cr


\ ---- DEFINITIONS ----
cr .( Defining general stuff ...)
hex

\ TODO make sure we even need this 
400 constant 1k       1k 8 * constant 8k       8k 2* constant 16k
16k 8 * constant 64k  64k 100 * constant 16M   64k constant bank 


\ ---- HARDWARE: CPU ----
cr .( Setting up CPU ... ) 

\ Names follow the convention from the WDC data sheet
\ Note the Accumulator (A/B/C) is top of stack (TOS) but we don't include 
\ this is the stack comments because it would drive us nuts
variable PC    \ program counter (16 bit) 
variable X     \ X register (8\16 bit)
variable Y     \ Y register (8\16 bit)
variable D     \ Direct register (Zero Page on 6502) (16 bit) 
variable P     \ Processor Status Register (8 bit)
variable S     \ Stack Pointer (8/16 bit)
variable DBR   \ Data Bank register ("B") (8 bit)
variable PBR   \ Program Bank register ("K") (8 bit)


\ ---- HELPER FUNCTIONS ----

\ mask addresses / hex numbers
: mask8 ( u -- u8 ) 0ff and ; 
: mask16 ( u -- u16 ) 0ffff and ; 
: mask24 ( u -- u24 ) 0ffffff and ; 
: maskmsb ( u16 -- u16 ) 0ff00 and ; \ used for B register 

\ return least, most significant byte of 16-bit number
: lsb ( u16 -- u8 )  mask8 ;
: msb ( u16 -- u8 )  maskmsb  8 rshift ;

\ 16 bit addresses and endian conversion. Assumes MSB is TOS
: 16>lsb/msb ( u16 -- lsb msb  )  dup lsb swap msb ; 
: lsb/msb>16 ( lsb msb -- u16 )  8 lshift or ; 

\ Convert various combinations to full 24 bit address. Assumes HEX 
: mem16>24  ( 65addr16 -- 65addr24 )  PBR @  10 lshift or ; 
: mem16/bank>24  ( 65addr16 bank -- 65addr24 )  10 lshift or ; 
: mem8>24  ( lsb msb bank -- 65addr24 )  -rot lsb/msb>16 swap mem16/bank>24 ; 

\ handle Program Counter
: PC+u ( u -- ) ( -- )   
   create ,
   does> @ PC +! ;

1 PC+u PC+1   2 PC+u PC+2   3 PC+u PC+3

\ Get full 24 bit current address (PC plus PBR) 
: PC24 ( -- 65addr24)  PC @  PBR @  mem16/bank>24 ; 

\ Advance PC depending on what size our registers are
defer PC+fetch.a   defer PC+fetch.xy

\ Rescue B part of Accumulator. Used by AND for example
: rescue.b ( C u -- B C u )  over maskmsb -rot ; 


\ ---- MEMORY ----
\ All accesses to memory are always full 24 bit. Stack follows little-endian
\ format with bank on top, then msb and lsb ( lsb msb bank -- ) 
cr .( Creating memory ...) 

\ We just allot the whole possible memory range. Note that this will fail
\ unless you called Gforth with "-m 1G" or something of that size like you
\ were told in the MANUAL.txt
create memory 16M allot

: loadrom ( 65addr24 addr u -- )
   r/o open-file drop            ( 65addr fileid ) 
   slurp-fid                     ( 65addr addr u ) 
   rot  memory +  swap           ( addr 65addrROM u ) 
   move ;  

\ load ROM files into memory
cr .( Loading ROM files to memory ...) 
include config.fs  

\ Fetch from memory 
defer fetch.a   defer fetch.xy

\ Get one byte or double byte out of memory. Double bytes assume 
\ little-endian storage in memory but returns it to the Forth data 
\ stack in "normal" big endian format
: fetch8  ( 65addr24 -- u8 )  memory +  c@ ; 
: fetch16  ( 65addr24 -- u16 )  dup fetch8  swap 1+  fetch8  lsb/msb>16 ; 
: fetch24  ( 65addr24 -- u24 ) ." fetch24 not coded yet" ; \ TODO 

\ Store to memory 
defer store.a   defer store.xy

: store8 ( u8 65addr24 -- ) memory +  c! ;
: store16 ( u16 65addr24 -- ) \ store LSB first
   2dup swap lsb swap store8  swap msb swap 1+ store8 ; 

\ Read current byte in stream. Note we use PBR, not DBR
: fetch1byte ( -- u8 )  PC24 fetch8 ; 
: fetch2bytes ( -- u16 )  PC24 fetch16 ; 


\ ---- FLAGS ----
\ All flags are fully formed Forth flags (one cell large) 
cr .( Setting up flag routines ... ) 

variable n-flag   variable v-flag   variable m-flag
variable x-flag   variable b-flag   variable d-flag 
variable i-flag   variable z-flag   variable c-flag 
variable e-flag 

\ make flag code easier for humans to read
: set?  ( addr -- f )  @ ;  
: clear?  ( addr -- f )  @ invert ;
: set  ( addr -- )  true swap ! ; 
: clear  ( addr -- )  false swap ! ; 

defer status-r

\ construct status byte for emulation mode 
: status-r8  ( -- u8 ) 
   n-flag @  80 and 
   v-flag @  40 and +
\   bit 5 is empty TODO see if this needs to be set to zero 
   b-flag @  10 and +
   d-flag @  08 and +
   i-flag @  04 and +
   z-flag @  02 and +
   c-flag @  01 and + ; 

\ construct status byte for native mode
: status-r16  ( -- u8 ) 
   n-flag @  80 and 
   v-flag @  40 and +
   m-flag @  20 and +
   x-flag @  10 and +
   d-flag @  08 and +
   i-flag @  04 and +
   z-flag @  02 and +
   c-flag @  01 and + ; 


\ ---- TEST AND SET FLAGS ----

\ Note that checks that work on A/C have their own DUP in violation of 
\ usual Forth convention. Assumes HEX
defer check-N.a   defer check-N.x   defer check-N.y

: check-N8 ( n -- )  80 and  if n-flag set  else  n-flag clear  then ;
: check-N16 ( n -- )  8000 and  if n-flag set  else  n-flag clear  then ;
: check-N.a8 ( n -- ) dup check-N8 ; 
: check-N.a16 ( n -- ) dup check-N16 ; 
: check-N.x8 ( n -- )  X @  check-N8 ; 
: check-N.x16 ( n -- )  X @  check-N16 ; 
: check-N.y8 ( n -- ) Y @  check-N8 ; 
: check-N.y16 ( n -- ) Y @ check-N16 ;   

defer check-Z.a
: check-Z ( n -- )  if  z-flag clear  else  z-flag set  then ; 
: check-Z.a8 ( -- ) dup 0ff AND check-Z ; 
: check-Z.a16 ( -- ) dup check-Z  ; 
: check-Z.x ( -- )  X @  check-Z ;
: check-Z.y ( -- )  Y @  check-Z ; 
   
\ common combinations
: check-NZ.a ( -- )  check-N.a  check-Z.a ; 
: check-NZ.x ( -- )  check-N.x  check-Z.x ; 
: check-NZ.y ( -- )  check-N.y  check-Z.y ; 
    

\ ----- ALU COMMANDS ---- 

defer and.a

: and8  ( u8 -- u8 )  rescue.b  and mask8  or ;  
: and16 ( u16 -- u16 )  and mask16 ; \ paranoid 


\ ---- REGISTER MODE SWITCHES ----

\ Switch accumulator 8<->16 bit (p. 51 in Manual)
\ Remember A is TOS 
: a->16  ( -- )  
   ['] fetch16 is fetch.a 
   ['] store16 is store.a
   ['] PC+2 is PC+fetch.a
   ['] check-N.a16 is check-N.a
   ['] check-Z.a16 is check-Z.a
   ['] and16 is and.a
   m-flag clear ; 

: a->8 ( -- )  
   ['] fetch8 is fetch.a 
   ['] store8 is store.a
   ['] PC+1 is PC+fetch.a
   ['] check-N.a8 is check-N.a
   ['] check-Z.a8 is check-Z.a
   ['] and8 is and.a
   m-flag set ;

\ Switch X and Y 8<->16 bit (p. 51 in Manual) 
: xy->16  ( -- )  
   ['] fetch16 is fetch.xy 
   ['] store16 is store.xy
   ['] PC+2 is PC+fetch.xy
   ['] check-N.x16 is check-N.x
   ['] check-N.y16 is check-N.y
   X @  00FF AND  X !   Y @  00FF AND  Y !  \ paranoid
   x-flag clear ; 

: xy->8 ( -- )  
   ['] fetch8 is fetch.xy
   ['] store8 is store.xy
   ['] PC+1 is PC+fetch.xy
   ['] check-N.x8 is check-N.x
   ['] check-N.y8 is check-N.y
   X @  00FF AND  X !   Y @  00FF AND  Y !  
   x-flag set ; 

\ switch processor modes (native/emulated). There doesn't seem to be a good
\ verb for "native" like "emulate", so we're "going" 
: go-native ( -- )  
   e-flag clear
   ['] status-r16 is status-r 

   \ TODO set stack pointer to 16 bits
   \ TODO set direct page to 16 zero page
   ; 

: go-emulated ( -- )  
   ['] status-r8 is status-r 
   a->8   xy->8  
   e-flag set   
   S @  00FF AND  0100 OR  S ! \ stack pointer to 0100
   0 D !  \ direct page register initialized to zero 
   \ TODO clear b-flag ?
   \ TODO PBR and DBR ?
   \ TODO D-Flag?
   ; 


\ ---- ADDRESSING MODES --- 
cr .( Defining addressing modes ...) 

\ Note that the opcodes for Absolute Mode have no suffix, but we use
\ "mode.abs" for clarity. Note that not all modes are listed here, as 
\ some are easier to code by hand. 

: mode.abs ( -- 65addr24 ) fetch2bytes mem16>24 PC+2 ; \ absolute
: mode.x  ( -- 65addr24 ) mode.abs  x @  + ; \ absolute indexed x \ TODO TESTME
: mode.y  ( -- 65addr24 ) mode.abs  y @  + ; \ absolute indexed y \ TODO TESTME


\ ---- OUTPUT FUNCTIONS ----

\ Print byte as bits, does not add space, returns as HEX
: .8bits ( u -- ) 
   2 base !  s>d <# # # # # # # # # #> type  hex ; 

\ Format numbers to two, four places, assumes HEX
: .mask8 ( n -- addr u )  s>d <# # # #> type space ; 
: .mask16 ( n -- addr u )  s>d <# # # # # #> type space ; 

\ Print state of machine 
: .state ( -- )
   cr  e-flag set? if
      ."  PC   K  BA    X    Y    S    D   B NV-BDIZC" else
      ."  PC   K   C    X    Y    S    D   B NVMXDIZC" then cr 
 \    ." xxxx xx xxxx xxxx xxxx xxxx xxxx xx xxxxxxxx" 

   PC @ .mask16   PBR @ .mask8   dup .mask16   X @ .mask16 
   Y @ .mask16    S @ .mask16    D @ .mask16   DBR @ .mask8
   status-r .8bits  space 
   e-flag set? if ." emulated" else ." native" then  cr ; 

   
\ ---- OPCODE ROUTINES ----
cr .( Defining opcode routines ... ) 

: opc-00 ( brk )   ." BRK, halting CPU (ALPHA only)" .state quit ; \ testing only
: opc-01 ( ora.dxi )   ." 01 not coded yet" ; 
: opc-02 ( cop )   ." 02 not coded yet" ; 
: opc-03 ( ora.s )   ." 03 not coded yet" ; 
: opc-04 ( tsb.d )  ." 04 not coded yet" ; 
: opc-05 ( ora.d )   ." 05 not coded yet" ; 
: opc-06 ( asl.d )  ." 06 not coded yet" ; 
: opc-07 ( ora.dil )   ." 07 not coded yet" ; 
: opc-08 ( php )   ." 08 not coded yet" ; 
: opc-09 ( ora.# )   ." 09 not coded yet" ; 
: opc-0A ( asl.a )   ." 0A not coded yet" ; 
: opc-0B ( phd )   ." 0B not coded yet" ; 
: opc-0C ( tsb )   ." 0C not coded yet" ; 
: opc-0D ( ora )   ." 0D not coded yet" ; 
: opc-0E ( asl )   ." 0E not coded yet" ; 
: opc-0F ( ora.l )   ." 0F not coded yet" ; 
: opc-10 ( bpl )   ." 10 not coded yet" ; 
: opc-11 ( ora.diy )   ." 11 not coded yet" ; 
: opc-12 ( ora.di )   ." 12 not coded yet" ; 
: opc-13 ( ora.siy )   ." 13 not coded yet" ; 
: opc-14 ( trb.d )   ." 14 not coded yet" ; 
: opc-15 ( ora.dx )   ." 15 not coded yet" ; 
: opc-16 ( asl.dx )   ." 16 not coded yet" ; 
: opc-17 ( ora.dily )   ." 17 not coded yet" ; 
: opc-18 ( clc )  c-flag clear ; 
: opc-19 ( ora.y )   ." 19 not coded yet" ; 
: opc-1A ( inc.a )   ." 1A not coded yet" ; 
: opc-1B ( tcs ) ." 1B not coded yet" ; 
: opc-1C ( trb )   ." 1C not coded yet" ; 
: opc-1D ( ora.x )   ." 1D not coded yet" ; 
: opc-1E ( asl.x )   ." 1E not coded yet" ; 
: opc-1F ( ora.lx )   ." 1F not coded yet" ; 
: opc-20 ( jsr )   ." 20 not coded yet" ; 
: opc-21 ( and.dxi )   ." 21 not coded yet" ; 
: opc-22 ( jsr.l )   ." 22 not coded yet" ; 
: opc-23 ( and.s )   ." 23 not coded yet" ; 
: opc-24 ( bit.d )   ." 24 not coded yet" ; 
: opc-25 ( and.d )   ." 25 not coded yet" ; 
: opc-26 ( rol.d )   ." 26 not coded yet" ; 
: opc-27 ( and.dil )   ." 27 not coded yet" ; 
: opc-28 ( plp )   ." 28 not coded yet" ; 
: opc-29 ( and.# )   ." 29 not coded yet" ; 
: opc-2A ( rol )   ." 2A not coded yet" ; 
: opc-2B ( pld )   ." 2B not coded yet" ; 
: opc-2C ( bit )   ." 2C not coded yet" ; 
: opc-2D ( and )  mode.abs  fetch.a  and.a  check-NZ.a ;  \ TODO TESTME
: opc-2E ( rol )   ." 2E not coded yet" ; 
: opc-2F ( and.l )   ." 2F not coded yet" ; 
: opc-30 ( bmi )   ." 30 not coded yet" ; 
: opc-31 ( and.diy )   ." 31 not coded yet" ; 
: opc-32 ( and.di )   ." 32 not coded yet" ; 
: opc-33 ( and.siy )   ." 33 not coded yet" ; 
: opc-34 ( bit.dx )   ." 34 not coded yet" ; 
: opc-35 ( and.dx )   ." 35 not coded yet" ; 
: opc-36 ( rol.dx )   ." 36 not coded yet" ; 
: opc-37 ( and.dy )   ." 37 not coded yet" ; 
: opc-38 ( sec )  c-flag set ;  
: opc-39 ( and.y )   ." 39 not coded yet" ; 
: opc-3A ( dec.a )   ." 3A not coded yet" ; 
: opc-3B ( tsc )  drop  S @  mask16  check-NZ.a ; \ TODO check N behavior
: opc-3C ( bit.x )   ." 3C not coded yet" ; 
: opc-3D ( and.x )   ." 3D not coded yet" ; 
: opc-3E ( rol.x )   ." 3E not coded yet" ; 
: opc-3F ( and.lx )   ." 3F not coded yet" ; 
: opc-40 ( rti )   ." 40 not coded yet" ; 
: opc-41 ( eor.dxi )   ." 41 not coded yet" ; 
: opc-42 ( wdm ) ." WARNING: WDM executed."  PC+1 ; 
: opc-43 ( eor.s )   ." 43 not coded yet" ; 
: opc-44 ( mvp )   ." 44 not coded yet" ; 
: opc-45 ( eor.d )   ." 45 not coded yet" ; 
: opc-46 ( lsr.d )   ." 46 not coded yet" ; 
: opc-47 ( eor.dil )   ." 47 not coded yet" ; 
: opc-48 ( pha )   ." 48 not coded yet" ; 
: opc-49 ( eor.# )   ." 49 not coded yet" ; 
: opc-4A ( lsr.a )   ." 4A not coded yet" ; 
: opc-4B ( phk )   ." 4B not coded yet" ; 
: opc-4C ( jmp )  fetch2bytes  PC ! ;
: opc-4D ( eor )   ." 4D not coded yet" ; 
: opc-4E ( lsr )   ." 4E not coded yet" ; 
: opc-4F ( eor.l )   ." 4F not coded yet" ; 
: opc-50 ( bvc )   ." 50 not coded yet" ; 
: opc-51 ( eor.diy )   ." 51 not coded yet" ; 
: opc-52 ( eor.di )   ." 52 not coded yet" ; 
: opc-53 ( eor.siy )   ." 53 not coded yet" ; 
: opc-54 ( mvn )   ." 54 not coded yet" ; 
: opc-55 ( eor.dx )   ." 55 not coded yet" ; 
: opc-56 ( lsr.dx )   ." 56 not coded yet" ; 
: opc-57 ( eor.dy )   ." 57 not coded yet" ; 
: opc-58 ( cli )  i-flag clear ;  
: opc-59 ( eor.y )   ." 59 not coded yet" ; 
: opc-5A ( phy )   ." 5A not coded yet" ; 
: opc-5B ( tcd )  dup  mask16  D ! check-NZ.a ; \ mask16 is paranoid
: opc-5C ( jmp.l )   ." 5C not coded yet" ; 
: opc-5D ( eor.dx )   ." 5D not coded yet" ; 
: opc-5E ( lsr.x )   ." 5E not coded yet" ; 
: opc-5F ( eor.lx )   ." 5F not coded yet" ; 
: opc-60 ( rts )   ." 60 not coded yet" ; 
: opc-61 ( adc.dxi )   ." 61 not coded yet" ; 
: opc-62 ( per )   ." 62 not coded yet" ; 
: opc-63 ( adc.s )   ." 63 not coded yet" ; 
: opc-64 ( stz.d )   ." 64 not coded yet" ; 
: opc-65 ( adc.d )   ." 65 not coded yet" ; 
: opc-66 ( ror.d )   ." 66 not coded yet" ; 
: opc-67 ( adc.dil )   ." 67 not coded yet" ; 
: opc-68 ( pla )   ." 68 not coded yet" ; 
: opc-69 ( adc.# )   ." 69 not coded yet" ; 
: opc-6A ( ror.a )   ." 6A not coded yet" ; 
: opc-6B ( rtl )   ." 6B not coded yet" ; 
: opc-6C ( jmp.i )   ." 6C not coded yet" ; 
: opc-6D ( adc )   ." 6D not coded yet" ; 
: opc-6E ( ror )   ." 6E not coded yet" ; 
: opc-6F ( adc.l )   ." 6F not coded yet" ; 
: opc-70 ( bvs )   ." 70 not coded yet" ; 
: opc-71 ( adc.diy )   ." 71 not coded yet" ; 
: opc-72 ( adc.di )   ." 72 not coded yet" ; 
: opc-73 ( adc.siy )   ." 73 not coded yet" ; 
: opc-74 ( stx.dx )   ." 74 not coded yet" ; 
: opc-75 ( adc.dx)   ." 75 not coded yet" ; 
: opc-76 ( ror.dx )   ." 76 not coded yet" ; 
: opc-77 ( adc.dy )   ." 77 not coded yet" ; 
: opc-78 ( sei ) i-flag set ; 
: opc-79 ( adc.y )   ." 79 not coded yet" ; 
: opc-7A ( ply )   ." 7A not coded yet" ; 
: opc-7B ( tdc )  drop  D @  mask16  check-NZ.a ;  \ TODO TESTME, mask16 paranoid
: opc-7C ( jmp.xi )   ." 7C not coded yet" ; 
: opc-7D ( adc.x )   ." 7D not coded yet" ; 
: opc-7E ( ror.x )   ." 7E not coded yet" ; 
: opc-7F ( adc.lx )   ." 7F not coded yet" ; 
: opc-80 ( bra )   ." 80 not coded yet" ; 
: opc-81 ( sta.dxi )   ." 81 not coded yet" ;
: opc-82 ( brl )   ." 82 not coded yet" ; 
: opc-83 ( sta.s )   ." 83 not coded yet" ; 
: opc-84 ( sty.d )   ." 84 not coded yet" ; 
: opc-85 ( sta.d )   ." 85 not coded yet" ; 
: opc-86 ( stx.d )   ." 86 not coded yet" ; 
: opc-87 ( sta.dil )   ." 87 not coded yet" ; 
: opc-88 ( dey )   ." 88 not coded yet" ; 
: opc-89 ( bit.# )   ." 89 not coded yet" ; 
: opc-8A ( txa )  m-flag clear? if  drop  X @  else
   maskmsb  X @  mask8 or  then ; \ TODO TESTME
: opc-8B ( phb )   ." 8B not coded yet" ; 
: opc-8C ( sty )  Y @  mode.abs  store.xy ;
: opc-8D ( sta )  dup  mode.abs  store.a ; 
: opc-8E ( stx )  X @  mode.abs  store.xy ;
: opc-8F ( sta.l )   ." 8F not coded yet" ; 
: opc-90 ( bcc )   ." 90 not coded yet" ; 
: opc-91 ( sta.diy )   ." 91 not coded yet" ; 
: opc-92 ( sta.di )   ." 92 not coded yet" ; 
: opc-93 ( sta.siy )   ." 93 not coded yet" ; 
: opc-94 ( sty.dx )   ." 94 not coded yet" ; 
: opc-95 ( sta.dx )   ." 95 not coded yet" ; 
: opc-96 ( stx.dy )   ." 96 not coded yet" ; 
: opc-97 ( sta.dily )   ." 97 not coded yet" ; 
: opc-98 ( tya )  m-flag clear? if  drop  Y @  else
   maskmsb  Y @  mask8 or  then ; \ TODO TESTME
: opc-99 ( sta.y )   ." 99 not coded yet" ; 
: opc-9A ( txs )   ." 9A not coded yet" ; 
: opc-9B ( txy )  X @  Y !  check-NZ.y ;
: opc-9C ( stz )  0  mode.abs  store.a ;  \ TODO TESTME
: opc-9D ( sta.x )   ." 9D not coded yet" ; 
: opc-9E ( stz.x )   ." 9E not coded yet" ; 
: opc-9F ( sta.lx )   ." 9F not coded yet" ; 
: opc-A0 ( ldy.# )  PC24 fetch.xy  Y !  check-NZ.y  PC+fetch.xy ;
: opc-A1 ( lda.dxi )   ." A1 not coded yet" ; 
: opc-A2 ( ldx.# )  PC24 fetch.xy  X !  check-NZ.x  PC+fetch.xy ;
: opc-A3 ( lda.s )   ." A3 not coded yet" ; 
: opc-A4 ( ldy.d )   ." A4 not coded yet" ; 
: opc-A5 ( lda.d )   ." A5 not coded yet" ; 
: opc-A6 ( ldx.d )   ." A6 not coded yet" ; 
: opc-A7 ( lda.dil )   ." A7 not coded yet" ; 
: opc-A8 ( tay )  dup x-flag set? if mask8 else mask16 then  Y !  check-NZ.y ; 
: opc-A9 ( lda.# ) drop  PC24 fetch.a  check-NZ.a  PC+fetch.a ; 
: opc-AA ( tax )  dup x-flag set? if mask8 else mask16 then  X !  check-NZ.x ; 
: opc-AB ( plb )   ." AB not coded yet" ; 
: opc-AC ( ldy )   ." AC not coded yet" ; 
: opc-AD ( lda )   ." AD not coded yet" ; 
: opc-AE ( ldx )   ." AE not coded yet" ; 
: opc-AF ( lda.l )   ." AF not coded yet" ; 
: opc-B0 ( bcs )   ." B0 not coded yet" ; 
: opc-B1 ( lda.diy )   ." B1 not coded yet" ; 
: opc-B2 ( lda.di )   ." B2 not coded yet" ; 
: opc-B3 ( lda.siy )   ." B3 not coded yet" ; 
: opc-B4 ( ldy.dx )   ." B4 not coded yet" ; 
: opc-B5 ( lda.dx )   ." B5 not coded yet" ; 
: opc-B6 ( ldx.dy )   ." B6 not coded yet" ; 
: opc-B7 ( lda.dy )   ." B7 not coded yet" ; 
: opc-B8 ( clv ) v-flag clear ; 
: opc-B9 ( lda.y )   ." B9 not coded yet" ; 
: opc-BA ( tsx )  S @  x-flag set? if 00ff and else mask16 then  
   X !  check-NZ.x ;  \ mask16 is paranoid TODO TESTME
: opc-BB ( tyx )  Y @  X !  check-NZ.x ;
: opc-BC ( ldy.x )   ." BC not coded yet" ; 
: opc-BD ( lda.x )   ." BD not coded yet" ; 
: opc-BE ( ldx.y )   ." BE not coded yet" ; 
: opc-BF ( lda.lx )   ." BF not coded yet" ; 
: opc-C0 ( cpy.# )   ." C0 not coded yet" ; 
: opc-C1 ( cmp.dxi )   ." C1 not coded yet" ; 
: opc-C2 ( rep ) \ TODO crude testing version, complete this for all flags
   cr ." WARNING: REP is incomplete, works only on m and x flags" cr
   fetch1byte 
   dup  20 = if a->16 else 
   dup  10 = if xy->16  else 
   dup  30 = if a->16 xy->16 then then then drop 
   PC+1 ; 
: opc-C3 ( cmp.s )   ." C3 not coded yet" ; 
: opc-C4 ( cpy.d )   ." C4 not coded yet" ; 
: opc-C5 ( cmp.d )   ." C5 not coded yet" ; 
: opc-C6 ( dec.d )   ." C6 not coded yet" ; 
: opc-C7 ( cmp.dil )   ." C7 not coded yet" ; 
: opc-C8 ( iny )   ." C8 not coded yet" ; 
: opc-C9 ( cmp.# )   ." C9 not coded yet" ; 
: opc-CA ( dex )   ." CA not coded yet" ; 
: opc-CB ( wai ) \ TODO crude testing version, complete this for i-flag
   cr ." WARNING: WAI not fully implemented" 
   cr ." WAI instruction, halting processor" cr .state quit ; 
: opc-CC ( cpy )   ." CC not coded yet" ; 
: opc-CD ( cmp )   ." CD not coded yet" ; 
: opc-CE ( dec )   ." dec not coded yet" ; 
: opc-CF ( cmp.l )   ." CF not coded yet" ; 
: opc-D0 ( bne )   ." D0 not coded yet" ; 
: opc-D1 ( cmp.diy )   ." D1 not coded yet" ; 
: opc-D2 ( cmp.di )   ." D2 not coded yet" ; 
: opc-D3 ( cmp.siy )   ." D3 not coded yet" ; 
: opc-D4 ( pei )   ." D4 not coded yet" ; 
: opc-D5 ( cmp.dx )   ." D5 not coded yet" ; 
: opc-D6 ( dec.dx )   ." D6 not coded yet" ; 
: opc-D7 ( cmp.dy )   ." D7 not coded yet" ; 
: opc-D8 ( cld )  d-flag clear ;  
: opc-D9 ( cmp.y )   ." D9 not coded yet" ; 
: opc-DA ( phx )   ." DA not coded yet" ; 
: opc-DB ( stp )  cr ." STP instruction, halting processor" cr .state quit ; 
: opc-DC ( jmp.il )   ." DC not coded yet" ; 
: opc-DD ( cmp.x )   ." DD not coded yet" ; 
: opc-DE ( dec.x )   ." DE not coded yet" ; 
: opc-DF ( cmp.lx )   ." DF not coded yet" ; 
: opc-E0 ( cpx.# )   ." E0 not coded yet" ; 
: opc-E1 ( sbc.dxi )  ." E1 not coded yet" ; 
: opc-E2 ( sep ) \ TODO crude testing version, complete this for all flags
   cr ." WARNING: SEP is incomplete, works only on m and x flags" cr
   fetch1byte 
   dup  20 = if a->8 else 
   dup  10 = if xy->8  else 
   dup  30 = if a->8 xy->8 then then then drop 
   PC+1 ; 
: opc-E3 ( sbc.s )   ." E3 not coded yet" ; 
: opc-E4 ( cpx.d )   ." E4 not coded yet" ; 
: opc-E5 ( sbc.d )   ." E5 not coded yet" ; 
: opc-E6 ( inc.d )   ." E6 not coded yet" ; 
: opc-E7 ( sbc.dil )   ." E7 not coded yet" ; 
: opc-E8 ( inx )   ." E8 not coded yet" ; 
: opc-E9 ( sbc.# )   ." E9 not coded yet" ; 
: opc-EA ( nop ) ;
: opc-EB ( xba ) dup msb swap  lsb 8 lshift  or  check-NZ.a ;  \ TODO TESTME
: opc-EC ( cpx )   ." EC not coded yet" ; 
: opc-ED ( sbc )   ." ED not coded yet" ; 
: opc-EE ( inc )   ." EE not coded yet" ; 
: opc-EF ( sbc.l )   ." EF not coded yet" ; 
: opc-F0 ( beq )   ." F0 not coded yet" ; 
: opc-F1 ( sbc.diy )   ." F1 not coded yet" ; 
: opc-F2 ( sbc.di )   ." F2 not coded yet" ; 
: opc-F3 ( sbc.siy )   ." F3 not coded yet" ; 
: opc-F4 ( pea )   ." F4 not coded yet" ; 
: opc-F5 ( sbc.dx )   ." F5 not coded yet" ; 
: opc-F6 ( inc.dx )   ." F6 not coded yet" ; 
: opc-F7 ( sbc.dily )   ." F7 not coded yet" ; 
: opc-F8 ( sed )  d-flag set ; 
: opc-F9 ( sbc.y )   ." F9 not coded yet" ; 
: opc-FA ( plx )   ." FA not coded yet" ; 
: opc-FB ( xce )  e-flag @  c-flag @  swap  c-flag !  dup e-flag !
   if go-emulated else go-native then ; 
: opc-FC ( jsr.xi )   ." FC not coded yet" ; 
: opc-FD ( sbc.x )   ." FD not coded yet" ; 
: opc-FE ( inc.x )   ." FE not coded yet" ; 
: opc-FF ( sbc.lx )   ." FF not coded yet" ; 


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

: reset-i ( -- ) 
   go-emulated
   00  \ TOS is A
   \ TODO Set flags
   00 X !   00 Y !  00 PBR !  00 DBR ! 
   reset-v fetch16  PC ! ; 

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
: step ( -- )  opc-jumptable fetch1byte cells +  @    PC+1   execute ; 
: run ( -- )   begin step again ; 

\ ---- START EMULATION ----
cr .( All done.) cr 

reset-i 
.state 

cr ." Machine ready. Type 'run' to start emulation, 'step' for single-step."

