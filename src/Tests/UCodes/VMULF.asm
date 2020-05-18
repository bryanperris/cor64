// https://github.com/PeterLemon/N64

endian msb
arch n64.rsp

include "LIB/N64.INC" // Include N64 Definitions
include "LIB/N64_GFX.INC" // Include Graphics Macros
include "LIB/N64_RSP.INC" // Include RSP Macros

align(8) // Align 64-Bit
base $0000 // Set Base Of RSP Code Object To Zero

lqv v0[e0],$00(r0) // V0 = 128-Bit DMEM $000(R0), Load Quad To Vector: LQV VT[ELEMENT],$OFFSET(BASE)
lqv v1[e0],$10(r0) // V1 = 128-Bit DMEM $010(R0), Load Quad To Vector: LQV VT[ELEMENT],$OFFSET(BASE)

vmulf v0,v1[e0] // V0 = V0 * V1[0], Vector Multiply Signed Fractions: VMULF VD,VS,VT[ELEMENT]
sqv v0[e0],$00(r0) // 128-Bit DMEM $000(R0) = V0, Store Vector To Quad: SQV VT[ELEMENT],$OFFSET(BASE)

vsar v0,v0[e8] // V0 = Vector Accumulator HI, Vector Accumulator Read: VSAR VD,VS,VT[ELEMENT]
sqv v0[e0],$10(r0) // 128-Bit DMEM $010(R0) = V0, Store Vector To Quad: SQV VT[ELEMENT],$OFFSET(BASE)
vsar v0,v0[e9] // V0 = Vector Accumulator MD, Vector Accumulator Read: VSAR VD,VS,VT[ELEMENT]
sqv v0[e0],$20(r0) // 128-Bit DMEM $020(R0) = V0, Store Vector To Quad: SQV VT[ELEMENT],$OFFSET(BASE)
vsar v0,v0[e10] // V0 = Vector Accumulator LO, Vector Accumulator Read: VSAR VD,VS,VT[ELEMENT]
sqv v0[e0],$30(r0) // 128-Bit DMEM $030(R0) = V0, Store Vector To Quad: SQV VT[ELEMENT],$OFFSET(BASE)

cfc2 t0,vco   // T0 = RSP CP2 Control Register: VCO (Vector Carry Out)
sh t0,$40(r0) // 16-Bit DMEM $040(R0) = T0
cfc2 t0,vcc   // T0 = RSP CP2 Control Register: VCC (Vector Compare Code)
sh t0,$42(r0) // 16-Bit DMEM $042(R0) = T0
cfc2 t0,vce   // T0 = RSP CP2 Control Register: VCE (Vector Compare Extension)
sb t0,$44(r0) //  8-Bit DMEM $044(R0) = T0

break // Set SP Status Halt, Broke & Check For Interrupt