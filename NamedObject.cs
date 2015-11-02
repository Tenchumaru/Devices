using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    abstract class NamedObject
    {
        public string Name { get; private set; }

        public NamedObject(string name)
        {
            Name = name;
        }

        public override bool Equals(object obj)
        {
            var that = obj as NamedObject;
            return !Object.ReferenceEquals(that, null) && GetType() == that.GetType() && Name == that.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public static bool operator ==(NamedObject left, NamedObject right)
        {
            return Object.ReferenceEquals(left, null) ? Object.ReferenceEquals(right, null) : left.Equals(right);
        }

        public static bool operator !=(NamedObject left, NamedObject right)
        {
            return !(left == right);
        }
    }
}
