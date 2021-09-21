using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    public abstract class NamedObject
    {
        public string Name { get; private set; }

        public NamedObject(string name)
        {
            Name = name;
        }

        public override bool Equals(object obj)
        {
            return obj is NamedObject that && GetType() == that.GetType() && Name == that.Name;
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
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(NamedObject left, NamedObject right)
        {
            return !(left == right);
        }
    }
}
