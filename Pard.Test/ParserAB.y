%%

S: { Result.Append("<empty>"); } ;
S: 'a' S 'b' { Result.Append("<aSb>"); } ;
