using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public delegate void ValueChangedHandler<T>(T data);

    public interface IWatchableRegister<T>
    {
        event ValueChangedHandler<T> ValueChanged;
    }
}
