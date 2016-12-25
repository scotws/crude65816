# The Mock Mensch Monitor ROM file 

Scot W. Stevenson <scot.stevenson@gmail.com> 

This is a ROM file for the [Crude 65816
emulator](https://github.com/scotws/crude65816) that emulates certain utility
routines of the W65C265SXB' Mensch Monitor from WDC to make development with
that single board computer easier. It does _not_ function as a monitor itself. 

**NOTE THIS IS ALPHA STAGE SOFTWARE.**

## Utility routines currently emulated

All these routines must be called with a Subroutine Long Jump (JSL / jsr.l)
instruction.

| Name | Location | Function |
| :--- | :------: | :------- |
| GET_CHR      | 00:E036 | Read one character from input, no echo | 
| PUT_CHR      | 00:E042 | Print a single character | 
| PUT_STR      | 00:E04E | Print a string | 
| SEND_CR      | 00:E066 | Print a Carriage Return | 

## Testing 

This file includes a test suite. To test, make sure the correct files are
uncommented in config.fs. Then, start Gforth:

```
gforth -m 18M
```

and then load the emulator with

```
include crude65816.fs
```

The Mock Mensch Monitor routines start at 00:e000, while the test suite starts
at 00:8000. Therefore, we run the test with:

```
8000 PC !
run
```
See the source code for details.

# Chances related to the Mensch Monitor code

*SEND_CR* does not send a CR ($0D) but rather a line feed (LF, $0A) character,
because CR causes the text to overwrite itself on normal terminals.


## Links

Consult the official [Mensch Monitor ROM Reference
Manual](http://www.westerndesigncenter.com/Wdc/documentation/265monrom.pdf), the
official [Assembler code listing of the Mensch
Monitor](http://www.westerndesigncenter.com/wdc/documentation/265iromlist.pdf)
or the [Most Very Unofficial Guide to the
W65C265SXB](https://github.com/scotws/265SXB-Guide) for details on how these
routines work.

## Interaction with the Crude 65816 Emulator

The MMM was created to work with [A Crude Emulator for the 65186 in
Forth](https://github.com/scotws/crude65816). The emulator assumes that the name
of the file is `mmm.bin` in this directory. Make sure to uncomment the correct
lines in config.sys, that is, both the LOADROM entry as well as the addresses
for the emulator's PUTCHR and GETCHR routines.
