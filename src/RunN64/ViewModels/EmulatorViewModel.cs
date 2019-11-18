using System;
using System.Collections.Generic;
using System.Text;

namespace RunN64.ViewModels
{
    public class EmulatorViewModel : ViewModelBase
    {
        private Emulator m_Emulator;

        public EmulatorViewModel(Emulator emulator) {
            m_Emulator = emulator;
        }

        
    }
}