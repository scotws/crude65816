\ A Crude Test Suite for a Crude Emulator for the 65816  
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ First version: 22. Sep 2015
\ This version: 23. Sep 2015

\ Use this test suite by starting Gforth normally and then first including first
\ the emulator and then this file. Prefix words in this program with t- to avoid
\ conflict with emulator. Note a lot of these could easily be combined into
\ smaller Forth structures, but we're leaving them the way they are for the
\ moment to focus on other parts of the project. At some point, these should be
\ rewritten.

cr .( Loading test suite ...) 
hex 

\ ==== SET UP STUFF ==== 

\ Set up screen

4 constant t-grid-start-y 
4 constant t-grid-start-x 
17 constant t-info-y
18 constant t-message-y

: .byte ( u8 -- ) s>d <# # # #> type ;
: .t-msb-list ( -- )  10 0 do cr space i . loop ; 
: .t-info ( addr u --)  
   0 t-info-y 2dup  at-xy 79 spaces  at-xy type ; 
: .t-message ( addr u --)  
   0 t-message-y 2dup  at-xy 79 spaces  at-xy type ; 

: .t-entry ( u8 -- ) 
   dup dup 0f and  3 *  t-grid-start-x +  \ X value 
   swap 0f0 and  4 rshift  t-grid-start-y +  \ Y value 
   at-xy .byte ; 

\ Print all opcodes to test the matrix
\ : crudetesttest ( -- )  100 0 do i .t-entry loop ; 

\ Print frame 
page 
." Crude Test Suite for the Crude Emulator for the 65816" cr cr 
."      0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F" cr 
.t-msb-list cr cr 
."      0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F" 
s" (No info yet)" .t-info 
s" (No message yet)" .t-message


\ ==== TEST ROUTINES ==== 

: t-result ( u8 f -- )  
   if dup .t-entry else s" INSTRUCTION FAILED: " .t-message . quit then 
   drop ; 

\ Start tests at 1000
: t-reset-PC  1000 PC !  s" (Intializing PC to 1000)" .t-info ; 

\ We start with the simple instructions and work our way up to the more complex
\ ones instead of going systematically. So we start with NOP. 


\ ==== OPCODES ====


\ ---- No Operation Codes ----

\ TEST: NOP
0EA  s" Testing NOP" .t-info 
opc-ea
true t-result        \ nothing crashed, so we're happy

\ TEST: WDM 
042  s" Testing NOP" .t-info 
t-reset-PC
0042 1000 store16    \ 42 00 
step
true t-result        \ nothing crashed, so we're happy


\ ---- SINGLE-BYTE INC/DEC INSTRUCTIONS ----

\ TEST: INX   TODO Check 16 bit
0E8  s" Testing INX" .t-info 
0ff X !  opc-e8  
   X @  0= 
   z-flag set? and 
   n-flag clear? and 
t-result 

\ TEST: INY  TODO Check 16 bit 
0C8  s" Testing INY" .t-info 
00 Y !  opc-c8
   Y @  1 = 
   n-flag clear? and 
   z-flag clear? and 
t-result 

\ TEST: INC.A  TODO Check 16 bit 
1A  s" Testing INC.A" .t-info 
00 C !  opc-1a
   A  1 = 
   n-flag clear? and 
   z-flag clear? and 
t-result 

\ TEST: DEX  TODO Check 16 bit
0CA  s" Testing DEX" .t-info 
00 X !  opc-ca
   X @  0ff = 
   n-flag set? and
   z-flag clear? and 
t-result 

\ TEST: DEY  TODO Check 16 bit
088  s" Testing DEY" .t-info 
00 Y !  opc-88
   Y @  0ff = 
   n-flag set? and
   z-flag clear? and 
t-result 

\ TEST: DEC.A  TODO Check 16 bit 
3A  s" Testing DEC.A" .t-info 
00 C !  opc-3a
   A  0FF = 
   n-flag set? and 
   z-flag clear? and 
t-result 


\ ---- FLAG INSTRUCTIONS ---- 

\ TEST: CLC
018  s" Testing CLC" .t-info 
opc-18
   z-flag clear? 
t-result 

\ TEST: CLD
0D8  s" Testing CLD" .t-info 
opc-d8
   d-flag clear? 
t-result 

\ TEST: CLI
058  s" Testing CLI" .t-info 
opc-58
   i-flag clear? 
t-result 

\ TEST: CLV
0B8  s" Testing CLV" .t-info 
opc-b8
   v-flag clear? 
t-result 

\ TEST: SEC
038  s" Testing SEC" .t-info 
opc-38
   c-flag set? 
t-result 

\ TEST: SED
0f8  s" Testing SED" .t-info 
opc-f8
   c-flag set? 
t-result 

\ TEST: SED
078  s" Testing SEI" .t-info 
opc-78
   c-flag set? 
t-result 


\ ---- SWITCH VARIOUS MODES ----

\ TEST: XCE  TODO check all the register sizes
\ This assumes working SEC/CLC flags, and that we are in emulation mode
\ (e-flag is set)
0FB s" Testing XCE" .t-info 
opc-18   \ CLC 
opc-FB   \ XCE swap carry/emulation
   c-flag set?
   e-flag clear? and 
t-result
opc-FB   \ switch back to emulation mode for further testing


\ ---- ADDRESSING MODE TESTS ----

\ HIER HIER 

\ ---- LOAD AND STORE INSTRUCTIONS ----

\ TEST: LDA IMMEDIATE  TODO check 16 bit
\ TODO replace this by mode tests
0A9 s" Testing LDA.#" .t-info 
t-reset-PC
00A9 1000 store16    \ A9 00 
0ff C !  step
   z-flag set?
   n-flag clear? and 
   A 0= and
   B 0= and
t-result 


\ ==== SUCCESSFULLY COMPLETED ====

s" *** ALL TESTS SUCCESSFUL ***" .t-message cr 

