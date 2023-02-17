#include "WinDivertUtils.h"

#include <iostream>
#include <format>


HANDLE InitializeWinDivert(const char* filter)
{
    HANDLE windivert_handle = WinDivertOpen(
        filter,
        WINDIVERT_LAYER_NETWORK,
        0,
        0
    );
    if (windivert_handle == INVALID_HANDLE_VALUE)
    {
        std::cerr << std::format("[FATAL ERROR] Failed to open the WinDivert device. Error code: {}", GetLastError()) << std::endl;
        return INVALID_HANDLE_VALUE;
    }

    std::cout << std::format("[Log] Open the WinDivert device successfully.") << std::endl;
    return windivert_handle;
}


void HandleWinDivertRecv(HANDLE windivert_handle, std::function<bool(PWINDIVERT_IPHDR, PWINDIVERT_UDPHDR, PVOID, UINT)> func_handle_packet)
{
    auto packet = new unsigned char[WINDIVERT_MTU_MAX];
    UINT packet_len;
    WINDIVERT_ADDRESS addr;

    while (true)
    {
        // Read a matching packet.
        if (!WinDivertRecv(windivert_handle, packet, WINDIVERT_MTU_MAX, &packet_len, &addr))
        {
            std::cerr << std::format("[ERROR] Failed to read packet. Error code: {}", GetLastError()) << std::endl;
            continue;
        }

        PWINDIVERT_IPHDR ip_header;
        PWINDIVERT_UDPHDR udp_header;
        PVOID payload;
        UINT payload_len;
        WinDivertHelperParsePacket(packet, packet_len, &ip_header, NULL, NULL, NULL, NULL, NULL, &udp_header, &payload, &payload_len, NULL, NULL);
        if (func_handle_packet(ip_header, udp_header, payload, payload_len))
        {
            continue;
        }

        if (!WinDivertSend(windivert_handle, packet, packet_len, NULL, &addr))
        {
            std::cerr << std::format("[ERROR] Failed to reinject packet. Error code: {}", GetLastError()) << std::endl;
        }
    }
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
