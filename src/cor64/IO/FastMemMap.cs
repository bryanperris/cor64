using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    public class FastMemMap
    {
        private UnifiedMemModel<IntPtr[]> m_ReadMap = new UnifiedMemModel<IntPtr[]>();
        private UnifiedMemModel<IntPtr[]> m_WriteMap = new UnifiedMemModel<IntPtr[]>();
        private UnifiedMemModel<BlockDevice> m_MainMap;

        public void Init(UnifiedMemModel<BlockDevice> memModel)
        {
            m_MainMap = memModel;

            m_ReadMap.RDRAM = memModel.RDRAM.GetReadPointerMap();
            m_ReadMap.RDRAMRegs = memModel.RDRAMRegs.GetReadPointerMap();
            m_ReadMap.SPRegs = memModel.SPRegs.GetReadPointerMap();
            m_ReadMap.DPCmdRegs = memModel.DPCmdRegs.GetReadPointerMap();
            m_ReadMap.DpSpanRegs = memModel.DpSpanRegs.GetReadPointerMap();
            m_ReadMap.MIRegs = memModel.MIRegs.GetReadPointerMap();
            m_ReadMap.VIRegs = memModel.VIRegs.GetReadPointerMap();
            m_ReadMap.AIRegs = memModel.AIRegs.GetReadPointerMap();
            m_ReadMap.PIRegs = memModel.PIRegs.GetReadPointerMap();
            m_ReadMap.RIRegs = memModel.RIRegs.GetReadPointerMap();
            m_ReadMap.SIRegs = memModel.SIRegs.GetReadPointerMap();
            m_ReadMap.Cart = memModel.Cart.GetReadPointerMap();
            m_ReadMap.PIF = memModel.PIF.GetReadPointerMap();
            m_ReadMap.Init();

            m_WriteMap.RDRAM = memModel.RDRAM.GetWritePointerMap();
            m_WriteMap.RDRAMRegs = memModel.RDRAMRegs.GetWritePointerMap();
            m_WriteMap.SPRegs = memModel.SPRegs.GetWritePointerMap();
            m_WriteMap.DPCmdRegs = memModel.DPCmdRegs.GetWritePointerMap();
            m_WriteMap.DpSpanRegs = memModel.DpSpanRegs.GetWritePointerMap();
            m_WriteMap.MIRegs = memModel.MIRegs.GetWritePointerMap();
            m_WriteMap.VIRegs = memModel.VIRegs.GetWritePointerMap();
            m_WriteMap.AIRegs = memModel.AIRegs.GetWritePointerMap();
            m_WriteMap.PIRegs = memModel.PIRegs.GetWritePointerMap();
            m_WriteMap.RIRegs = memModel.RIRegs.GetWritePointerMap();
            m_WriteMap.SIRegs = memModel.SIRegs.GetWritePointerMap();
            m_WriteMap.Cart = memModel.Cart.GetWritePointerMap();
            m_WriteMap.PIF = memModel.PIF.GetWritePointerMap();
            m_WriteMap.Init();
        }

        public void Read(uint address, byte[] buffer, int offset, int count)
        {
            var blkPtr = m_ReadMap.GetDevice(address);
            var blkOffset = m_ReadMap.GetDeviceOffset(address);
            var ptr = blkPtr[blkOffset / 4];
            var off = (int)(address % 4);
            Marshal.Copy(ptr.Offset(off), buffer, offset, count);
        }

        public void Write(uint address, byte[] buffer, int offset, int count)
        {
            var blkPtr = m_WriteMap.GetDevice(address);
            var blkOffset = m_WriteMap.GetDeviceOffset(address);
            var ptr = blkPtr[blkOffset / 4];
            var off = (int)(address % 4);
            Marshal.Copy(buffer, offset, ptr.Offset(off), count);

            var blkDevice = m_MainMap.GetDevice(address);
            blkOffset = m_MainMap.GetDeviceOffset(address);
            blkDevice.WriteNotify(blkOffset / 4);
        }
    }
}
