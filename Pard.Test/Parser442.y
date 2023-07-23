// Page 4.21 on page 231

%%

S: C C { Result.Append("<CC>");Console.WriteLine("I got two Cs."); } ;
C: 'c' C { Result.Append("<cC>");Console.WriteLine("I got a c and a C."); } ;
C: 'd' { Result.Append("<d>");Console.WriteLine("I got a d."); } ;
