using System;
namespace cor64.Mips
{
    public interface ISymbolProvider
    {
        String GetSymbol(ulong address);

        String GetLabel(ulong address);
    }
}
