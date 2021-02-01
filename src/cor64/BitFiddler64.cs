using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64
{
    public class BitFiddler64
    {
        private readonly List<_Field> m_Fields = new List<_Field>();

        private struct _Field
        {
            public ulong mask;
            public int shift;
        }

        public void DefineField(int offset, int size)
        {
            _Field field;
            field.shift = offset;
            field.mask = (~(0xFFFFFFFFFFFFFFFFUL >> size << size)) << field.shift;
            m_Fields.Add(field);
        }

        public void Inject(int fieldId, ref ulong source, ulong value)
        {
            var field = m_Fields[fieldId];
            value <<= field.shift;
            value &= field.mask;
            source = value | (source & ~field.mask);
        }

        public void J(int fieldId, ref ulong source, ulong value)
        {
            Inject(fieldId, ref source, value);
        }

        public void J(int fieldId, ref ulong source, bool value) {
            if (value)
                Inject(fieldId, ref source, 1UL);
            else
                Inject(fieldId, ref source, 0UL);
        }

        public ulong Extract(int fieldId, ref ulong source)
        {
            var field = m_Fields[fieldId];
            return (field.mask & source) >> field.shift;
        }

        public ulong X(int fieldId, ref ulong source)
        {
            return Extract(fieldId, ref source);
        }

        public bool XB(int fieldId, ref ulong source) {
            var x = X(fieldId, ref source);
            return x != 0;
        }
    }
}
