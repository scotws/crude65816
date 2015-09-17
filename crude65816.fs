\ A Crude 65816 Emulator 
\ Copyright 2015 Scot W. Stevenson <scot.stevenson@gmail.com>
\ Written with gforth 0.7
\ First version: 08. Jan 2015
\ This version: 17. Sep 2015 

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
cr .( Version pre-ALPHA  17. Sep 2015)  
cr .( Copyright 2015 Scot W. Stevenson <scot.stevenson@gmail.com> ) 
cr .( This program comes with ABSOLUTELY NO WARRANTY) cr


\ ---- DEFINITIONS ----
cr .( Defining general stuff ...)
hex

\ TODO make sure we even need any of this 
400 constant 1k       1k 8 * constant 8k       8k 2* constant 16k
16k 8 * constant 64k  64k 100 * constant 16M


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
defer mask.xy
: mask8 ( u -- u8 ) 0ff and ; 
: mask16 ( u -- u16 ) 0ffff and ; 
: mask24 ( u -- u24 ) 0ffffff and ; 
: mask.B ( u16 -- u16 ) 0ff00 and ; \ used for B register 

\ return least, most significant byte of 16-bit number
: lsb ( u -- u8 )  mask8 ;
: msb ( u -- u8 )  mask.B  8 rshift ;
: bank ( u -- u8 )  10 rshift  mask8 ; \ assumes HEX

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

\ Make 24 bit value the new 24 bit address
: 24>PC24  ( 65addr -- )  24>lsb/msb/bank  PBR !  lsb/msb>16  PC ! ; 

\ Convert various combinations to full 24 bit address. Assumes HEX 
: mem16/bank>24  ( 65addr16 bank -- 65addr24 )  10 lshift or ; 
: mem16/PBR>24  ( 65addr16 -- 65addr24 )  PBR @  mem16/bank>24 ; 
: mem16/DBR>24  ( 65addr16 -- 65addr24 )  DBR @  mem16/bank>24 ; 
: lsb/msb/bank>24  ( lsb msb bank -- 65addr24 )  
   -rot lsb/msb>16 swap mem16/bank>24 ; 

\ Handle wrapping for 8-bit and 16-bit additions TODO TESTME 
\ TODO See if we want to changed the c-flag directly
: wrap8&c ( u -- u8 f )  dup mask8 swap  mask.B  0= invert ; 
: wrap16&c ( u -- u16 f ) dup mask16 swap  0ff0000 and  0= invert ; 

\ handle Program Counter
: PC+u ( u -- ) ( -- )   
   create ,
   does> @ PC +! ;

1 PC+u PC+1   2 PC+u PC+2   3 PC+u PC+3

\ Get full 24 bit current address (PC plus PBR) 
: PC24 ( -- 65addr24)  PC @  PBR @  mem16/bank>24 ; 

\ Advance PC depending on what size our registers are
defer PC+fetch.a   
defer PC+fetch.xy

\ Rescue B part of Accumulator. Used by AND for example
: rescue.b ( C u -- B C u )  over mask.B -rot ; 


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

\ Fetch from memory 
\ Note that FETCH.A assumes we are going to replace A (the TOS) as in the LDA
\ instructions. If we keep A (AND, ORA, EOR, etc), we need to include a DUP
defer fetch.a   
defer fetch.xy

\ Get one byte, a double byte, or three bytes from any 24-bit memory address.
\ Double bytes assume  little-endian storage in memory but returns it to the
\ Forth data stack in "normal" big endian format. Note we don't advance the PC
\ here so we can use these routines with stuff like stack manipulations

\ FETCH8 includes the check for special addresses (I/O chips, etc) so all other
\ store words should be based on it
: fetch8  ( 65addr24 -- u8 )  
   special-fetch?  dup 0= if     ( 65addr24 0|xt)
      drop  memory +  c@  else
      nip execute  then ; 
: fetch16  ( 65addr24 -- u16 )  dup fetch8  swap 1+ fetch8  lsb/msb>16 ; 
: fetch24  ( 65addr24 -- u24 )  
   dup fetch8  over 1+ fetch8  
   rot 2 + fetch8  
   lsb/msb/bank>24 ; 

\ We need special FETCH commands for A because we have the current value TOS and
\ we need to protect B if A is 8 bits wide
: fetch8.a ( u 65addr24 -- u16 )  fetch8  swap  0ff00 and  or ; 
: fetch16.a  ( u 65addr24 -- u16 )  nip fetch16 ; 

\ Store to memory 
defer store.a   
defer store.xy

\ STORE8 includes the check for special addresses (I/O chips, etc) so all other
\ store words should be based on it
: store8 ( u8 65addr24 -- ) 
   special-store?  dup 0= if     ( u8 65addr24 0|xt)
      drop  memory +  c!  else
      nip execute  then ; 

: store16 ( u16 65addr24 -- ) \ store LSB first
   2dup swap lsb swap store8  swap msb swap 1+ store8 ; 
: store24 ( u24 65addr24 -- ) 
\ TODO rewrite 
   >r               ( u24  R: 65addr ) 
   24>bank/msb/lsb  ( bank msb lsb  R: 65addr)
   r> dup 1+ >r     ( bank msb lsb 65addr  R: 65addr+1)
   store8           ( bank msb  R: 65addr+1) 
   r> dup 1+ >r     ( bank msb 65addr+1  R: 65addr+2)
   store8           ( bank  R: 65addr+2)
   r> store8 ; 


\ Read current bytes in stream. Note we use PBR, not DBR
: next1byte ( -- u8 )  PC24 fetch8 ; 
: next2bytes ( -- u16 )  PC24 fetch16 ; 
: next3bytes ( -- u24 )  PC24 fetch24 ; 


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
: check-Z.a8 ( -- ) dup 0ff AND  check-Z ;    \ ignore B
: check-Z.a16 ( -- ) dup 0ffff AND  check-Z  ; 
: check-Z.x ( -- )  X @  check-Z ;
: check-Z.y ( -- )  Y @  check-Z ; 

\ common combinations
: check-NZ.8 ( n8 -- )  dup check-N8  dup check-Z ; 
: check-NZ.16 ( n16 --)  dup check-N16  dup check-Z ; 
: check-NZ.a ( -- )  check-N.a  check-Z.a ; 
: check-NZ.x ( -- )  check-N.x  check-Z.x ; 
: check-NZ.y ( -- )  check-N.y  check-Z.y ; 
 

\ ----- ALU COMMANDS ---- 
\ TODO test the checks for N,Z 

defer and.a
: and8  ( u8 u8 -- u8 )  rescue.b  and mask8  or ;  
: and16 ( u16 u16 -- u16 )  and mask16 ; \ paranoid 

defer eor.a
: eor8  ( u8 u8 -- u8 )  rescue.b  xor mask8  or ;  
: eor16 ( u16 u16 -- u16 )  xor mask16 ; \ paranoid 

defer ora.a
: ora8  ( u8 u8 -- u8 )  rescue.b  or mask8  or ;  
: ora16 ( u16 u16 -- u16 )  or mask16 ; \ paranoid 

\ Used for Accumulator, note inc.a is the code itself
defer inc.accu 
: inc8  ( u8 -- u8 ) dup mask.B  swap  1+ mask8  check-NZ.8  or ; 
: inc16  ( u16 -- u16 )  1+ mask16  check-NZ.16 ; 

\ Used for Accumulator, note dec.a is the code itself
defer dec.accu 
: dec8  ( u8 -- u8 ) dup mask.B  swap  1- mask8  check-NZ.8  or ; 
: dec16  ( u16 -- u16 )  1- mask16  check-NZ.16 ; 

\ Used for memory, not Accumulator, but affected by size of Accumulator
defer inc.mem  
: inc8.mem  ( 65addr -- )  dup fetch8 1+ mask8  check-NZ.8  swap store8 ; 
: inc16.mem  ( 65addr -- )  dup fetch16 1+ mask16  check-NZ.16  swap store16 ; 

\ Used for memory, not Accumulator, but affected by size of Accumulator
defer dec.mem  
: dec8.mem  ( 65addr -- )  dup fetch8 1- mask8  check-NZ.8  swap store8 ; 
: dec16.mem  ( 65addr -- )  dup fetch16 1- mask16  check-NZ.16  swap store16 ; 


\ --- BRANCHING --- 
cr .( Setting up branching ...) 

\ Extend the sign of an 8-bit/16-bit number in a way we don't have to care about
\ how large the cell size on the Forth machine is. Assumes that TRUE flag is
\ some form of FFFF. MASK8/MASK16 is paranoid. 
: signextend ( u8 -- u )  mask8 dup  80 and 0<>  8 lshift  or ; 
: signextend.l ( u16 -- u ) mask16 dup  8000 and 0<> 10 lshift or ;

\ Note BRANCH is reserved by Forth
: takebranch ( u8 -- u16 )  next1byte signextend 1+  PC +! ;

: branch-if-true ( f -- )  if takebranch else PC+1 then ; 
   

\ --- STACK STUFF ----
cr .( Setting up stack ...)

\ increase stack pointer 
defer S++.a
defer S++.xy
: S++8 ( -- )  S @  1+  mask8  0100 OR  S ! ; 
: S++16 ( -- )  S @  1+  mask16  S ! ; 

\ decrease stack pointer, hardcoding 01 as MSB of pointer
defer S--.a
defer S--.xy
: S--8 ( -- )  S @  1-  mask8  0100 OR  S ! ; 
: S--16 ( -- )  S @  1-  mask16  S ! ; 

\ Push stuff to stack. Note these destroy the top of the Forth stack so they
\ require a DUP for each 65816 stack instruction. We don't want to include DUP
\ here because we push other stuff than just A 
defer push.a
defer push.xy
: push8 ( n8 -- )  S @  store8  S--8 ; 
: push16 ( n16 -- ) 16>lsb/msb push8 push8 ; 
: push24 ( n24 -- ) 24>bank/msb/lsb  rot push8  swap push8  push8 ; 

\ Pull stuff from stack 
defer pull.a
defer pull.xy
: pull8 ( -- n8 )  S++8  S @  fetch8 ;  
: pull16 ( -- n16 )  pull8 pull8 lsb/msb>16 ; 
: pull24 ( -- n24 )  pull8 pull8 pull8 lsb/msb/bank>24 ; 
   
\ We need a special pull8 for A in eight bit mode because we need to protect B
: pull8.a  ( u16 -- u16 ) mask.B pull8 or ; 
: pull16.a  ( u16 -- u16 ) drop pull16 ; 


\ ---- REGISTER MODE SWITCHES ----

\ Switch accumulator 8<->16 bit (p. 51 in Manual)
\ Remember A is TOS 
: a:16  ( -- )  
   ['] fetch16.a is fetch.a  \ note special FETCH for A
   ['] store16 is store.a
   ['] PC+2 is PC+fetch.a
   ['] check-N.a16 is check-N.a
   ['] check-Z.a16 is check-Z.a
   ['] and16 is and.a
   ['] eor16 is eor.a
   ['] ora16 is ora.a
   ['] inc16 is inc.accu
   ['] dec16 is dec.accu
   ['] inc16.mem is inc.mem
   ['] dec16.mem is dec.mem
   ['] S++16 is S++.a
   ['] S--16 is S--.a
   ['] push16 is push.a 
   ['] pull16.a is pull.a  \ note special PULL for A
   m-flag clear ; 

: a:8 ( -- )  
   ['] fetch8.a is fetch.a   \ note special FETCH for A
   ['] store8 is store.a
   ['] PC+1 is PC+fetch.a
   ['] check-N.a8 is check-N.a
   ['] check-Z.a8 is check-Z.a
   ['] and8 is and.a
   ['] eor8 is eor.a
   ['] ora8 is ora.a
   ['] inc8 is inc.accu
   ['] dec8 is dec.accu 
   ['] inc8.mem is inc.mem
   ['] dec8.mem is dec.mem
   ['] S++8 is S++.a
   ['] S--8 is S--.a
   ['] push8 is push.a 
   ['] pull8.a is pull.a      \ note special PULL8 for A
   m-flag set ;

\ Switch X and Y 8<->16 bit (p. 51 in Manual) 
: xy:16  ( -- )  
   ['] fetch16 is fetch.xy 
   ['] store16 is store.xy
   ['] mask16 is mask.xy
   ['] PC+2 is PC+fetch.xy
   ['] check-N.x16 is check-N.x
   ['] check-N.y16 is check-N.y
   ['] S++16 is S++.xy
   ['] S--16 is S--.xy
   ['] push16 is push.xy 
   ['] pull16 is pull.xy
   X @  00FF AND  X !   Y @  00FF AND  Y !  \ paranoid
   x-flag clear ; 

: xy:8 ( -- )  
   ['] fetch8 is fetch.xy
   ['] store8 is store.xy
   ['] mask8 is mask.xy
   ['] PC+1 is PC+fetch.xy
   ['] check-N.x8 is check-N.x
   ['] check-N.y8 is check-N.y
   ['] S++8 is S++.xy
   ['] S--8 is S--.xy
   ['] push8 is push.xy 
   ['] pull8 is pull.xy
   X @  00FF AND  X !   Y @  00FF AND  Y !  
   x-flag set ; 

\ switch processor modes (native/emulated). There doesn't seem to be a good
\ verb for "native" like "emulate", so we're "going" 
: native ( -- )  
   e-flag clear
   ['] status-r16 is status-r 
   \ TODO set direct page to 16 zero page
   ; 

: emulated ( -- )  \ p. 45
   e-flag set   
   ['] status-r8 is status-r 
   \ We explicitly change the status flags M and X eben though we don't see them
   \ because we use them internally to figure out the size of the registers
   a:8   xy:8  
   S @  00FF AND  0100 OR  S ! \ stack pointer to 0100
   0000 D !  \ direct page register initialized to zero 
   \ TODO clear b-flag ?
   \ TODO PBR and DBR ?
   ; 


\ ---- ADDRESSING MODES --- 
cr .( Defining addressing modes ...) 

\ Note that the mnemonics for Absolute Mode have no suffix, but we use
\ MODE.ABS for clarity. Note that not all modes are listed here, as some are
\ easier to code by hand. Note that modes advance the PC so we don't have to
\ include that in the operand code. Note that register manipulation must come
\ before the mode word (eg "Y @  MODE.ABS.DBR"), not behind it 

\ Absolute 
\ We need two different versions, one for instructions that affect data and take
\ the DBR, and one for instructions that affect programs and take the PBR
: mode.abs.PBR ( -- 65addr24 )  next2bytes mem16/PBR>24 PC+2 ;
: mode.abs.DBR ( -- 65addr24 )  next2bytes mem16/DBR>24 PC+2 ;

\ Absolute Indirect 
: mode.i  ( -- 65addr24)  mode.abs.PBR  fetch16  mem16/PBR>24 ;

\ Absolute Long 
: mode.l  ( -- 65addr24)  next3bytes PC+3 ;

\ Absolute X/Y Indexed 
\ TODO TESTME  
\ TODO handle wrapping
\ TODO Figure out if we want MODE.ABS.PBR or .DBR
\ : mode.x  ( -- 65addr24 )  mode.abs  X @  + ;
\ : mode.y  ( -- 65addr24 )  mode.abs  Y @  + ;

\ Absolute Long X Indexed 
\ This assumes that X will be the correct width (8 or 16 bit) 
\ TODO handle wrapping
: mode.lx ( -- 65addr24)  mode.l  X @  + ; 

\ Direct Page (pp. 94, 155, 278) 
\ Nobody seems to be sure what happens in emulation mode if you set D to
\ a 16-bit value (see http://forum.6502.org/viewtopic.php?f=8&t=3459 for
\ a discussion). We currently assume that you can manipulate it the same way
\ you can in native mode.
\ TODO handle page boundries / wrapping
: mode.d ( -- 65addr24)  next1byte  D @  +   00  mem16/bank>24  PC+1 ;

\ Direct Page Indirect  (p. 302) 
\ Note this uses the Data Bank Register DBR, not PBR
\ TODO handle page boundries / wrapping
: mode.di  ( -- 65addr24)  
   next1byte  D @  +  0  mem16/bank>24  fetch16  DBR @  mem16/bank>24  PC+1 ;



\ ---- OUTPUT FUNCTIONS ----
cr .( Creating output functions ...) 

\ Print byte as bits, does not add space, returns as HEX
: .8bits ( u -- ) 
   2 base !  s>d <# # # # # # # # # #> type  hex ; 

\ Format numbers to two, four places, assumes HEX
: .mask8 ( n -- addr u )  s>d <# # # #> type space ; 
: .mask16 ( n -- addr u )  s>d <# # # # # #> type space ; 

\ Print state of machine 
\ TODO rewrite this once we know what we really want to see
: .state ( -- )

   \ Print status line 
   \  ." xxxx xx "
   cr ."  PC   K "

   m-flag set?  e-flag set?  or  if 
   \  ." xx xx "
      ."  B  A " else
   \  ." xxxx "
      ."   C  " then 
   
   x-flag set?  e-flag set?  or  if 
   \  ." xx xx "
      ."  X  Y " else
   \  ." xxxx xxxx "
      ."   X    Y  " then 
 
   e-flag set? if 
   \  ." xxxx xxxx xx xxxxxxxx" 
      ."   S    D   B NV-BDIZC" else
      ."   S    D   B NVMXDIZC" then cr 

   PC @ .mask16  PBR @ .mask8   
   
   \ print A/B or C
   dup 
   m-flag set?  e-flag set?  or  if  
      dup msb .mask8 lsb .mask8 else
      .mask16 then

   \ print XY
   Y @  X @
   x-flag set? if .mask8 .mask8  else  .mask16 .mask16  then 
   
   S @ .mask16  D @ .mask16  DBR @ .mask8
   status-r .8bits  space 
   e-flag set? if ." emulated" else ." native" then  cr ; 

\ Print stack if we are in emulated mode
: stackempty? ( -- f )  S @  01ff  = ; 
: .stack ( -- )
   cr  e-flag clear? if
         ." Can't dump stack when in native mode (yet)"
      else
         stackempty? if  
            ." Stack is empty (S is 01FF in emulated mode)" cr  else
         0200  S @ 1+  ?do  i dup .  space  fetch8 .mask8  cr  loop 
      then then ; 

\ Print Direct Page contents. We use D as a base regardless of which mode we are
\ in; see MODE.D for discussion of what happens with D in emulation mode.
\ Assumes HEX.
: .direct ( -- ) 
   cr ."       00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F"
   10 0 ?do  cr  D @  i +  .mask16 ."  " 
      10 0 ?do   D @  i +  j +  fetch8 .mask8
   loop loop cr ; 



\ ---- OPCODE ROUTINES ----
cr .( Defining opcode routines ... ) 

\ TODO change so we drop into single-step mode
\ TODO add stack stuff
: opc-00 ( brk )   cr ." *** BRK encountered, halting CPU (ALPHA only) ***" 
   .state quit ; 

: opc-01 ( ora.dxi )   ." 01 not coded yet" ; 
: opc-02 ( cop )   ." 02 not coded yet" ; 
: opc-03 ( ora.s )   ." 03 not coded yet" ; 
: opc-04 ( tsb.d )  ." 04 not coded yet" ; 
: opc-05 ( ora.d )   ." 05 not coded yet" ; 
: opc-06 ( asl.d )  ." 06 not coded yet" ; 

: opc-07 ( ora.dil )   ." 07 not coded yet" ; 
: opc-08 ( php )   ." 08 not coded yet" ; 

: opc-09 ( ora.# )  \ DUP required because FETCH.A replaces A
   dup PC24 fetch.a  ora.a  check-NZ.a  PC+fetch.a ; \ TODO TESTME

: opc-0A ( asl.a )   ." 0A not coded yet" ; 

: opc-0B ( phd )  D @  mask16  push16 ;

: opc-0C ( tsb )   ." 0C not coded yet" ; 

: opc-0D ( ora )  \ DUP required because FETCH.A replaces A
   dup mode.abs.DBR  fetch.a  ora.a  check-NZ.a ;

: opc-0E ( asl )   ." 0E not coded yet" ; 

\ DUP required because FETCH.A replaces A
: opc-0F ( ora.l )  dup mode.l  fetch.a  ora.a  check-NZ.a ;

: opc-10 ( bpl )  n-flag clear? branch-if-true ; 

: opc-11 ( ora.diy )   ." 11 not coded yet" ; 
: opc-12 ( ora.di )   ." 12 not coded yet" ; 
: opc-13 ( ora.siy )   ." 13 not coded yet" ; 
: opc-14 ( trb.d )   ." 14 not coded yet" ; 
: opc-15 ( ora.dx )   ." 15 not coded yet" ; 
: opc-16 ( asl.dx )   ." 16 not coded yet" ; 
: opc-17 ( ora.dily )   ." 17 not coded yet" ; 

: opc-18 ( clc )  c-flag clear ; 

: opc-19 ( ora.y )   ." 19 not coded yet" ; 

: opc-1A ( inc.a )   inc.accu ;

: opc-1B ( tcs )  \ Does not affect flags; compare TXS
   dup e-flag set?  if \ emulation mode, hi byte paranoided to 01
      mask8 0100 or  else
      mask16 then      \ native mode always copies full C to S
      S ! ;

: opc-1C ( trb )   ." 1C not coded yet" ; 
: opc-1D ( ora.x )   ." 1D not coded yet" ; 
: opc-1E ( asl.x )   ." 1E not coded yet" ; 
: opc-1F ( ora.lx )  dup mode.lx  fetch.a  ora.a  check-NZ.a ; \ TODO TEST ME 

\ STEP already increases the PC by one, so we only need to add one byte because
\ the address pushed is the last byte of the instruction
: opc-20 ( jsr )  PC @  1+  push16   next2bytes PC ! ;

: opc-21 ( and.dxi )   ." 21 not coded yet" ; 

: opc-22 ( jsr.l )  PC24  2 +  push24  next3bytes 24>PC24 ;

: opc-23 ( and.s )   ." 23 not coded yet" ; 
: opc-24 ( bit.d )   ." 24 not coded yet" ; 

\ DUP required because FETCH.A replaces A
: opc-25 ( and.d )  dup mode.d fetch.a  and.a check-NZ.a ; 

: opc-26 ( rol.d )   ." 26 not coded yet" ; 
: opc-27 ( and.dil )   ." 27 not coded yet" ; 
: opc-28 ( plp )   ." 28 not coded yet" ; 
\
\ DUP required because FETCH.A replaces A
: opc-29 ( and.# )  dup PC24 fetch.a  and.a  check-NZ.a  PC+fetch.a ; 

: opc-2A ( rol )   ." 2A not coded yet" ; 

\ Affects N and Z, is always 16 bit
: opc-2B ( pld )  pull16  dup check-Z  dup check-N16  D ! ;

: opc-2C ( bit )   ." 2C not coded yet" ; 

: opc-2D ( and )  dup mode.abs.DBR  fetch.a  and.a  check-NZ.a ;  \ TODO TESTME

: opc-2E ( rol )   ." 2E not coded yet" ; 

: opc-2F ( and.l ) dup  mode.l  fetch.a  and.a  check-NZ.a ;

: opc-30 ( bmi )  n-flag set? branch-if-true ; 

: opc-31 ( and.diy )   ." 31 not coded yet" ; 

: opc-32 ( and.di )  dup mode.di fetch.a and.a check-NZ.a ; \ TODO TESTME

: opc-33 ( and.siy )   ." 33 not coded yet" ; 
: opc-34 ( bit.dx )   ." 34 not coded yet" ; 
: opc-35 ( and.dx )   ." 35 not coded yet" ; 
: opc-36 ( rol.dx )   ." 36 not coded yet" ; 
: opc-37 ( and.dy )   ." 37 not coded yet" ; 

: opc-38 ( sec )  c-flag set ;  

: opc-39 ( and.y )   ." 39 not coded yet" ; 

: opc-3A ( dec.a )  dec.accu ;

\ Assumes N affected by 16th bit in all modes
: opc-3B ( tsc )  drop  S @  mask16  check-NZ.a ; 

: opc-3C ( bit.x )   ." 3C not coded yet" ; 
: opc-3D ( and.x )   ." 3D not coded yet" ; 
: opc-3E ( rol.x )   ." 3E not coded yet" ; 

: opc-3F ( and.lx )  dup mode.lx  fetch.a  and.a  check-NZ.a ; \ TODO TEST ME 

: opc-40 ( rti )   ." 40 not coded yet" ; 
: opc-41 ( eor.dxi )   ." 41 not coded yet" ; 

: opc-42 ( wdm ) ." WARNING: WDM executed."  PC+1 ; 

: opc-43 ( eor.s )   ." 43 not coded yet" ; 
: opc-44 ( mvp )   ." 44 not coded yet" ; 
: opc-45 ( eor.d )   ." 45 not coded yet" ; 
: opc-46 ( lsr.d )   ." 46 not coded yet" ; 
: opc-47 ( eor.dil )   ." 47 not coded yet" ; 
: opc-48 ( pha )  dup push.a ; 

: opc-49 ( eor.# )  
      dup PC24 fetch.a  eor.a  check-NZ.a  PC+fetch.a ; \ TODO TESTME

: opc-4A ( lsr.a )   ." 4A not coded yet" ; 

: opc-4B ( phk )  PBR @  mask8  push8 ; \ Mask8 is paranoid 
: opc-4C ( jmp )  next2bytes  PC ! ;
: opc-4D ( eor )  dup mode.abs.DBR  fetch.a  eor.a  check-NZ.a ;  \ TODO TESTME

: opc-4E ( lsr )   ." 4E not coded yet" ; 

: opc-4F ( eor.l )  dup  mode.l  fetch.a  eor.a  check-NZ.a ;
: opc-50 ( bvc )  v-flag clear? branch-if-true ; 

: opc-51 ( eor.diy )   ." 51 not coded yet" ; 

: opc-52 ( eor.di )  dup mode.di  fetch.a eor.a check-NZ.a ; \ TODO TESTME

: opc-53 ( eor.siy )   ." 53 not coded yet" ; 
: opc-54 ( mvn )   ." 54 not coded yet" ; 
: opc-55 ( eor.dx )   ." 55 not coded yet" ; 
: opc-56 ( lsr.dx )   ." 56 not coded yet" ; 
: opc-57 ( eor.dy )   ." 57 not coded yet" ; 

: opc-58 ( cli )  i-flag clear ;  

: opc-59 ( eor.y )   ." 59 not coded yet" ; 

: opc-5A ( phy )  Y @ push.xy ; 
: opc-5B ( tcd )  dup  mask16  D ! check-NZ.a ;  \ mask16 is paranoid
: opc-5C ( jmp.l )  next3bytes 24>PC24 ; 

: opc-5D ( eor.dx )   ." 5D not coded yet" ; 
: opc-5E ( lsr.x )   ." 5E not coded yet" ; 

: opc-5F ( eor.lx )  dup mode.lx  fetch.a  eor.a  check-NZ.a ; \ TODO TEST ME

: opc-60 ( rts )  pull16 1+  PC ! ;
 
: opc-61 ( adc.dxi )   ." 61 not coded yet" ; 
: opc-62 ( phe.r )   ." 62 not coded yet" ; 
: opc-63 ( adc.s )   ." 63 not coded yet" ; 

: opc-64 ( stz.d )  0 mode.d store.a ; 

: opc-65 ( adc.d )   ." 65 not coded yet" ; 
: opc-66 ( ror.d )   ." 66 not coded yet" ; 
: opc-67 ( adc.dil )   ." 67 not coded yet" ; 

: opc-68 ( pla )  pull.a check-NZ.a ; 

: opc-69 ( adc.# )   ." 69 not coded yet" ; 
: opc-6A ( ror.a )   ." 6A not coded yet" ; 

: opc-6B ( rts.l )  pull24 1+  24>PC24 ; 

: opc-6C ( jmp.i )  mode.i  PC ! ; 

: opc-6D ( adc )   ." 6D not coded yet" ; 
: opc-6E ( ror )   ." 6E not coded yet" ; 
: opc-6F ( adc.l )   ." 6F not coded yet" ; 

: opc-70 ( bvs )  v-flag set? branch-if-true ;  

: opc-71 ( adc.diy )   ." 71 not coded yet" ; 
: opc-72 ( adc.di )   ." 72 not coded yet" ; 
: opc-73 ( adc.siy )   ." 73 not coded yet" ; 
: opc-74 ( stx.dx )   ." 74 not coded yet" ; 
: opc-75 ( adc.dx)   ." 75 not coded yet" ; 
: opc-76 ( ror.dx )   ." 76 not coded yet" ; 
: opc-77 ( adc.dy )   ." 77 not coded yet" ; 

: opc-78 ( sei ) i-flag set ; 

: opc-79 ( adc.y )   ." 79 not coded yet" ; 

: opc-7A ( ply )  pull.xy  Y !  check-NZ.y ;
: opc-7B ( tdc )  drop  D @  mask16  check-NZ.a ;  \ TODO TESTME, mask16 paranoid

: opc-7C ( jmp.xi )   ." 7C not coded yet" ; 
: opc-7D ( adc.x )   ." 7D not coded yet" ; 
: opc-7E ( ror.x )   ." 7E not coded yet" ; 
: opc-7F ( adc.lx )   ." 7F not coded yet" ; 

: opc-80 ( bra )  takebranch ;

: opc-81 ( sta.dxi )   ." 81 not coded yet" ;

: opc-82 ( bra.l )  next2bytes signextend.l  2 +  PC +! ; 

: opc-83 ( sta.s )   ." 83 not coded yet" ; 

: opc-84 ( sty.d )  Y @  mode.d store.xy ;
: opc-85 ( sta.d )  dup mode.d store.a ; 
: opc-86 ( stx.d )  X @  mode.d store.xy ;  

: opc-87 ( sta.dil )   ." 87 not coded yet" ; 

: opc-88 ( dey )  Y @  1- mask.xy  Y !  check-NZ.y ;

: opc-89 ( bit.# )   ." 89 not coded yet" ; 

: opc-8A ( txa )  \ TODO test what happens to B register
   m-flag clear? if  
      drop  X @  else
      mask.B  X @  mask8 or  then 
      check-NZ.a ; 

: opc-8B ( phb )  DBR @  mask8  push8 ; \ mask8 is paranoid 
: opc-8C ( sty )  Y @  mode.abs.DBR store.xy ;
: opc-8D ( sta )  dup  mode.abs.DBR  store.a ; 
: opc-8E ( stx )  X @  mode.abs.DBR store.xy ;
: opc-8F ( sta.l )  dup  mode.l  store.a ; 
: opc-90 ( bcc )  c-flag clear? branch-if-true ; 

: opc-91 ( sta.diy )   ." 91 not coded yet" ; 

: opc-92 ( sta.di )  dup mode.di  store.a ;

: opc-93 ( sta.siy )   ." 93 not coded yet" ; 
: opc-94 ( sty.dx )   ." 94 not coded yet" ; 
: opc-95 ( sta.dx )   ." 95 not coded yet" ; 
: opc-96 ( stx.dy )   ." 96 not coded yet" ; 
: opc-97 ( sta.dily )   ." 97 not coded yet" ; 

: opc-98 ( tya )  \ TODO test what happens to B register
   m-flag clear? if  
      drop  Y @  else
      mask.B  Y @  mask8 or  then 
      check-NZ.a ; 

: opc-99 ( sta.y )   ." 99 not coded yet" ; 

\ Does not alter flags; compare TCS 
: opc-9A ( txs ) 
   X @
   e-flag set? if    \ emulation mode, hi byte paranoided to 01
      mask8  0100 or else
         x-flag set? if mask8 then  \ native mode, 8 bit X; hi byte is 00
   then 
   S ! ; 
            
: opc-9B ( txy )  X @  Y !  check-NZ.y ;
: opc-9C ( stz )  0  mode.abs.DBR store.a ; 

: opc-9D ( sta.x )   ." 9D not coded yet" ; 
: opc-9E ( stz.x )   ." 9E not coded yet" ; 

: opc-9F ( sta.lx )  dup  mode.lx  store.a ;

\ We need PC+fetch.xy for PC because no MODE.* used
: opc-A0 ( ldy.# )  PC24 fetch.xy  Y !  check-NZ.y  PC+fetch.xy ;

: opc-A1 ( lda.dxi )   ." A1 not coded yet" ; 

\ We need PC+fetch.xy for PC because no MODE.* used
: opc-A2 ( ldx.# )  PC24 fetch.xy  X !  check-NZ.x  PC+fetch.xy ;

: opc-A3 ( lda.s )   ." A3 not coded yet" ; 

: opc-A4 ( ldy.d )  mode.d fetch.xy  Y !  check-NZ.y ; 
: opc-A5 ( lda.d )  mode.d fetch.a check-NZ.a ; 
: opc-A6 ( ldx.d )  mode.d fetch.xy  X !  check-NZ.x ; 

: opc-A7 ( lda.dil )   ." A7 not coded yet" ; 

: opc-A8 ( tay )  dup x-flag set? if mask8 else mask16 then  Y !  check-NZ.y ; 

\ We need PC+fetch.a for PC because no MODE.* used
: opc-A9 ( lda.# ) PC24 fetch.a  check-NZ.a  PC+fetch.a ; 

: opc-AA ( tax )  dup x-flag set? if mask8 else mask16 then  X !  check-NZ.x ; 
: opc-AB ( plb )  pull8  dup check-Z dup check-N8  DBR ! ;
: opc-AC ( ldy )  mode.abs.DBR  fetch.xy  Y !  check-NZ.y ;
: opc-AD ( lda )  mode.abs.DBR  fetch.a  check-NZ.a ;
: opc-AE ( ldx )  mode.abs.DBR  fetch.xy  X !  check-NZ.x ;
: opc-AF ( lda.l ) mode.l  fetch.a  check-NZ.a PC+fetch.a ; 
: opc-B0 ( bcs )  c-flag set? branch-if-true ;  

: opc-B1 ( lda.diy )   ." B1 not coded yet" ; 

: opc-B2 ( lda.di )  mode.di  fetch.a  check-NZ.a ; 

: opc-B3 ( lda.siy )   ." B3 not coded yet" ; 
: opc-B4 ( ldy.dx )   ." B4 not coded yet" ; 
: opc-B5 ( lda.dx )   ." B5 not coded yet" ; 
: opc-B6 ( ldx.dy )   ." B6 not coded yet" ; 
: opc-B7 ( lda.dy )   ." B7 not coded yet" ; 

: opc-B8 ( clv ) v-flag clear ; 

: opc-B9 ( lda.y )   ." B9 not coded yet" ; 

: opc-BA ( tsx )  S @  x-flag set? if mask8 then  X !  check-NZ.x ;  
: opc-BB ( tyx )  Y @  X !  check-NZ.x ;

: opc-BC ( ldy.x )   ." BC not coded yet" ; 
: opc-BD ( lda.x )   ." BD not coded yet" ; 
: opc-BE ( ldx.y )   ." BE not coded yet" ; 

: opc-BF ( lda.lx )  mode.lx  fetch.a check-NZ.a ;

: opc-C0 ( cpy.# )   ." C0 not coded yet" ; 
: opc-C1 ( cmp.dxi )   ." C1 not coded yet" ; 

: opc-C2 ( rep ) \ TODO crude testing version, complete this for all flags
   cr ." WARNING: REP is incomplete, works only on m and x flags" cr
   next1byte 
   dup  20 = if a:16 else 
   dup  10 = if xy:16  else 
   dup  30 = if a:16 xy:16 then then then drop 
   PC+1 ; 

: opc-C3 ( cmp.s )   ." C3 not coded yet" ; 
: opc-C4 ( cpy.d )   ." C4 not coded yet" ; 
: opc-C5 ( cmp.d )   ." C5 not coded yet" ; 

: opc-C6 ( dec.d )  mode.d dec.mem ;

: opc-C7 ( cmp.dil )   ." C7 not coded yet" ; 

: opc-C8 ( iny )  Y @  1+  mask.xy  Y !  check-NZ.y ;

: opc-C9 ( cmp.# )   ." C9 not coded yet" ; 

: opc-CA ( dex )  X @  1- mask.xy  X !  check-NZ.x ;

: opc-CB ( wai ) \ TODO crude testing version, complete this for i-flag
   cr cr ." *** WAI instruction, halting processor ***" .state quit 
   cr ." WARNING: WAI not fully implemented" ; 

: opc-CC ( cpy )   ." CC not coded yet" ; 
: opc-CD ( cmp )   ." CD not coded yet" ; 

: opc-CE ( dec )  mode.abs.DBR dec.mem ;

: opc-CF ( cmp.l )   ." CF not coded yet" ; 

: opc-D0 ( bne )  z-flag clear? branch-if-true ; 

: opc-D1 ( cmp.diy )   ." D1 not coded yet" ; 
: opc-D2 ( cmp.di )   ." D2 not coded yet" ; 
: opc-D3 ( cmp.siy )   ." D3 not coded yet" ; 

: opc-D4 ( phe.d )  mode.d fetch16 push16 ; \ pp. 169, 373

: opc-D5 ( cmp.dx )   ." D5 not coded yet" ; 
: opc-D6 ( dec.dx )   ." D6 not coded yet" ; 
: opc-D7 ( cmp.dy )   ." D7 not coded yet" ; 

: opc-D8 ( cld )  d-flag clear ;  

: opc-D9 ( cmp.y )   ." D9 not coded yet" ; 

: opc-DA ( phx )  X @  push.xy ;

: opc-DB ( stp )  cr cr ." *** STP instruction, halting processor" cr
   .state quit ; 

: opc-DC ( jmp.il )   ." DC not coded yet" ; 
: opc-DD ( cmp.x )   ." DD not coded yet" ; 
: opc-DE ( dec.x )   ." DE not coded yet" ; 
: opc-DF ( cmp.lx )   ." DF not coded yet" ; 
: opc-E0 ( cpx.# )   ." E0 not coded yet" ; 
: opc-E1 ( sbc.dxi )  ." E1 not coded yet" ; 

: opc-E2 ( sep ) \ TODO crude testing version, complete this for all flags
   cr ." WARNING: SEP is incomplete, works only on m and x flags" cr
   next1byte 
   dup  20 = if a:8 else 
   dup  10 = if xy:8  else 
   dup  30 = if a:8 xy:8 then then then drop 
   PC+1 ; 

: opc-E3 ( sbc.s )   ." E3 not coded yet" ; 
: opc-E4 ( cpx.d )   ." E4 not coded yet" ; 
: opc-E5 ( sbc.d )   ." E5 not coded yet" ; 

: opc-E6 ( inc.d )   mode.d inc.mem ; 

: opc-E7 ( sbc.dil )   ." E7 not coded yet" ; 

: opc-E8 ( inx )  X @  1+  mask.xy  X !  check-NZ.x ;

: opc-E9 ( sbc.# )   ." E9 not coded yet" ; 
: opc-EA ( nop ) ;

\ N and Z depend only on value in A, regardless if the register is in 8 or 
\ 16 bit mode 
: opc-EB ( xba )  
   dup msb swap  lsb 8 lshift  or  check-N.a8 check-Z.a8 ;

: opc-EC ( cpx )   ." EC not coded yet" ; 
: opc-ED ( sbc )   ." ED not coded yet" ; 

: opc-EE ( inc )   mode.abs.DBR inc.mem ; \ Does not affect carry or decimal flag ; 

: opc-EF ( sbc.l )   ." EF not coded yet" ; 

: opc-F0 ( beq )  z-flag set? branch-if-true ; 

: opc-F1 ( sbc.diy )   ." F1 not coded yet" ; 
: opc-F2 ( sbc.di )   ." F2 not coded yet" ; 
: opc-F3 ( sbc.siy )   ." F3 not coded yet" ; 

: opc-F4 ( phe.# )  next2bytes push16 PC+2 ; \ Does not affect flags

: opc-F5 ( sbc.dx )   ." F5 not coded yet" ; 
: opc-F6 ( inc.dx )   ." F6 not coded yet" ; 
: opc-F7 ( sbc.dily )   ." F7 not coded yet" ; 

: opc-F8 ( sed )  d-flag set ; 

: opc-F9 ( sbc.y )   ." F9 not coded yet" ; 

: opc-FA ( plx )  pull.xy  X !  check-NZ.x ; 

: opc-FB ( xce )  e-flag @  c-flag @  swap  c-flag !  dup e-flag !
   if emulated else native then ; 

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

: poweron ( -- ) \ TODO not used yet
   0000 D !  \ intiate Direct Page to zero (p. 155)
   \ TODO add rest
   ; 

: reset-i ( -- )  \ p.201
   emulated
   00  \ TOS is A
   \ TODO Set flags
   00 X !   00 Y !  00 PBR !  00 DBR !  0000 D ! 
   reset-v fetch16  PC ! 
   \ RESET and power on set the MSB of the S to $01 but don't put the LSB
   \ in a defined state (http://forum.6502.org/viewtopic.php?f=4&t=2258) 
   S @  mask8  0100 OR  S ! ; \ TODO check if we keep LSB

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
: step ( -- )  opc-jumptable next1byte cells +  @    PC+1   execute ; 
: run ( -- )   begin step again ; 


\ ---- START EMULATION ----
cr .( All done.) cr 

\ Note that we currently cheat here using RESET as the situation after boot,
\ which is not technically correct
reset-i 
.state 

cr ." Machine ready. Type 'run' to start emulation, 'step' for single-step."

