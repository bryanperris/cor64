using System.Runtime.InteropServices;
using System;
using cor64.IO;
using NLog;

namespace cor64.PIF
{
    public class PIFController : N64MemoryDevice
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly MemMappedBuffer m_Rom = new MemMappedBuffer(0x7C0, MemMappedBuffer.MemModel.SINGLE_READONLY);
        private readonly MemMappedBuffer m_Ram = new MemMappedBuffer(0x40, MemMappedBuffer.MemModel.SINGLE_READ_WRITE);

        public const uint ADDRESS_JOY_CHANNEL = 0x1FC007C4U;

        public const int PIF_RAM_OFFSET = 0x7C0;
        public const int COMMAND_BYTE_OFFSET = 0x3F;

        private byte m_CommandByte;

        [Flags]
        private enum PifCommands : byte {
            None = 0,
            JoyBus = 0x1, // Joybus communication
            CRX105 = 0x2, // Request for challenge-response
            AllowNormalOperation = 0x8, // Request for continue operation, otherwise console locksup (must be sent within 5 seconds)
            DisablePifRomAccess = 0x10, // Request to disable read access to the PIF ROM
            CICChecksumVerify = 0x20, // CIC checksum verified result
            Clear = 0x40 // Clear PIF RAM
        }

        private enum JoyBusCommand : byte {
            Info = 0,
            ControllerState = 1,
            ReadEEPROM = 4,
            WriteEEPROM = 5,
            Reset = 0xFF
        }

        private const int END = -2; // 0xFE
        private const int NOP = -1; // 0xFF

        private readonly ushort[] m_LastJoyconStatus = new ushort[4];

        public PIFController(N64MemoryController n64MemoryController) : base(n64MemoryController, 0x100000)
        {
            StaticMap(m_Rom, m_Ram);
        }

        public void JoyWriteButtons(int slot, JoyController.ButtonPress buttonPress) {
            Log.Debug("Joy Button Press: {0}", buttonPress.ToString());
            // m_LastJoyconStatus[slot] = ((ushort)buttonPress).ByteSwapped();
             m_LastJoyconStatus[slot] = (ushort)buttonPress;
        }

        public byte[] ReadRam() {
            byte[] buffer = new byte[64];
            for (int i = 0; i < 64; i++) {
                // always read in BE form
                buffer[i] = m_Ram.ReadPtr.Offset(N64Endianess.Address8(i)).AsType_8();
            }
            return buffer;
        }

        private static bool HasCommand(PifCommands commands, PifCommands testFlags) {
            return (commands & testFlags) == testFlags;
        }

        public void ReadCommandByte() {
            m_CommandByte = ReadRamU8(COMMAND_BYTE_OFFSET);
            // Console.WriteLine("Command byte read: {0:X2}", m_CommandByte);
        }

        public void ProcessPifCommands() {
            PifCommands cmd = (PifCommands)m_CommandByte;

            if (cmd == PifCommands.None) return;

            // Log.Debug("Requested PIF Command: {0}", cmd.ToString());

            if (HasCommand(cmd, PifCommands.JoyBus)) {
                // Console.WriteLine("Joybus command pending");
                ParseJoybus();
            }

            m_CommandByte = 0;
        }

        private void Copy(int ramOffset, byte[] buffer, int index, int len) {
            for (int i = 0; i < len; i++) {
                buffer[index + i] = m_Ram.ReadPtr.Offset(N64Endianess.Address8(ramOffset + i)).AsType_8();
            }
        }

        public void ParseJoybus() {
            int channel = 0;

            for (int i = 0; i < 64; i++) {
                if (channel > 4) return;

                int tx = ReadJoyByte(i);

                // Console.WriteLine("Off: {0} | Channel: {1} | TX: {2}", i, channel, tx);

                switch (tx) {
                    case END: return;
                    case NOP: continue;
                    case 0x0: { channel++; break; }
                    default: {
                        int rx = ReadJoyByte(i + 1);

                        if (rx == END) return;

                        byte[] data = new byte[tx];
                        Copy(i + 2, data, 0, tx);

                        // Console.WriteLine("Data Size: {0}", data.Length);
                        // Console.Write("Command Data: ");
                        // for (int d = 0; d < data.Length; d++) { 
                        //     Console.Write(data[d].ToString("X2"));
                        // }
                        // Console.WriteLine();

                        ExecuteJoybusCommands(data, channel, i + 2 + tx);

                        i += tx;
                        i += rx;
                        i++;

                        // Log.Debug("offset pushed to {0} (RX:{1})", i, rx);

                        channel++;
                        break;
                    }
                }
            }
        }

        private void ExecuteJoybusCommands(byte[] data, int channel, int responseOffset) {
            JoyBusCommand command = (JoyBusCommand)data[0];

            // Log.Debug("Joy Command Execute: CH: {0} {1} TX={2}", channel, command.ToString(), data.Length);

            switch (command) {
                default: break;

                case JoyBusCommand.Reset:
                case JoyBusCommand.Info: {

                    switch (channel) {
                        default: break;

                        case 0:
                        case 1:
                        case 2:
                        case 3: {
                            // Log.Debug("PIF Read controller Info");

                            // just report 1 active controller in slot 0 for now
                            if (channel == 0) {
                                WriteRamU8(responseOffset++, 0x50);
                                WriteRamU8(responseOffset++, 0x00);
                                WriteRamU8(responseOffset, 0x00); // no pak inserted
                                break;
                            }
                            else {
                                WriteRamU8(responseOffset++, 0xFF);
                                WriteRamU8(responseOffset++, 0xFF);
                                WriteRamU8(responseOffset, 0xFF);
                                break;
                            }
                        }

                        case 4: {
                            // Log.Debug("PIF EEEPROM Info");
                            WriteRamU16(responseOffset, 0x0080); responseOffset+=2;
                            WriteRamU8(responseOffset, 0x00);
                            break;
                        }
                    }

                    break;
                }

                case JoyBusCommand.ControllerState: {
                    if (channel >= 0 && channel <= 3) {
                        // var buttonPresses = N64Endianess.JoyconRead(m_LastJoyconStatus[channel]);
                        JoyController.ButtonPress buttonPresses = (JoyController.ButtonPress)m_LastJoyconStatus[channel];
                        // Console.WriteLine("PIF: Buttons Pressed: {1} = {0:X4}", buttonPresses, channel);

                        byte x_dir = 0;
                        byte y_dir = 0;

                        if ((buttonPresses & JoyController.ButtonPress.AnalogLeft) == JoyController.ButtonPress.AnalogLeft) {
                            x_dir = unchecked((byte)(sbyte)-50);
                        }

                        if ((buttonPresses & JoyController.ButtonPress.AnalogRight) == JoyController.ButtonPress.AnalogRight) {
                            x_dir = unchecked((byte)50);
                        }

                        if ((buttonPresses & JoyController.ButtonPress.AnalogDown) == JoyController.ButtonPress.AnalogDown) {
                            y_dir = unchecked((byte)(sbyte)-50);
                        }

                        if ((buttonPresses & JoyController.ButtonPress.AnalogUp) == JoyController.ButtonPress.AnalogUp) {
                            y_dir = unchecked((byte)50);
                        }

                        WriteRamU16(responseOffset, (ushort)buttonPresses); // Buttons
                        responseOffset+=2;
                        WriteRamU8(responseOffset++, x_dir); // X Axis
                        WriteRamU8(responseOffset++, y_dir); // Y Axis
                        m_LastJoyconStatus[channel] = 0;
                    }
                    else {
                        WriteRamU8(responseOffset++, 0);
                        WriteRamU8(responseOffset++, 0);
                        WriteRamU8(responseOffset++, 0);
                    }
                    WriteRamU8(responseOffset, 0); // Error num
                    break;
                }

                case JoyBusCommand.ReadEEPROM: {
                    // For now just pretend we have the largest EEPROM available
                    break;
                }

                case JoyBusCommand.WriteEEPROM: {
                    break;
                }

            }
        }

        private byte ReadRamU8(int offset) {
            return m_Ram.ReadPtr.Offset(N64Endianess.Address8(offset)).AsType_8();
        }

        private void WriteRamU8(int offset, byte value) {
            m_Ram.WritePtr.Offset(N64Endianess.Address8(offset)).AsType_8(value);
        }

        private void WriteRamU16(int offset, ushort value) {
            m_Ram.WritePtr.Offset(N64Endianess.Address16(offset)).AsType_16(value);
        }

        private sbyte ReadJoyByte(int offset) => (sbyte)m_Ram.ReadPtr.Offset(N64Endianess.Address8(offset)).AsType_8();


        // public byte PifCtrl {
        //     get => m_Ram.ReadPtr.Offset(N64Endianess.Address8(SR_OFFSET)).AsType_8();
        //     set => m_Ram.WritePtr.Offset(N64Endianess.Address8(SR_OFFSET)).AsType_8(value);
        // }

        public override string Name => "PIF Module Memory";
    }
}
