using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64
{
    public class BitFiddler
    {
        private List<_Field> m_Fields = new List<_Field>();

        private struct _Field
        {
            public uint mask;
            public int shift;
        }

        public void DefineField(int offset, int size)
        {
            _Field field;
            field.shift = offset - (size - 1);
            field.mask = ~((~(0xFFFFFFFFU >> size << size)) << field.shift);
            m_Fields.Add(field);
        }

        public void Inject(int fieldId, ref uint source, uint value)
        {
            var field = m_Fields[fieldId];
            value <<= field.shift;
            value &= ~field.mask;
            source = value | (source & field.mask);
        }

        public void J(int fieldId, ref uint source, uint value)
        {
            Inject(fieldId, ref source, value);
        }

        public uint Extract(int fieldId, ref uint source)
        {
            var field = m_Fields[fieldId];
            return (~field.mask & source) >> field.shift;
        }

        public uint X(int fieldId, ref uint source)
        {
            return Extract(fieldId, ref source);
        }
    }
}
