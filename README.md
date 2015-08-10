# A Crude 65816 Emulator

Scot W. Stevenson <scot.stevenson@gmail.com>

First version: 08. Jan 2015

This version: 25. May 2015


## TL;DR

This is a PRE-ALPHA version of an emulator for the 65816 8/16-bit CPU in Gforth.
Start it with 

```
gforth -m 1G 
```

and then load the emulator with

```
include crude65816.fs
```

You an either "run" or "step" the default monitor program. That doesn't do jack
at the moment, really, but will in the future. PRE-ALPHA, remember? There is a
discussion of the program [http://forum.6502.org/viewtopic.php?f=8&t=3306] (at
6502.org).

## WHAT'S ALL THIS HERE NOW ANYWAY?

The 65816 is the [http://en.wikipedia.org/wiki/WDC_65816/65802] ("big sibling")
of the venerable 6502 8-bit processor. It is a hybrid processor that can run in
16-bit ("native") and 8-bit ("emulated") mode.

After bulding a 6502 machine as a hobby, [http://uebersquirrel.blogspot.de/]
(the "Übersquirrel" Mark Zero) (ÜSqM0), I found eight bits to be too limiting.
The 65816 is the logical next step up, since you can reuse the 8-bit code at
first. 

Unfortunately, emulators for the 65816 are few and far between, and so I decided
I would have to write my own. This is it. It is horribly crude -- hence the
name. For instance, it completely ignores all timing and clock considerations.

## BUT MOTHER OF DRAGONS, WHY IN FORTH?

The ÜSqM0 taught me how amazingly powerful Forth is on simple hardware. In fact,
one of the main reasons for switching to the 65816 is to be able to do more with
Forth. In fact, the Crude Emulator relies on modern hardware to work and just
assumes we have enough RAM and a fast processor for everything. 

See `MANUAL.txt` for further information.

## DEVELOPMENT

This program is a hobby, and is developed in fits and starts. Feedback is most
welcome. 
