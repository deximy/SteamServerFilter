using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using WindivertDotnet;

namespace SteamServerFilter
{
    public class OutboundRequestFilterService
    {
        private readonly WinDivert windivert_instance_;
        private readonly WinDivertPacket packet_;
        private readonly WinDivertAddress address_;

        private readonly BlockedEndpointsRepository blocked_endpoints_repo_;
        private readonly SniffingServerNameService sniffing_server_name_service_;

        public OutboundRequestFilterService(BlockedEndpointsRepository blocked_endpoints_repo, SniffingServerNameService sniffing_server_name_service)
        {
            blocked_endpoints_repo_ = blocked_endpoints_repo;
            sniffing_server_name_service_ = sniffing_server_name_service;

            windivert_instance_ = new WinDivert(
                Filter.True
                    .And(f => f.Network.Outbound)
                    .And(f => f.IsUdp)
                    .And(
                        // qconnect protocol
                        f => f.Udp.Payload32[0] == 0xFFFFFFFF
                            && f.Udp.Payload32[1] == 0x71636F6E
                            && f.Udp.Payload32[2] == 0x6E656374
                            && f.Udp.Payload16[6] == 0x3078
                     ),
                WinDivertLayer.Network
            );
            packet_ = new WinDivertPacket();
            address_ = new WinDivertAddress();

            Task.Run(
                async () => {
                    while (true)
                    {
                        await windivert_instance_.RecvAsync(packet_, address_);

                        var parsed_packet = packet_.GetParseResult();

                        IPEndPoint? endpoint = null;
                        unsafe
                        {
                            endpoint = new IPEndPoint(parsed_packet.IPV4Header->DstAddr, parsed_packet.UdpHeader->DstPort);
                            if (endpoint == null)
                            {
                                LogService.Error("Error occurred when reading server endpoint. Please contact developer for further help.");
                                LogService.Error($"IPV4Header->DstAddr: {parsed_packet.IPV4Header->DstAddr.MapToIPv4()}, UdpHeader->DstPort: {parsed_packet.UdpHeader->DstPort}");
                                LogService.Debug($"parsed_packet.IPV4Header: {(int)parsed_packet.IPV4Header}");
                                continue;
                            }
                        }
                        if (blocked_endpoints_repo_.Contains(endpoint))
                        {
                            LogService.Debug($"Block outcoming traffic to an endpoint in black list: {endpoint.Address.MapToIPv4()}:{endpoint.Port}");
                            continue;
                        }


                        // Here is the most tricky problem:
                        // How can we determine whether an endpoint isn't in the blacklist because it doesn't match the rules or it does but hasn't been logged yet?
                        // Here we send a A2S_INFO query to the target server.
                        // Usually the endpoint will response to our request.
                        // If `InboundServerNameFilterService` receives response matching rules and targeting our socket, DROP ALL PACKETS!
                        // As we didn't block the connection request here, the game client should be connecting (or has connected) to the server.
                        // So drop all packets to block the connection.
                        // This won't help if the server block A2S_INFO query.
                        // We must find some other way to block them.
                        await sniffing_server_name_service.QueryServerName(endpoint);
                        LogService.Debug($"Detected an unknown endpoint: {endpoint.Address.MapToIPv4()}:{endpoint.Port}");

                        await windivert_instance_.SendAsync(packet_, address_);
                    }
                }
            );
        }

        
    }
}
