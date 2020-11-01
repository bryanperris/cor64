// https://github.com/PeterLemon/N64

endian msb
arch n64.rdp

include "LIB/N64.INC" // Include N64 Definitions
include "LIB/N64_GFX.INC" // Include Graphics Macros

align(8) // Align 64-Bit

Set_Scissor 0<<2,0<<2, 0,0, 320<<2,240<<2 // Set Scissor: XH 0.0,YH 0.0, Scissor Field Enable Off,Field Off, XL 320.0,YL 240.0
Set_Other_Modes CYCLE_TYPE_FILL // Set Other Modes
Set_Color_Image IMAGE_DATA_FORMAT_RGBA,SIZE_OF_PIXEL_32B,320-1, $00100000 // Set Color Image: FORMAT RGBA,SIZE 32B,WIDTH 320, DRAM ADDRESS $00100000
Set_Fill_Color $000000FF // Set Fill Color: PACKED COLOR 32B R8G8B8A8 Pixel
Fill_Rectangle 319<<2,239<<2, 0<<2,0<<2 // Fill Rectangle: XL 319.0,YL 239.0, XH 0.0,YH 0.0

Set_Fill_Color $00FFFFFF // Set Fill Color: PACKED COLOR 32B R8G8B8A8 Pixel
Fill_Rectangle 312<<2,224<<2, 192<<2,160<<2 // Fill Rectangle: XL 312.0,YL 224.0, XH 192.0,YH 160.0

Sync_Full // Ensure Entire Scene Is Fully Drawn