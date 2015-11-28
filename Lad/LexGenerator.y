%using System.Text
%using IntPair=System.Collections.Generic.KeyValuePair<int, int>
%using CharPair=System.Collections.Generic.KeyValuePair<char, char>
%using StringList=System.Collections.Generic.List<string>
%using NfaList=System.Collections.Generic.List<Lad.Nfa>

%token <string> Identifier NamedExpression OptionValue Action PDefine PUsing PP
%token <int> POption
%token <char> Symbol
%token <IntPair> Single Double
%token Default NextExpression NegativeClass Gtocb

%type <StringList> idents csv
%type <int> opts
%type <CharPair> range
%type <NfaList> rexprs rule rules grule grules
%type <Nfa> rexpr bol choice concat kleene simple
%type <RangeSymbol> class
%type <bool> caret

%%

lad:	sec1 PP sec2 opt3
	;

sec1:	/* nothing */
	|	sec1 decl
	;

decl:	POption opts '\n' { /* $1, the line number, is an inheritable attribute. */ }
	|	Identifier rexpr '\n' { namedExpressions.Add($1, $2); }
//	|	PI Identifier '\n' { initialRuleGroupName= $2; }
//	|	PR idents '\n' { AddRuleGroupNames($2, false); }
//	|	PX idents '\n' { AddRuleGroupNames($2, true); }
	|	PDefine '\n' { definitions.Add($1); }
	|	PUsing '\n' { usingDirectives.Add($1); }
	|	error '\n' { ReportError(scanner.LineNumber - 1, "unexpected characters"); }
	|	'\n'
	;

opts:	option { $$= $<int>0; }
	|	opts option { $$= $<int>0; }
	;

option:	Default ':' OptionValue { ParseDefaultOption($<int>0, $3); /* $<int>0 is the line number. */ }
	|	OptionValue { ParseOption($<int>0, $1); /* $<int>0 is the line number. */ }
	;

rexpr:	bol
	|	bol '/' choice { usesTrailingContext= true; $$= $1; $1.TrailingContext= $3; }
	|	bol '$'
		{
			usesTrailingContext= true;
			Nfa crlf= new Nfa(new SimpleSymbol('\r'));
			crlf.Count(0, 1);
			crlf.Concat(new Nfa(new SimpleSymbol('\n')));
			$$= $1;
			$1.TrailingContext= crlf;
		}
	;

bol	:	caret choice { $$= $2; if($1) $2.AddBol(); }
	;

caret:	{ $$= false; }
	|	'^' { $$= usesBol= true; }
	;

choice:	concat
	|	choice '|' concat { $$= $1; $1.Or($3); }
	;

concat:	kleene
	|	concat kleene { $$= $1; $1.Concat($2); }
	;

kleene:	simple
	|	simple '*' { $$= $1; $1.Kleene(); }
	|	simple '+' { $$= $1; $1.Plus(); }
	|	simple '?' { $$= $1; $1.Count(0, 1); }
	|	simple Single { $$= $1; $1.Count($2.Key, $2.Value); }
	|	simple Double { $$= $1; $1.Count($2.Key, $2.Value); }
	;

simple:	Symbol
		{
			// TODO: does Symbol include '^'?
			if(ignoringCase)
			{
				char lower= char.ToLower($1);
				char upper= char.ToUpper($1);
				if(lower != upper)
				{
					var nfa= new Nfa(new SimpleSymbol(lower));
					nfa.Or(new Nfa(new SimpleSymbol(upper)));
					$$= nfa;
				}
				else
					$$= new Nfa(new SimpleSymbol($1));
			}
			else
				$$= new Nfa(new SimpleSymbol($1));
		}
	|	'.' { $$= new Nfa(dotIncludesNewline ? AnySymbol.Value : AnySymbol.WithoutNewLine); }
	|	NamedExpression
		{
			Nfa value_;
			if(!namedExpressions.TryGetValue($1, out value_))
			{
				Console.Error.WriteLine("error: line {0}: cannot find named RE {1}", scanner.LineNumber, $1);
				return false;
			}
			value_= new Nfa(value_);
			$$= value_;
		}
	|	NegativeClass class ']' { $$= new Nfa(-$2); }
	|	'[' class ']' { $$= new Nfa($2); }
	|	'(' choice ')' { $$= $2; }
	;

class:	range { $$= new RangeSymbol($1.Key, $1.Value); }
	|	class range { $$= $1 + new RangeSymbol($2.Key, $2.Value); }
	;

range:	Symbol '-' Symbol { $$= new CharPair($1, $3); }
	|	Symbol { $$= new CharPair($1, $1); }
	;

idents:	Identifier { $$= new List<string>(); $1.Add($1); }
	|	idents Identifier { $$= $1; $1.Add($2); }
	;

sec2:	grules { ConstructCompositeNfa($1); }
	;

grules:	grule
	|	grules grule { $$= $1; $1.AddRange($2); }
	;

grule:	rule
	|	groups '>' rule { $$= $3; ActivateDefaultRuleGroups(); }
	|	groups Gtocb rules '}' { $$= $3; ActivateDefaultRuleGroups(); }
	|	'\n' { $$= new NfaList(); }
	;

groups:	'<' csv { ActivateRuleGroups($2); }
	|	'<' '*' { ActivateAllRuleGroups(); }
	;

csv	:	Identifier { $$= new List<string> { $1 }; }
	|	csv ',' Identifier { $$= $1; $1.Add($3); }
	;

rules:	rule
	|	rules rule { $$= $1; $1.AddRange($2); $2.ForEach(n => n.SetRuleGroupNames(activeRuleGroupNames)); }
	;

rule:	rexprs Action { $$= $1; SetRuleActions($1, $2); }
	|	Action { $$= CheckIgnoredAction($1); }
	;

rexprs:	rexpr { $$= new NfaList { $1 }; }
	|	rexprs NextExpression rexpr { $$= $1; $1.Add($3); }
	;

opt3:	/* nothing */
	|	PP { finalCodeBlock= $1; }
	;
