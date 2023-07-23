%type<Nfa> rx choice concat kleene simple
%type<RangeSymbol> class range
%token<string> NamedExpression
%token<int> Number
%token<char> Symbol
%token OSBC

%%

rx: { Result.Append("<>"); } ;
rx: choice { Result.Append("<rx -> choice>"); } ;
choice: concat ;
choice: choice '|' concat { Result.Append("<choice -> choice | concat>"); } ;
concat: kleene ;
concat: concat kleene { Result.Append("<concat -> concat kleene>"); } ;
kleene: simple ;
kleene: simple '*' { Result.Append("<kleene -> simple*>"); } ;
kleene: simple '+' { Result.Append("<kleene -> simple+>"); } ;
kleene: simple '?' { Result.Append("<kleene -> simple?>"); } ;
kleene: simple '{' Number '}' { Result.Append("<kleene -> simple{#}>"); } ;
kleene: simple '{' Number separator Number '}' { Result.Append("<kleene -> simple{#,#}>"); } ;
separator: '-' ;
separator: ',' ;
simple: Symbol { Result.Append("<simple -> Symbol>"); } ;
simple: '.' { Result.Append("<simple -> .>"); } ;
simple: NamedExpression { Result.Append("<simple -> NamedExpression>"); } ;
simple: '[' class ']' { Result.Append("<simple -> [class]>"); } ;
simple: OSBC class ']' { Result.Append("<simple -> [^class]>"); } ;
simple: '(' choice ')' { Result.Append("<simple -> (choice)>"); } ;
class: range ;
class: class range { Result.Append("<class -> class range>"); } ;
range: Symbol { Result.Append("<range -> Symbol>"); } ;
range: Symbol '-' Symbol { Result.Append("<range -> Symbol-Symbol>"); } ;
