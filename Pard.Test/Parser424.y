// Page 250

%right 'e'

%%

S: 'i' S 'e' S { Result.Append("<iSeS>"); } ;
S: 'i' S { Result.Append("<iS>"); } ;
S: 'a' { Result.Append("<a>"); } ;
