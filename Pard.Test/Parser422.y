// Page 247

%type<int> E
%left '+'
%left '*'
%token<int> id

%%

E: E '*' E { $$ = Result = $1 * $3; } ;
E: E '+' E { $$ = Result = $1 + $3; } ;
E: '(' E ')' { $$ = Result = $2; } ;
E: id ;
