# A Crude Emulator for the 65816 in Forth

Scot W. Stevenson <scot.stevenson@gmail.com>


### TL;DR

This is an ALPHA version of an emulator for the 65816 8/16-bit CPU in Gforth.
"Alpha" means that "everything does something, sometimes even the right thing".
If you must jump in without reading the documentation, start it with 

```
gforth -m 64M 
```

and then load the emulator with

```
include crude65816.fs
```

You an either "run" or "step" through whatever is setup in CONFIG.FS. There is a
discussion of the program [at 6502.org]
(http://forum.6502.org/viewtopic.php?f=8&t=3306).

### SO WHAT'S THIS NOW?

The 65816 is the ["big sibling"](http://en.wikipedia.org/wiki/WDC_65816/65802) 
of the venerable 6502 8-bit processor. It is a hybrid processor that can run in
16-bit ("native") and 8-bit ("emulated") mode.

After bulding a 6502 machine as a hobby, [the "Übersquirrel" Mark Zero]
(http://uebersquirrel.blogspot.de/) (ÜSqM0), I found eight bits to be too
limiting.  The 65816 is the logical next step up, since you can reuse the 8-bit
code at first. 

Unfortunately, emulators for the 65816 are few and far between, and so I decided
I would have to write my own. This is it. It is horribly crude -- hence the
name. For instance, it completely ignores all timing and clock considerations.
But it works. 

### BUT MOTHER OF DRAGONS, WHY IN FORTH?

The Übersquirrel Mark Zero taught me how amazingly powerful Forth is on simple
hardware, how short the programs can be, and how fun it is just to code it. In
fact one of the main reasons for switching to the 65816 is to be able to do more
with Forth. The Crude Emulator itself relies on modern hardware to work and just
assumes we have enough RAM and a fast processor for everything. 

See `docs/MANUAL.txt` for further information.

### DEVELOPMENT SPEED

This program is a hobby, and is developed in fits and starts. Feedback is  most
welcome. 
