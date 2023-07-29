// This is the Flurbie scanner ported to C#.

%{
using System.Globalization;
%}

ws      [\a\b\f\r\t\v ]+
rem     ;.*
reg1    r[012]?[0-9]
reg2    r3[01]
id      [_a-z][_0-9a-z]*
hex     \$[0-9a-f]+
num     [0-9]+

%%

{ws}    |
{rem}   ;
{hex}   { return DBOUT(int.Parse(tokenValue[1..], NumberStyles.HexNumber), tokenValue, VALUE); }
{num}   { return DBOUT(int.Parse(tokenValue), tokenValue, VALUE); }
addc    { return DBOUT( 1 << 23, tokenValue, OP); }
nand    { return DBOUT( 9 << 23, tokenValue, OP); }
subb    { return DBOUT( 3 << 23, tokenValue, OP); }
udiv    { return DBOUT( 7 << 23, tokenValue, OP); }
umul    { return DBOUT( 5 << 23, tokenValue, OP); }
xnor    { return DBOUT(13 << 23, tokenValue, OP); }
add     { return DBOUT( 0 << 23, tokenValue, OP); }
and     { return DBOUT( 8 << 23, tokenValue, OP); }
div     { return DBOUT( 6 << 23, tokenValue, OP); }
mul     { return DBOUT( 4 << 23, tokenValue, OP); }
nor     { return DBOUT(11 << 23, tokenValue, OP); }
sub     { return DBOUT( 2 << 23, tokenValue, OP); }
xor     { return DBOUT(12 << 23, tokenValue, OP); }
or      { return DBOUT(10 << 23, tokenValue, OP); }
ldi     { return DBOUT(LDI, tokenValue, OP); }
xorih   { return DBOUT(XORIH, tokenValue, OP); }
ld      { return DBOUT(LD, tokenValue, OP); }
st      { return DBOUT(ST, tokenValue, OP); }
int     { return DBOUT(null, tokenValue, INT); }
nop     { return DBOUT(null, tokenValue, NOP); }
set     { return DBOUT(null, tokenValue, SET); }
cx      { return DBOUT(CX, tokenValue, SOP); }
pc      { return DBOUT(pc_index, tokenValue, REG); }
{reg1}  |
{reg2}  { return DBOUT(int.Parse(tokenValue[1..]), tokenValue, REG); }
{id}    { return DBOUT(tokenValue, tokenValue, ID); }
">>>"   return DBOUT(null, tokenValue, ASR);
"<<"    return DBOUT(null, tokenValue, SHL);
">>"    return DBOUT(null, tokenValue, LSR);
"=="    return DBOUT(null, tokenValue, EQ);
"!="    return DBOUT(null, tokenValue, NE);
"<="    return DBOUT(null, tokenValue, LE);
">="    return DBOUT(null, tokenValue, GE);
"&&"    return DBOUT(null, tokenValue, AND);
"||"    return DBOUT(null, tokenValue, OR);
\n      return DBOUT(null, tokenValue, '\n');
.       return DBOUT(null, tokenValue, char.ToLowerInvariant(tokenValue[0]));
