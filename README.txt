README for The Crude 65816 Emulator
Scot W. Stevenson <scot.stevenson@gmail.com>
First version: 08. Jan 2015
This version: 08. Jan 2015


TL;DR

Make sure you have enought RAM (say, 4 GB) and then start the emulator with 

        gforth -m 1G 

Next, load the emulator with

        include crude65816.fs

Now read the rest of this file and the MANUAL.txt please. 



WHAT'S ALL THIS HERE NOW ANYWAY?



WAIT, I'VE NEVER HEARD OF THE 65816




CALLING THE PROGRAM

By default, Gforth only reserves measly 256k for the dictionary, which is not enough when you are going to simulate a 16M large memory space. We need to call Gforth with something like 

        gforth -m 1G

which should be way more than enough. You can also start the emulator directly from the command line with 

        gforth -m 1G crude65816.fs

Linux users will want to put this in a shell script. 

