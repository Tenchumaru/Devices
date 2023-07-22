using System.Collections.Generic;
using System.Linq;
class Scanner{ // using directives, namespace, and class declaration $
private class Reader_{
internal int Position=>index;
private IEnumerator<char>enumerator;
private System.Text.StringBuilder buffer=new System.Text.StringBuilder();
private int index=0;
internal string Consume(int position){
index=0;position=System.Math.Min(position,buffer.Length);if(position==0)return"";
var s=buffer.ToString(0,position);buffer.Remove(0,position);return s;}
internal int Read(){
if(index<buffer.Length)return buffer[index++];
if(enumerator.MoveNext()){buffer.Append(enumerator.Current);return buffer[index++];}
return-1;}
internal Reader_(IEnumerable<char>reader){enumerator=reader.GetEnumerator();}
internal Reader_(System.IO.TextReader reader){enumerator=Enumerable.
Repeat<System.Func<int>>(reader.Read,int.MaxValue).Select(f=>f()).TakeWhile(v=>v>=0).Cast<char>().GetEnumerator();}
}private Reader_ reader_;
internal Token Read() {{switch(index){case 0:{ // state machine and rule actions $
}break;}}}
// additional code (section three) $
#if WANTS_MAIN
static void Main(string[] args){
var scanner = new Scanner(Console.In);
scanner.Read();}
internal class Token {}
#endif
}
