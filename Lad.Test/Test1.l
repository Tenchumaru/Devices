// This is the Gevo assembler scanner.

ident		[A-Z_a-z][0-9A-Z_a-z]*
number		[+\-]?[0-9]+
ws			[\a\b\f\r\t\v ]+

%%

	/* These are the opcodes. */
count		return Token.Count;
amplify		return Token.Amplify;
attenuate	return Token.Attenuate;
clip		return Token.Clip;
hpf			return Token.Hpf;
spread		return Token.Spread;
train		return Token.Train;
buy			return Token.Buy;
march		return Token.March;
support		return Token.Support;
investigate	return Token.Investigate;

	/* These are the register opcodes. */
copy		|
cp			return Token.Copy;
add			return Token.Add;
subtract	|
sub			return Token.Subtract;
eor			|
xor			return Token.ExclusiveOr;

	/* These are the registers. */
acc			|
ac			|
r0			return Token.Accumulator;
r1			|
r2			|
r3			return new Token { Symbol = Parser.Register, Value = tokenValue[1] - '0' };

	/* These are the count arguments. */
zero		return Token.Zero;
my			return Token.My;
army		return Token.Army;
enemy		return Token.Enemy;
camp		return Token.Camp;
not			return Token.Not;
negative	return Token.Negative;
one			return Token.One;
"-1"		return Token.NegativeOne;

	/* These are the rest. */
{ident}		{
				Console.Error.WriteLine("invalid token {0}", tokenValue);
				return new Token { Symbol = '?' };
			}
{number}	return new Token { Symbol = Parser.Number, Value = int.Parse(tokenValue) };
"/*"		IgnoreComment();
{ws}		|
#.*			|
\/\/.*		|
;.*			|
\n			;
.			return new Token { Symbol = tokenValue[0] };
