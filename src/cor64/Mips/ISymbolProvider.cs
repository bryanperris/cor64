using System;
namespace cor64.Mips
{
    public interface ISymbolProvider
    {
        String GetSymbol(long address);

        String GetLabel(long address);
    }
}
