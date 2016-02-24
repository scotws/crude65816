# Manual for A Crude 65816 Emulator 
Scot W. Stevenson <scot.stevenson@gmail.com>  
First version: 09. Jan 2015  
This version: 24. Feb 2016  


> THIS DOCUMENT IS CURRENTLY MERELY A COLLECTION OF NOTES. WHEN IN 
> DOUBT, USE THE SOURCE CODE. 

This is an emulator for the 65816 MPU, the "big sibling" of the famous 8-bit
6502 and 65c02 processors. It was created so I could more easily write
a version of Forth for this processor after being frustrated by the limitations
of the smaller processor while creating [Tali Forth for the 65c02](http://forum.6502.org/viewtopic.php?f=9&t=2926). 

Because of this, the emulator in Forth itself was the logical decision. It is
written in Gforth, the [GNU version of
Forth](https://www.gnu.org/software/gforth/). But fear not -- it is perfectly
usable even if you have never seen a line of Forth in your life. It does,
however, offer various tricks for Forthwrights.  

This manual provides a general introduction and guide to the emulator. The
source code is heavily commented and should be consulted for details. Please
note that at this moment, the emulator's status is ALPHA, defined as
"everything does something, sometimes even the right thing". There has not been
extensive testing of any part of the program yet.  


## WHAT IT DOES

The Crude Emulator provides an environment to test 65816 binary programs.
Because of the 8/16-bit hybrid nature of the MPU, it also functions as a 6502
and 65c02 emulator when the processor is in Emulator Mode. The program allows
defining various files as ROM areas via the main CONFIG.FS file. The emulator
makes an spirited attempt to handle the correct wrapping of instructions at
page and bank boundries. There is a primitive provision for interrupt testing. 

The emulator offers a _very_ crude emulation of some of the core utility routines present in the Mensch Monitor ROM shipped by WDC in the [W65C265SXB](http://wdc65xx.com/134_265_SXB-engineering-development-system/gettingstarted/). 


## WHAT IT DOESN'T DO

The emulator does not track the system clock and in fact has no concept of time
at all. Things are done when they are done. Currently, there is no emulation of
support chips such as the VIA 6522. Instead, there are "special addresses" that
can be defined in the configuration file. 

There are a host of known issues where the emulator does not behave like the
real silicon, mostly related to BCD addition and subtraction. See the bottom of
this document for details. 


## CALLING THE PROGRAM

By default, Gforth reserves measly 256k for the dictionary, which is not enough
when you are going to simulate a 16M large memory space. We need to call Gforth
with at least 

```gforth -m 18M``` 

You can also start the emulator directly from the command line with

```gforth -m 18M crude65816.fs```

You might want to put this in a shell script. 

The central configuration file is CONFIG.FS in the main folder. Its main
function is to allow the user to load various binary files to simulate ROMs
that are loaded at boot. There are various examples included. Note some of them
might be for internal testing. 


## IMPORTING ROM FILES

The Crude Emulator has a very, well, crude memory model: It simply reserves 16
MByte of RAM as the complete memory range of the 65816. During boot, the
emulator loads the contents of the files given in config.fs to the memory
locations in the lines designated with LOADROM. This lets you simulate ROM
data. Note, however, there is no write protection for ROM; the emulator will
happily let you change any of these values. 

The current ROM file is a primitive test program. 


## USING THE PROGRAM

In case you are new to Forth, know this: it is not a programming language, but
a system to create specialized languages. As such, with the Crude Emulator you
have access to all Forth commands such as DUMP, and in single-step mode can
assign values to, say, the 65816 registers. 

The Crude Emulator current supports the following addition commands. Note that
by Forth convention, a command that starts with a dot (for instance `.state`)
prints some information to the screen. 

        .direct         - print the Direct Page based on D register
        .stack          - print the 65816 stack if in emulated mode
        .state          - print the CPU state (register content, etc)
        65dump          - dump memory range based on 65816 addresses
        abort-i         - trigger the ABORT interrupt 
        bye             - end emulation (normal Forth command) 
        fetch8          - fetch a byte from 24-bit address on Forth stack
        fetch16         - fetch a double byte from 24-bit address on Forth stack
        fetch24         - fetch three bytes from 24-bit address on Forth stack
        emulate         - switch the CPU to emulated (8-bit) mode
        irq-i           - trigger the IRQ interrupt 
        native          - switch the CPU to native (16-bit) mode
        nmi-i           - tritter the NMI interrupt 
        reset-i         - trigger the RESET interrupt 
        run             - run the emulator continuously
        step            - run one instruction in single-step mode

STEP is usually used in combination with .STATE ("step .state") to show the
status of the emulator after every step. Because of the nature of Forth, any
word defined in the emulator can be used from the command line. To force the
machine to run at a certain address, save it in the PC: 

```
00e000 PC !
```

followed by a "run" or "step". The same procedure will let you store a value in, say, the A register:

```
61 C !
```

(note that the name is not "A") or X, Y, D, S, DBR, and PBR. To change the register sizes, use words
```
a:8 
a:16 
xy:8 
xy:16
```
from the command line. 

> For instance, to test the PUT_CHR routine of the Mock Mensch Monitor (MMM) ROM
> included in the emulator, follow these steps (assuming that MMM was loaded
> through config.fs):
```
        native          \ switches 65816 to native mode
        a:8             \ make A register 8 bit
        61 C !          \ save ASCII value for "a" in A register
        0e04b PC !      \ move to start of emulated PUT_CHR routine
        step .state     \ walk through the routine
```
> At some point, a small "a" should appear.

Interrupts can be triggered by hand as well: 
```
        reset-i
```
To use RUN and STEP during testing, use the WAI instruction in the 65816 code
to pause execution, and STP to stop the system. 


## OTHER USEFUL COMBINATIONS

To walk through the program 
```
        step .state
```
To walk through the program, showing the stack with every step (emulation mode only)
```
        step .state .stack
```
To monitor a memory address every step, we can use the FETCH instructions. To
watch what happens to $000000, for instance: 
```
        step .state  ." DP 00:"  0000 fetch16 . 
```
Note FETCH does not wrap at the bank boundry, use FETCH/WRAP for this. 



## HALTING THE EMULATION 

In many 6502 emulators, the BRK instruction is used to give control back to the
emulator. We use the 65816's STP instruction, which halts the processor. After
STP, you can resume the emulation with either STEP or RUN from the same spot.  



## EMULATING INTERRUPTS

To test interrupts, place a WAI instruction in the code at the place where you
want the interrupt to trigger. Run the code until then. When the emulation
stops, type the instruction for the interrupt (for instance irq-i), and then
continue the run. 

Put differently, the emulator is too crude to allow interrupts during the
execution of one word. Because of this, questions such if MVN and MVP are
interruptable is not relevant.

Note that after an interrupt, the emulator halts to let the user check things.
It must be restared with RUN or STEP. 



## NAMES OF MODES 

Internally, the Crude Emulator internally uses an assembler syntax called
[Typist's Assembler Notation](https://github.com/scotws/tasm65816). TAN
takes care of various problems with the "classical" syntax, especially with the
65816, is faster to type (hence the name), and comes in variants for "normal"
postfix assemblers and those such as Forth with a prefix notation. Briefly, the
modes are: 
```
    MODE                      WDC SYNTAX       TYPIST'S SYNTAX

    implied                   dex                    dex
    accumulator               inc                    inc.a
    immediate                 lda #$00            00 lda.#
    absolute                  lda $1000         1000 lda
    absolute x indexed        lda $1000,x       1000 lda.x
    absolute y indexed        lda $1000,y       1000 lda.y
    absolute indirect         jmp ($1000)       1000 jmp.i
    indexed indirect          jmp ($1000,x)     1000 jmp.xi
    absolute long             jmp $101000     101000 jmp.l    (65816)
    absolute long x indexed   jmp $101000,x   101000 jmp.lx   (65816)
    absolute indirect long    jmp [$1000]       1000 jmp.il   (65816)
    direct page               lda $10             10 lda.d
    direct page x indexed     lda $10,x           10 lda.dx
    direct page y indexed     lda $10,y           10 lda.dy
    direct page indirect      lda ($10)           10 lda.di
    dp indirect x indexed     lda ($10,x)         10 lda.dxi
    dp indirect long          lda [$10]           10 lda.dil  (65816) 
    dp indirect y indexed     lda ($10),y         10 lda.diy  
    dp indirect long y index  lda [$10],y         10 lda.dily (65816)
    relative                  bne $2f00         2f00 bne
    relative long             brl $20f000     20f000 brl      (65816)
    stack relative            lda 3,S              3 lda.s    (65816)
    stack rel ind y indexed   lda (3,S),y          3 lda.siy  (65816)
    block move                mvp 0,0            0 0 mvp      (65816) 
```
However, you do not need to know TAN (or any other notation, for that matter)
to use the emulator. Where it is most important, both variants are given in the
source code. 



## NOTES ON INTERNAL CONSTRUCTION FOR FORTHWRIGHTS

The Crude Emulator puts all registers in variables. Experiments during
development with either A/C or PC as TOS made working with the emulator too
complicated. Instead, the Forth Data Stack must be empty after every RUN or
STEP command. 

Because of the complicated wrapping rules for the 65816, in this stage of
development various routines have extra "masking" instructions (for instance,
MASK16) that are probably not necessary. These are marked with "paranoid" in
the source code. 



## KNOWN ISSUES 

The Overflow Flag (v) is not correctly set in Decimal mode. See
http://www.6502.org/tutorials/vflag.html and
http://www.righto.com/2012/12/the-6502-overflow-flag-explained.html for
details. This bug has low priority. 

The N and Z flag do not behave as required during subtraction in decimal
mode. See source text for details. This bug will be corrected during ALPHA.

The ABORT interrupt does not complete the instruction, throwing away the
results, and then rerunning it, but instead is run after an instruction. This
bug has low priority.  



## OTHER NOTES

Though the documentation is unclear on this subject, tests have shown that you
can relocate the Direct Page in Emulation Mode, see
http://forum.6502.org/viewtopic.php?f=8&t=3459&p=40389#p40370



## LITERATURE

List of paper and online literature used, with date of last access where appropriate. 

### Books:

"Forth Programmer's Handbook", 3rd edition. Conklin and Rather (2007) 

### Special topics: 

Interrupt system: http://sbc.bcstechnology.net/65c816interrupts.html   
Wrapping on banks and pages: http://forum.6502.org/viewtopic.php?f=8&t=3459&start=30#p40855

