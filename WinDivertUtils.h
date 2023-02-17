#pragma once
#include <Windows.h>
#include <functional>
#include "WinDivert/windivert.h"

HANDLE InitializeWinDivert(const char* filter);

void HandleWinDivertRecv(HANDLE windivert_handle, std::function<void(PWINDIVERT_IPHDR, PWINDIVERT_UDPHDR, PVOID, UINT)> func_handle_packet);

bool UninitializeWinDivert(HANDLE windivert_handle);
