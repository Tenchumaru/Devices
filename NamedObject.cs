using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    class NamedObject<T> where T : class
    {
        public string Name { get; private set; }

        public NamedObject(string name)
        {
            Name = name;
        }

        public override bool Equals(object obj)
        {
            var that = obj as T;
            return !Object.ReferenceEquals(that, null) && ToString() == that.ToString();
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public static bool operator ==(NamedObject<T> left, NamedObject<T> right)
        {
            return Object.ReferenceEquals(left, null) ? Object.ReferenceEquals(right, null) : left.Equals(right);
        }

        public static bool operator !=(NamedObject<T> left, NamedObject<T> right)
        {
            return !(left == right);
        }
    }
}
