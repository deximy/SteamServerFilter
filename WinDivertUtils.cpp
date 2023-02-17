#include "WinDivertUtils.h"

#include <iostream>
#include <format>


#include "WinDivert/windivert.h"


HANDLE InitializeWinDivert(const char* filter)
{
    HANDLE windivert_handle = WinDivertOpen(
        filter,
        WINDIVERT_LAYER_NETWORK,
        0,
        WINDIVERT_FLAG_RECV_ONLY
    );
    if (windivert_handle == INVALID_HANDLE_VALUE)
    {
        std::cerr << std::format("[FATAL ERROR] Failed to open the WinDivert device. Error code: {}", GetLastError()) << std::endl;
        return INVALID_HANDLE_VALUE;
    }

    std::cout << std::format("[Log] Open the WinDivert device successfully.") << std::endl;
    return windivert_handle;
}


bool UninitializeWinDivert(HANDLE windivert_handle)
{
    if (!WinDivertClose(windivert_handle))
    {
        std::cerr << std::format("[ERROR] Failed to close the WinDivert device. Error code: {}", GetLastError()) << std::endl;
        return false;
    }

    SC_HANDLE service_control_manager = OpenSCManager(nullptr, nullptr, SC_MANAGER_ALL_ACCESS);
    if (service_control_manager == INVALID_HANDLE_VALUE)
    {
        std::cerr << std::format("[ERROR] Failed to open service control manager. Error code: {}", GetLastError()) << std::endl;
        return false;
    }

    SC_HANDLE windivert_service = OpenService(service_control_manager, L"WinDivert", SERVICE_STOP | SERVICE_QUERY_STATUS);
    if (windivert_service == INVALID_HANDLE_VALUE)
    {
        std::cerr << std::format("[ERROR] Failed to open WinDivert service. Error code: {}", GetLastError()) << std::endl;
        return false;
    }

    SERVICE_STATUS service_status;
    if (!ControlService(windivert_service, SERVICE_CONTROL_STOP, &service_status))
    {
        CloseServiceHandle(windivert_service);
        CloseServiceHandle(service_control_manager);
        std::cerr << std::format("[ERROR] Failed to stop WinDivert service. Error code: {}", GetLastError()) << std::endl;
        return false;
    }

    while (service_status.dwCurrentState != SERVICE_STOPPED)
    {
        if (!QueryServiceStatus(windivert_service, &service_status))
        {
            CloseServiceHandle(windivert_service);
            CloseServiceHandle(service_control_manager);
            std::cerr << std::format("[ERROR] Failed to stop WinDivert service. Error code: {}", GetLastError()) << std::endl;
            return false;
        }
        Sleep(service_status.dwWaitHint);
    }

    CloseServiceHandle(windivert_service);
    CloseServiceHandle(service_control_manager);

    std::cout << std::format("[Log] Close the WinDivert device successfully.") << std::endl;
    return true;
}
