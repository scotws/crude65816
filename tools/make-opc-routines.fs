\ Create Opcode Routine List for 
\ The Crude 65816 Emulator
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ This version 09. Jan 2015

hex 

: make-opc-routines ( -- ) 
   100 0 do
         cr ." : opc-" i s>d <# # # #> type space 
         ." ( TODO ) " 
         ."   " 2E emit  22 emit space
         i s>d <# # # #> type 
         ."  not coded yet"  22 emit ."  ; " 
   loop ;

make-opc-routines

bye 

