#pragma once
#include <Windows.h>

HANDLE InitializeWinDivert(const char* filter);

bool UninitializeWinDivert(HANDLE windivert_handle);
