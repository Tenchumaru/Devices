// Page 252

%right sub sup
%token id

%%

E: E sub E sup E { Result.Append("<EsubEsupE>"); } ;
E: E sub E { Result.Append("<EsubE>"); } ;
E: E sup E { Result.Append("<EsupE>"); } ;
E: '{' E '}' { Result.Append("<{E}>"); } ;
E: id { Result.Append("<id>"); } ;
