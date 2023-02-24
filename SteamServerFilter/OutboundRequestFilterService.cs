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

        public OutboundRequestFilterService(BlockedEndpointsRepository blocked_endpoints_repo)
        {
            blocked_endpoints_repo_ = blocked_endpoints_repo;

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

                        LogService.Debug($"Detected an unknown endpoint: {endpoint.Address.MapToIPv4()}:{endpoint.Port}");

                        await windivert_instance_.SendAsync(packet_, address_);
                    }
                }
            );
        }

        
    }
}
