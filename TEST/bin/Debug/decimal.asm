; verify decimal mode behavior
;
; returns:
;   error = 0 if the test passed
;   error = 1 if the test failed
;
; this routine requires 17 bytes of ram -- 1 byte each for:
;   ar, cf, da, dnvzc, error, ha, hnvzc, n1, n1h, n1l, n2, n2l, nf, vf, and zf
; and 2 bytes for n2h
;
; variables:
;   n1 and n2 are the two numbers to be added or subtracted
;   n1h, n1l, n2h, and n2l are the upper 4 bits and lower 4 bits of n1 and n2
;   da and dnvzc are the actual accumulator and flag results in decimal mode
;   ha and hnvzc are the accumulator and flag results when n1 and n2 are
;     added or subtracted using binary arithmetic
;   ar, nf, vf, zf, and cf are the predicted decimal mode accumulator and
;     flag results, calculated using binary arithmetic
;
; this program takes approximately 1 minute at 1 mhz (a few seconds more on
; a 65c02 than a 6502 or 65816)
;

error = $10
n1 = $11
n2 = $12
n1h = $13
n1l = $14
n2h = $15
n2l = $17
dnvzc = $18
ha = $19
hnvzc = $1A
ar = $1B
nf = $1C
vf = $1D
zf = $1E
cf = $1F
da = $20
		org $1000

test    ldy #1    ; initialize y (used to loop through carry flag values)
        sty error ; store 1 in error until the test passes
        lda #0    ; initialize n1 and n2
        sta n1
        sta n2
loop1   lda n2    ; n2l = n2 & $0f
        and #$0f  ; [1] see text
        sta n2l
        lda n2    ; n2h = n2 & $f0
        and #$f0  ; [2] see text
        sta n2h
        ora #$0f  ; n2h+1 = (n2 & $f0) + $0f
        sta n2h+1
loop2   lda n1    ; n1l = n1 & $0f
        and #$0f  ; [3] see text
        sta n1l
        lda n1    ; n1h = n1 & $f0
        and #$f0  ; [4] see text
        sta n1h
        jsr add
        jsr a6502
        jsr compare
        bne done
        jsr sub
        jsr s6502
        jsr compare
        bne done
        inc n1    ; [5] see text
        bne loop2 ; loop through all 256 values of n1
        inc n2    ; [6] see text
        bne loop1 ; loop through all 256 values of n2
        dey
        bpl loop1 ; loop through both values of the carry flag
        lda #0    ; test passed, so store 0 in error
        sta error
done    jmp *	  ; trap when program finished

; calculate the actual decimal mode accumulator and flags, the accumulator
; and flag results when n1 is added to n2 using binary arithmetic, the
; predicted accumulator result, the predicted carry flag, and the predicted
; v flag
;
add     sed       ; decimal mode
        cpy #1    ; set carry if y = 1, clear carry if y = 0
        lda n1
        adc n2
        sta da    ; actual accumulator result in decimal mode
        php
        pla
        sta dnvzc ; actual flags result in decimal mode
        cld       ; binary mode
        cpy #1    ; set carry if y = 1, clear carry if y = 0
        lda n1
        adc n2
        sta ha    ; accumulator result of n1+n2 using binary arithmetic

        php
        pla
        sta hnvzc ; flags result of n1+n2 using binary arithmetic
        cpy #1
        lda n1l
        adc n2l
        cmp #$0a
        ldx #0
        bcc a1
        inx
        adc #5    ; add 6 (carry is set)
        and #$0f
        sec
a1      ora n1h
;
; if n1l + n2l <  $0a, then add n2 & $f0
; if n1l + n2l >= $0a, then add (n2 & $f0) + $0f + 1 (carry is set)
;
        adc n2h,x
        php
        bcs a2
        cmp #$a0
        bcc a3
a2      adc #$5f  ; add $60 (carry is set)
        sec
a3      sta ar    ; predicted accumulator result
        php
        pla
        sta cf    ; predicted carry result
        pla
;
; note that all 8 bits of the p register are stored in vf
;
        sta vf    ; predicted v flags
        rts

; calculate the actual decimal mode accumulator and flags, and the
; accumulator and flag results when n2 is subtracted from n1 using binary
; arithmetic
;
sub     sed       ; decimal mode
        cpy #1    ; set carry if y = 1, clear carry if y = 0
        lda n1
        sbc n2
        sta da    ; actual accumulator result in decimal mode
        php
        pla
        sta dnvzc ; actual flags result in decimal mode
        cld       ; binary mode
        cpy #1    ; set carry if y = 1, clear carry if y = 0
        lda n1
        sbc n2
        sta ha    ; accumulator result of n1-n2 using binary arithmetic

        php
        pla
        sta hnvzc ; flags result of n1-n2 using binary arithmetic
        rts

; calculate the predicted sbc accumulator result for the 6502 and 65816

;
sub1    cpy #1    ; set carry if y = 1, clear carry if y = 0
        lda n1l
        sbc n2l
        ldx #0
        bcs s11
        inx
        sbc #5    ; subtract 6 (carry is clear)
        and #$0f
        clc
s11     ora n1h
;
; if n1l - n2l >= 0, then subtract n2 & $f0
; if n1l - n2l <  0, then subtract (n2 & $f0) + $0f + 1 (carry is clear)
;
        sbc n2h,x
        bcs s12
        sbc #$5f  ; subtract $60 (carry is clear)
s12     sta ar
        rts

; calculate the predicted sbc accumulator result for the 6502 and 65c02

;
sub2    cpy #1    ; set carry if y = 1, clear carry if y = 0
        lda n1l
        sbc n2l
        ldx #0
        bcs s21
        inx
        and #$0f
        clc
s21     ora n1h
;
; if n1l - n2l >= 0, then subtract n2 & $f0
; if n1l - n2l <  0, then subtract (n2 & $f0) + $0f + 1 (carry is clear)
;
        sbc n2h,x
        bcs s22
        sbc #$5f   ; subtract $60 (carry is clear)
s22     cpx #0
        beq s23
        sbc #6
s23     sta ar     ; predicted accumulator result
        rts

; compare accumulator actual results to predicted results
;
; return:
;   z flag = 1 (beq branch) if same
;   z flag = 0 (bne branch) if different
;
compare lda da
        cmp ar
        bne c1
        ; lda dnvzc ; [7] see text
        ; eor nf
        ; and #$80  ; mask off n flag
        ; bne c1
        ; lda dnvzc ; [8] see text
        ; eor vf
        ; and #$40  ; mask off v flag
        ; bne c1    ; [9] see text
        ; lda dnvzc
        ; eor zf    ; mask off z flag
        ; and #2
        ; bne c1    ; [10] see text
        lda dnvzc
        eor cf
        and #1    ; mask off c flag
c1      rts

; these routines store the predicted values for adc and sbc for the 6502,
; 65c02, and 65816 in ar, cf, nf, vf, and zf

a6502   lda vf
;
; since all 8 bits of the p register were stored in vf, bit 7 of vf contains
; the n flag for nf
;
        sta nf
        lda hnvzc
        sta zf
        rts

s6502   jsr sub1
        lda hnvzc
        sta nf
        sta vf
        sta zf
        sta cf
        rts
