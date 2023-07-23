// Page 218

%type<int> E T F
%token<int> id

%%

E: E '+' T { $$ = Result = $1 + $3; } ;
E: T { $$ = Result = $1; } ;
T: T '*' F { $$ = Result = $1 * $3; } ;
T: F { $$ = Result = $1; } ;
F: '(' E ')' { $$ = Result = $2; } ;
F: id ;
