using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    class Parameter
    {
        public enum Type
        {
            Define,
            Variable
        }

        public Parameter(Type type, String name, String value)
        {
            PType = type;
            Name = name;
            Value = value;
        }

        public String Name { get; set; }

        public String Value { get; set; }

        public Parameter.Type PType { get; set; }
    }
}
