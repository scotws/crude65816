\ A Crude Test Suite for a Crude Emulator for the 65816  
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ First version: 22. Sep 2015
\ This version: 23. Sep 2015

\ Use this test suite by starting Gforth normally and then first including first
\ the emulator and then this file

\ Prefix words in this program with t- to avoid conflict with emulator

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
   if dup .t-entry else s" INSTRUCTION FAILED: " .t-message . quit then ; 

\ Start tests at 1000
1000 PC !  s" (Intializing PC to 1000)" .t-info 

\ We start with the simple instructions and work our way up to the more complex
\ ones instead of going systematically. So we start with NOP. 


\ ==== OPCODES ====

\ TEST: NOP
0EA  s" Testing NOP" .t-info 
PC @  opc-ea  PC+1  PC @  swap -
1 = 
t-result 

\ ---- SINGLE-BYTE INC INSTRUCTIONS ----

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

\ TEST: CLD
058  s" Testing CLI" .t-info 
opc-58
   i-flag clear? 
t-result 

\ TEST: CLV
0B8  s" Testing CLV" .t-info 
opc-b8
   v-flag clear? 
t-result 



\ ==== SUCCESSFULLY COMPLETED ====

s" *** ALL TESTS SUCCESSFUL ***" .t-message cr 

