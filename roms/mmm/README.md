# The Mock Mensch Monitor ROM file 

Scot W. Stevenson <scot.stevenson@gmail.com> 

This is a ROM file for the W65C265SXB that emulates certain utility routines for
the Mensch Monitor from WDC to make development with that single board computer
easier. It does _not_ function as a monitor itself. 

## Utility routines currently emulated

All these routines must be called with a Subroutine Long Jump (JSL / jsr.l)
instruction.

| Name | Location | Function |
| :--- | :------: | :------- |
| GET_CHR      | 00:E036 | Read one character from input, no echo | 
| GET_PUT_CHR  | 00:E03C | Read one character from input, with echo | 
| GET_STR      | 00:E03F | Get a string from input, with echo |
| PUT_CHR      | 00:E042 | Print a single character | 
| PUT_STR      | 00:E04E | Print a string | 
| SEND_CR      | 00:E066 | Print a Carriage Return | 

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

## Syntax of the Mock Mensch Monitor 

This program is written in Typist's Assembler Notation, see
[https://docs.google.com/document/d/16Sv3Y-3rHPXyxT1J3zLBVq4reSPYtY2G6OSojNTm4SQ](https://docs.google.com/document/d/16Sv3Y-3rHPXyxT1J3zLBVq4reSPYtY2G6OSojNTm4SQ)
for details.
