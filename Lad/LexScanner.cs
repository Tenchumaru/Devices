using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Lad
{
    public partial class LexScanner
    {
        public int LineNumber
        {
            get { return lineNumber; }
        }

        private TextReader reader;
        private StringBuilder buffer = new StringBuilder();
        private int marker, position, lineNumber;
        private string tokenValue;
        private LexScanner yy;
        private int ScanValue;
        private ScannerMode mode = ScannerMode.SectionOne;

        public LexScanner(TextReader reader)
        {
            this.reader = reader;
            yy = this;
        }

        internal Token Read()
        {
            switch(mode)
            {
            case ScannerMode.SectionOne:
                return ReadSectionOne();
            case ScannerMode.SectionTwoExpression:
                return ReadSectionTwoExpression();
            case ScannerMode.SectionTwoClass:
                return ReadSectionTwoClass();
            case ScannerMode.SectionTwoQuotedExpression:
                return ReadSectionTwoQuotedExpression();
            }
            throw new Exception("unexpected scanner mode " + mode);
        }

        private Token ReadRestOfLine(int tokenSymbol)
        {
            // Read the rest of the line as the token value.
            var sb = new StringBuilder();
            for(int ch; ; )
            {
                ch = Get();
                if(ch < 0 || ch == '\r' || ch == '\n')
                    break;
                sb.Append((char)ch);
            }
            return new Token { Symbol = tokenSymbol, Value = sb.ToString() };
        }

        private Token HandleNoMatch()
        {
            return new Token { Symbol = Take() };
        }

        private void Save()
        {
            marker = position;
        }

        private void Restore()
        {
            tokenValue = buffer.ToString(0, marker);
            buffer.Remove(position = 0, marker);
            marker = 0;
        }

        private int Get()
        {
            if(position >= buffer.Length)
            {
                if(ScanValue < 0)
                    return ScanValue;
                int ch = reader.Read();
                if(ch < 0)
                    return ScanValue = -1;
                if(ch == '\n')
                    ++lineNumber;
                buffer.Append((char)ch);
            }
            ++position;
            return ScanValue = buffer[position - 1];
        }

        private int Take()
        {
            int ch = Get();
            Save();
            Restore();
            return ch;
        }

        private void ReportError(string message)
        {
            Console.Error.WriteLine(message);
        }

        private void ReportError(string format, params object[] args)
        {
            Console.Error.WriteLine(format, args);
        }

        private enum ScannerMode { SectionOne, SectionTwoExpression, SectionTwoClass, SectionTwoQuotedExpression }
    }
}
