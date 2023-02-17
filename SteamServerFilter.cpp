#include <iostream>
#include <format>
#include <Windows.h>
#include <vector>

#include "WinDivert/windivert.h"
#include "WinDivertUtils.h"


int main()
{
    // Here is the structure of the A2S_Info response packet.
    // The first five bytes are fixed to be 0xFF, 0xFF, 0xFF, 0xFF, 0x49.
    // The sixth byte is the version number, currently set to 0x11, which can also be considered fixed for now.
    // 
    // That's where the filters come from.
    HANDLE windivert_handle = InitializeWinDivert("inbound and udp and udp.Payload32[0] == 0xFFFFFFFF and udp.Payload[4] == 0x49 and udp.Payload[5] == 0x11");

    HandleWinDivertRecv(
        windivert_handle,
        [](PWINDIVERT_IPHDR ip_header, PWINDIVERT_UDPHDR udp_header, PVOID payload, UINT payload_len)
        {
            for (size_t i = 0; i < payload_len; i++)
            {
                std::cout << "0x" << std::hex << std::uppercase << static_cast<int>(*(static_cast<unsigned char*>(payload) + i)) << " ";
            }
            std::cout << std::endl;
        }
    );

    UninitializeWinDivert(windivert_handle);
    return 0;
}
