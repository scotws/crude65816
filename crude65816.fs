\ The Crude 65816 Emulator 
\ Copyright 2015 Scot W. Stevenson <scot.stevenson@gmail.com>
\ Written with gforth 0.7
\ First version: 08. Jan 2015
\ This version: 22. Jan 2015

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

cr .( The Crude 65816 Emulator Version pre-ALPHA) 
cr .( Copyright 2015 Scot W. Stevenson <scot.stevenson@gmail.com> ) 
cr .( This program comes with ABSOLUTELY NO WARRANTY) cr

hex

\ ---- DEFINITIONS ----
cr .( Defining things ...)

    400 constant 1k       1k 8 * constant 8k     8k 2* constant 16k
16k 8 * constant 64k   64k 100 * constant 16M    64k constant bank 




\ ---- VARIABLES ----

variable PC       \ Program Counter of CPU 



\ ---- HELPER FUNCTIONS ----

\ mask addresses
: mask8 ( u -- u8 ) 0ff and ; 
: mask16 ( u -- u16 ) 0ffff and ; 
: mask24 ( u -- u24 ) 0ffffff and ; 

\ return least or most significant byte of 16-bit number
: lsb ( u16 -- u8 )  mask8 ;
: msb ( u16 -- u8 )  0ff00 and  8 rshift ;

\ convert 16 bit address to little-endian
: swapbytes ( u16 -- u8h u8l )  dup msb swap lsb ;

\ handle Program Counter
: PC+u ( u -- ) ( -- )   
   create ,
   does> @ PC +! ;

1 PC+u PC+1   2 PC+u PC+2   3 PC+u PC+3


\ ---- HARDWARE: MEMORY ----
cr .( Setting up Memory ...) 

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
\ TODO close file when done
include config.fs  


\ all accesses to memory are full 24 bit
: mem16>24  ( u8 65addr16 -- 65addr24) ; 
: mem8>24  ( u8 u8 u8 -- 65addr24) ; 


\ Routines for fetching and storing memory
defer fetch.a
defer fetch.xy
: fetch8  ( 65addr24 -- u8 ) ; 
: fetch16  ( 65addr24 -- u16 ) ; 

defer store.a
defer store.xy
: store8 ( u8 65addr24 -- ) ; 
: store16 ( u16 65addr24 -- ) ; 


\ ---- HARDWARE: CPU ----
cr .( Setting up CPU ... ) 

variable pc    \ program counter (16 bit) 
variable x     \ X register (8\16 bit)
variable y     \ Y register (8\16 bit)
variable dp    \ Direct Page (Zero Page) pointer (16 bit) 
variable sp    \ Stack pointer (16 bit) 
variable db-r  \ Data Bank register
variable pb-r  \ Program Bank register


\ Read next byte in stream


\ ---- HARDWARE: FLAGS ----
\ All flags are fully formed Forth flags (one cell large) 
cr .( Setting up flag routines ... ) 

variable n-flag   variable v-flag   variable m-flag
variable x-flag   variable b-flag   variable d-flag 
variable i-flag   variable z-flag   variable c-flag 
variable e-flag 

\ make code easier for humans
: set?  ( addr -- f )  @ ;  
: clear?  ( addr -- f )  @ invert ;
: set  ( addr -- )  true swap ! ; 
: clear  ( addr -- )  false swap ! ; 

defer status-r

\ construct status byte for emulation mode 
: status-r8  ( -- u8 ) 
   n-flag @  80 and 
   v-flag @  40 and +
\   bit 5 is empty 
   b-flag @  10 and +
   d-flag @  08 and +
   i-flag @  04 and +
   z-flag @  02 and +
   c-flag @  01 and + ; 

\ construct status byte for native mode
: status-r16  ( -- u16 ) 
   n-flag @  80 and 
   v-flag @  40 and +
   m-flag @  20 and +
   x-flag @  10 and +
   d-flag @  08 and +
   i-flag @  04 and +
   z-flag @  02 and +
   c-flag @  01 and + ; 


\ ---- OPCODE ROUTINES ----
cr .( Loading opcode routines ... ) 

: opc-00 ( brk )   ." 00 not coded yet" ; 
: opc-01 ( TODO )   ." 01 not coded yet" ; 
: opc-02 ( cop )   ." 02 not coded yet" ; 
: opc-03 ( ora.s )   ." 03 not coded yet" ; 
: opc-04 ( TODO )   ." 04 not coded yet" ; 
: opc-05 ( TODO )   ." 05 not coded yet" ; 
: opc-06 ( asl.d )  ." 06 not coded yet" ; 
: opc-07 ( TODO )   ." 07 not coded yet" ; 
: opc-08 ( TODO )   ." 08 not coded yet" ; 
: opc-09 ( TODO )   ." 09 not coded yet" ; 
: opc-0A ( TODO )   ." 0A not coded yet" ; 
: opc-0B ( phd )   ." 0B not coded yet" ; 
: opc-0C ( TODO )   ." 0C not coded yet" ; 
: opc-0D ( TODO )   ." 0D not coded yet" ; 
: opc-0E ( TODO )   ." 0E not coded yet" ; 
: opc-0F ( TODO )   ." 0F not coded yet" ; 
: opc-10 ( TODO )   ." 10 not coded yet" ; 
: opc-11 ( TODO )   ." 11 not coded yet" ; 
: opc-12 ( TODO )   ." 12 not coded yet" ; 
: opc-13 ( TODO )   ." 13 not coded yet" ; 
: opc-14 ( TODO )   ." 14 not coded yet" ; 
: opc-15 ( TODO )   ." 15 not coded yet" ; 
: opc-16 ( TODO )   ." 16 not coded yet" ; 
: opc-17 ( TODO )   ." 17 not coded yet" ; 
: opc-18 ( clc )  c-flag clear ; 
: opc-19 ( TODO )   ." 19 not coded yet" ; 
: opc-1A ( TODO )   ." 1A not coded yet" ; 
: opc-1B ( TODO )   ." 1B not coded yet" ; 
: opc-1C ( TODO )   ." 1C not coded yet" ; 
: opc-1D ( TODO )   ." 1D not coded yet" ; 
: opc-1E ( TODO )   ." 1E not coded yet" ; 
: opc-1F ( TODO )   ." 1F not coded yet" ; 
: opc-20 ( TODO )   ." 20 not coded yet" ; 
: opc-21 ( TODO )   ." 21 not coded yet" ; 
: opc-22 ( jsr.l )   ." 22 not coded yet" ; 
: opc-23 ( TODO )   ." 23 not coded yet" ; 
: opc-24 ( bit.d )   ." 24 not coded yet" ; 
: opc-25 ( TODO )   ." 25 not coded yet" ; 
: opc-26 ( rol.d )   ." 26 not coded yet" ; 
: opc-27 ( TODO )   ." 27 not coded yet" ; 
: opc-28 ( plp )   ." 28 not coded yet" ; 
: opc-29 ( and.# )   ." 29 not coded yet" ; 
: opc-2A ( rol )   ." 2A not coded yet" ; 
: opc-2B ( pld )   ." 2B not coded yet" ; 
: opc-2C ( TODO )   ." 2C not coded yet" ; 
: opc-2D ( TODO )   ." 2D not coded yet" ; 
: opc-2E ( TODO )   ." 2E not coded yet" ; 
: opc-2F ( TODO )   ." 2F not coded yet" ; 
: opc-30 ( TODO )   ." 30 not coded yet" ; 
: opc-31 ( TODO )   ." 31 not coded yet" ; 
: opc-32 ( and.di )   ." 32 not coded yet" ; 
: opc-33 ( TODO )   ." 33 not coded yet" ; 
: opc-34 ( TODO )   ." 34 not coded yet" ; 
: opc-35 ( TODO )   ." 35 not coded yet" ; 
: opc-36 ( TODO )   ." 36 not coded yet" ; 
: opc-37 ( TODO )   ." 37 not coded yet" ; 
: opc-38 ( sec )  c-flag set ;  
: opc-39 ( TODO )   ." 39 not coded yet" ; 
: opc-3A ( TODO )   ." 3A not coded yet" ; 
: opc-3B ( TODO )   ." 3B not coded yet" ; 
: opc-3C ( TODO )   ." 3C not coded yet" ; 
: opc-3D ( TODO )   ." 3D not coded yet" ; 
: opc-3E ( TODO )   ." 3E not coded yet" ; 
: opc-3F ( TODO )   ." 3F not coded yet" ; 
: opc-40 ( rti )   ." 40 not coded yet" ; 
: opc-41 ( TODO )   ." 41 not coded yet" ; 
: opc-42 ( wdm )   ." 42 not coded yet" ; 
: opc-43 ( TODO )   ." 43 not coded yet" ; 
: opc-44 ( mvp )   ." 44 not coded yet" ; 
: opc-45 ( TODO )   ." 45 not coded yet" ; 
: opc-46 ( TODO )   ." 46 not coded yet" ; 
: opc-47 ( TODO )   ." 47 not coded yet" ; 
: opc-48 ( pha )   ." 48 not coded yet" ; 
: opc-49 ( eor.# )   ." 49 not coded yet" ; 
: opc-4A ( TODO )   ." 4A not coded yet" ; 
: opc-4B ( phk )   ." 4B not coded yet" ; 
: opc-4C ( TODO )   ." 4C not coded yet" ; 
: opc-4D ( TODO )   ." 4D not coded yet" ; 
: opc-4E ( TODO )   ." 4E not coded yet" ; 
: opc-4F ( TODO )   ." 4F not coded yet" ; 
: opc-50 ( TODO )   ." 50 not coded yet" ; 
: opc-51 ( TODO )   ." 51 not coded yet" ; 
: opc-52 ( TODO )   ." 52 not coded yet" ; 
: opc-53 ( TODO )   ." 53 not coded yet" ; 
: opc-54 ( mvn )   ." 54 not coded yet" ; 
: opc-55 ( TODO )   ." 55 not coded yet" ; 
: opc-56 ( TODO )   ." 56 not coded yet" ; 
: opc-57 ( TODO )   ." 57 not coded yet" ; 
: opc-58 ( TODO )   ." 58 not coded yet" ; 
: opc-59 ( TODO )   ." 59 not coded yet" ; 
: opc-5A ( phy )   ." 5A not coded yet" ; 
: opc-5B ( TODO )   ." 5B not coded yet" ; 
: opc-5C ( jmp.l )   ." 5C not coded yet" ; 
: opc-5D ( TODO )   ." 5D not coded yet" ; 
: opc-5E ( TODO )   ." 5E not coded yet" ; 
: opc-5F ( TODO )   ." 5F not coded yet" ; 
: opc-60 ( rts )   ." 60 not coded yet" ; 
: opc-61 ( TODO )   ." 61 not coded yet" ; 
: opc-62 ( per )   ." 62 not coded yet" ; 
: opc-63 ( adc.s )   ." 63 not coded yet" ; 
: opc-64 ( stz.d )   ." 64 not coded yet" ; 
: opc-65 ( adc.d )   ." 65 not coded yet" ; 
: opc-66 ( TODO )   ." 66 not coded yet" ; 
: opc-67 ( TODO )   ." 67 not coded yet" ; 
: opc-68 ( TODO )   ." 68 not coded yet" ; 
: opc-69 ( adc.# )   ." 69 not coded yet" ; 
: opc-6A ( TODO )   ." 6A not coded yet" ; 
: opc-6B ( rtl )   ." 6B not coded yet" ; 
: opc-6C ( TODO )   ." 6C not coded yet" ; 
: opc-6D ( adc )   ." 6D not coded yet" ; 
: opc-6E ( TODO )   ." 6E not coded yet" ; 
: opc-6F ( TODO )   ." 6F not coded yet" ; 
: opc-70 ( TODO )   ." 70 not coded yet" ; 
: opc-71 ( TODO )   ." 71 not coded yet" ; 
: opc-72 ( TODO )   ." 72 not coded yet" ; 
: opc-73 ( TODO )   ." 73 not coded yet" ; 
: opc-74 ( stx.dx )   ." 74 not coded yet" ; 
: opc-75 ( TODO )   ." 75 not coded yet" ; 
: opc-76 ( TODO )   ." 76 not coded yet" ; 
: opc-77 ( TODO )   ." 77 not coded yet" ; 
: opc-78 ( sei ) i-flag set ; 
: opc-79 ( TODO )   ." 79 not coded yet" ; 
: opc-7A ( ply )   ." 7A not coded yet" ; 
: opc-7B ( TODO )   ." 7B not coded yet" ; 
: opc-7C ( TODO )   ." 7C not coded yet" ; 
: opc-7D ( TODO )   ." 7D not coded yet" ; 
: opc-7E ( TODO )   ." 7E not coded yet" ; 
: opc-7F ( TODO )   ." 7F not coded yet" ; 
: opc-80 ( bra )   ." 80 not coded yet" ; 
: opc-81 ( TODO )   ." 81 not coded yet" ; 
: opc-82 ( brl )   ." 82 not coded yet" ; 
: opc-83 ( sta.s )   ." 83 not coded yet" ; 
: opc-84 ( TODO )   ." 84 not coded yet" ; 
: opc-85 ( TODO )   ." 85 not coded yet" ; 
: opc-86 ( TODO )   ." 86 not coded yet" ; 
: opc-87 ( TODO )   ." 87 not coded yet" ; 
: opc-88 ( dey )   ." 88 not coded yet" ; 
: opc-89 ( TODO )   ." 89 not coded yet" ; 
: opc-8A ( txa )   ." 8A not coded yet" ; 
: opc-8B ( phb )   ." 8B not coded yet" ; 
: opc-8C ( sty )   ." 8C not coded yet" ; 
: opc-8D ( sta ) ( 65addr24 -- ) dup store.a pc+2 ; 
: opc-8E ( stx ) ( 65addr24 -- ) x @ swap store.xy pc+2 ; 
: opc-8F ( sta.l )   ." 8F not coded yet" ; 
: opc-90 ( TODO )   ." 90 not coded yet" ; 
: opc-91 ( TODO )   ." 91 not coded yet" ; 
: opc-92 ( TODO )   ." 92 not coded yet" ; 
: opc-93 ( TODO )   ." 93 not coded yet" ; 
: opc-94 ( TODO )   ." 94 not coded yet" ; 
: opc-95 ( TODO )   ." 95 not coded yet" ; 
: opc-96 ( TODO )   ." 96 not coded yet" ; 
: opc-97 ( TODO )   ." 97 not coded yet" ; 
: opc-98 ( TODO )   ." 98 not coded yet" ; 
: opc-99 ( TODO )   ." 99 not coded yet" ; 
: opc-9A ( TODO )   ." 9A not coded yet" ; 
: opc-9B ( TODO )   ." 9B not coded yet" ; 
: opc-9C ( stz )   ." 9C not coded yet" ; 
: opc-9D ( TODO )   ." 9D not coded yet" ; 
: opc-9E ( TODO )   ." 9E not coded yet" ; 
: opc-9F ( TODO )   ." 9F not coded yet" ; 
: opc-A0 ( ldy.# )   ." A0 not coded yet" ; 
: opc-A1 ( TODO )   ." A1 not coded yet" ; 
: opc-A2 ( TODO )   ." A2 not coded yet" ; 
: opc-A3 ( TODO )   ." A3 not coded yet" ; 
: opc-A4 ( TODO )   ." A4 not coded yet" ; 
: opc-A5 ( TODO )   ." A5 not coded yet" ; 
: opc-A6 ( TODO )   ." A6 not coded yet" ; 
: opc-A7 ( TODO )   ." A7 not coded yet" ; 
: opc-A8 ( TODO )   ." A8 not coded yet" ; 
: opc-A9 ( TODO )   ." A9 not coded yet" ; 
: opc-AA ( TODO )   ." AA not coded yet" ; 
: opc-AB ( plb )   ." AB not coded yet" ; 
: opc-AC ( TODO )   ." AC not coded yet" ; 
: opc-AD ( TODO )   ." AD not coded yet" ; 
: opc-AE ( TODO )   ." AE not coded yet" ; 
: opc-AF ( TODO )   ." AF not coded yet" ; 
: opc-B0 ( TODO )   ." B0 not coded yet" ; 
: opc-B1 ( TODO )   ." B1 not coded yet" ; 
: opc-B2 ( TODO )   ." B2 not coded yet" ; 
: opc-B3 ( TODO )   ." B3 not coded yet" ; 
: opc-B4 ( TODO )   ." B4 not coded yet" ; 
: opc-B5 ( TODO )   ." B5 not coded yet" ; 
: opc-B6 ( TODO )   ." B6 not coded yet" ; 
: opc-B7 ( TODO )   ." B7 not coded yet" ; 
: opc-B8 ( clv ) v-flag clear ; 
: opc-B9 ( TODO )   ." B9 not coded yet" ; 
: opc-BA ( tsx )   ." BA not coded yet" ; 
: opc-BB ( tyx )   ." BB not coded yet" ; 
: opc-BC ( TODO )   ." BC not coded yet" ; 
: opc-BD ( TODO )   ." BD not coded yet" ; 
: opc-BE ( TODO )   ." BE not coded yet" ; 
: opc-BF ( TODO )   ." BF not coded yet" ; 
: opc-C0 ( cpy.# )   ." C0 not coded yet" ; 
: opc-C1 ( TODO )   ." C1 not coded yet" ; 
: opc-C2 ( rep.# )   ." C2 not coded yet" ; 
: opc-C3 ( TODO )   ." C3 not coded yet" ; 
: opc-C4 ( TODO )   ." C4 not coded yet" ; 
: opc-C5 ( TODO )   ." C5 not coded yet" ; 
: opc-C6 ( TODO )   ." C6 not coded yet" ; 
: opc-C7 ( TODO )   ." C7 not coded yet" ; 
: opc-C8 ( iny )   ." C8 not coded yet" ; 
: opc-C9 ( TODO )   ." C9 not coded yet" ; 
: opc-CA ( dex )   ." dex not coded yet" ; 
: opc-CB ( TODO )   ." CB not coded yet" ; 
: opc-CC ( TODO )   ." CC not coded yet" ; 
: opc-CD ( TODO )   ." CD not coded yet" ; 
: opc-CE ( TODO )   ." CE not coded yet" ; 
: opc-CF ( TODO )   ." CF not coded yet" ; 
: opc-D0 ( TODO )   ." D0 not coded yet" ; 
: opc-D1 ( TODO )   ." D1 not coded yet" ; 
: opc-D2 ( TODO )   ." D2 not coded yet" ; 
: opc-D3 ( TODO )   ." D3 not coded yet" ; 
: opc-D4 ( pei )   ." D4 not coded yet" ; 
: opc-D5 ( TODO )   ." D5 not coded yet" ; 
: opc-D6 ( TODO )   ." D6 not coded yet" ; 
: opc-D7 ( TODO )   ." D7 not coded yet" ; 
: opc-D8 ( cld )   ." D8 not coded yet" ; 
: opc-D9 ( TODO )   ." D9 not coded yet" ; 
: opc-DA ( TODO )   ." DA not coded yet" ; 
: opc-DB ( stp )   \ Stop the Processor, halts emulation
   ." stp not coded yet" ; 
: opc-DC ( TODO )   ." DC not coded yet" ; 
: opc-DD ( TODO )   ." DD not coded yet" ; 
: opc-DE ( TODO )   ." DE not coded yet" ; 
: opc-DF ( TODO )   ." DF not coded yet" ; 
: opc-E0 ( TODO )   ." E0 not coded yet" ; 
: opc-E1 ( TODO )   ." E1 not coded yet" ; 
: opc-E2 ( sep.# )   ." E2 not coded yet" ; 
: opc-E3 ( TODO )   ." E3 not coded yet" ; 
: opc-E4 ( TODO )   ." E4 not coded yet" ; 
: opc-E5 ( TODO )   ." E5 not coded yet" ; 
: opc-E6 ( TODO )   ." E6 not coded yet" ; 
: opc-E7 ( TODO )   ." E7 not coded yet" ; 
: opc-E8 ( TODO )   ." E8 not coded yet" ; 
: opc-E9 ( TODO )   ." E9 not coded yet" ; 
: opc-EA ( nop ) ; 
: opc-EB ( xba )   ." EB not coded yet" ; 
: opc-EC ( TODO )   ." EC not coded yet" ; 
: opc-ED ( TODO )   ." ED not coded yet" ; 
: opc-EE ( TODO )   ." EE not coded yet" ; 
: opc-EF ( sbc.l )   ." EF not coded yet" ; 
: opc-F0 ( beq )   ." F0 not coded yet" ; 
: opc-F1 ( TODO )   ." F1 not coded yet" ; 
: opc-F2 ( TODO )   ." F2 not coded yet" ; 
: opc-F3 ( TODO )   ." F3 not coded yet" ; 
: opc-F4 ( pea )   ." F4 not coded yet" ; 
: opc-F5 ( TODO )   ." F5 not coded yet" ; 
: opc-F6 ( TODO )   ." F6 not coded yet" ; 
: opc-F7 ( TODO )   ." F7 not coded yet" ; 
: opc-F8 ( sed )  d-flag set ; 
: opc-F9 ( TODO )   ." F9 not coded yet" ; 
: opc-FA ( plx )   ." FA not coded yet" ; 
: opc-FB ( xce )   ." FB not coded yet" ; 
: opc-FC ( TODO )   ." FC not coded yet" ; 
: opc-FD ( TODO )   ." FD not coded yet" ; 
: opc-FE ( TODO )   ." FE not coded yet" ; 
: opc-FF ( TODO )   ." FF not coded yet" ; 


\ ---- GENERATE OPCODE JUMP TABLE ----
\ Routine stores xt in table, offset is the opcode of the word in a cell. Use
\ "opcode-jumptable <opcode> cells + @ execute" to call the opcode's word
cr .( Generating opcode jump table ... ) 

: make-opc-jumptable ( -- )
   100 0 do
      i s>d <# # # [char] - hold [char] c hold [char] p hold [char] o hold #>
      find-name name>int ,
   loop ;

create opc-jumptable   make-opc-jumptable 


\ ---- MODE SWITCHES ----

\ switch processor modes (native/emulated) 
: native ( -- )
   ['] status-r16 is status-r ; 
: emulated ( -- ) 
   ['] status-r8 is status-r ; 


\ switch accumulator 8<->16 bit 
: a16  ( -- ) 
   ['] fetch16 is fetch.a ; 
: a8 ( -- ) 
   ['] fetch8 is fetch.a ; 


\ switch X and Y 8<->16 bit
: xy16  ( -- )
   ['] fetch16 is fetch.xy ; 
: xy8 ( -- ) 
   ['] fetch8 is fetch.xy ; 


\ ---- START EMULATION ----
cr .( Starting emulation ...) 

\ TODO set emulation mode 
\ TODO get jump vector 


cr 


