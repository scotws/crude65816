\ I/O stuff for A Crude Emulator for the 65816 
\ Scot W. Stevenson <scot.stevenson@gmail.com>
\ First version: 30. Jul 2015
\ This version: 15. Sep 2015

\ crude65816.fs must load this file after config.fs where putchr and getchr are
\ defined

\ Some day, this might emulate chips such as the 6522 or the 6551.  Right now,
\ it just provides putchr and getchr functionality with the addresses provided
\ in config.fs
: printchar ( n -- ) emit ; 
: readchar ( -- n ) key ; 


\ Handle all special addresses. There is no real reason to create two separate
\ tables except that it cuts down on the number of loopings that store and read
\ instructions have to go through.
create store-addrs
   here 0 ,                
   putchr ,  ' printchar , \ ## add new routines below this line ## 
   here  swap !   \ save address of last entry in table in its first entry

\ The equivalent for special read addresses
create read-addrs
   here 0 , 
   getchr ,  ' readchar ,  \ ### add new routines below this line
   here  swap !   \ save address of last entry in table in its first entry


\ Common routine for special i/o access. Takes the 65816 address in question and
\ the address of the table, either STORE-ADDRS or READ-ADDRS. Because Forth
\ doesn't have a BREAK statement for indefinite loops, we count the number of
\ elements and put the count in the first address of the array. See
\ http://forum.6502.org/viewtopic.php?f=9&t=3391 for a discussion of this code
: special-io? ( 65addr table -- 65addr 0|xt)
   false swap      ( 65addr 0 table )         \ default return value
   dup @           ( 65addr 0 addr-s addr-e ) \ start and end of table
   swap cell+      ( 65addr 0 addr-e addr-s+cell) 
   ?do             ( 65addr 0 ) 
      over i @     ( 65addr 0 65addr 65addr ) 
      = if  drop i cell+ @  leave then 
   [ cell 2* ] literal +loop ; 

: special-store? ( 65addr - 0|xt)  store-addrs special-io? ; 
: special-read? ( 65addr - 0|xt)  read-addrs special-io? ; 
   
