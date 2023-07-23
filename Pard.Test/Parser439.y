// Page 4.20 on page 229

%token id

%%

S: L '=' R { Result.Append("<S->L=R>"); } ;
S: R { Result.Append("<S->R>"); } ;
L: '*' R { Result.Append("<L->*R>"); } ;
L: id { Result.Append("<L->id>"); } ;
R: L {Result.Append("<R->L>");  } ;
