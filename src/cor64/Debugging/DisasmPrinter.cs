using System;
using System.IO;
using cor64.Mips;

namespace cor64.Debugging {
    public class DisasmPrinter {
        private readonly N64System m_Current;

        public DisasmPrinter(N64System system) {
            m_Current = system;
        }

        public void Print_RspCode(Stream printStream, uint address, int size) {
            // CLEANUP: Fix this
            // StreamWriter writer = new(printStream);

            // foreach (var inst in RspDisassembler.Disassemble(address, size)) {
            //     if (inst.CanFullyDisassemble) {
            //         writer.WriteLine("{0:X8} {1:X8} {2}", inst.Address, inst.Inst.inst, RspDisassembler.GetFullDisassembly(inst));
            //     }
            //     else {
            //         writer.WriteLine("{0:X8} {1:X8} ???", inst.Address, inst.Inst.inst);
            //     }
            // }

            // writer.Flush();
        }

        public void Print_CpuCode(Stream printStream, uint address, int size) {
            // CLEANUP: Fix this
            // StreamWriter writer = new(printStream);

            // foreach (var inst in CpuDisassembler.Disassemble(address, size)) {
            //     writer.WriteLine("{0:X8} {1}", inst.Address, CpuDisassembler.GetFullDisassembly(inst));
            // }

            // writer.Flush();
        }

        protected BaseDisassembler RspDisassembler => m_Current.DeviceRcp.DeviceRsp.Disassembler;

        protected BaseDisassembler CpuDisassembler => m_Current.DeviceCPU.Disassembler;
    }
}