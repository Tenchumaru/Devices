// This tests trailing context.  See "Lex and Yacc" page 149.

%%

(a|ab)/ba	return new Token(tokenValue);
zx*/xy*		return new Token(tokenValue);
