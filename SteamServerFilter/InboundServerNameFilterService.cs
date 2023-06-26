using System.Diagnostics;
using System.Net;
using System.Text;
using WindivertDotnet;

namespace SteamServerFilter
{
    public class InboundServerNameFilterService
    {
        private readonly WinDivert windivert_instance_;
        private readonly WinDivertPacket packet_;
        private readonly WinDivertAddress address_;

        private readonly BlockRulesRepository block_rules_repo_;
        private readonly BlockedEndpointsRepository blocked_endpoints_repo_;
        private readonly SniffingServerNameService sniffing_server_name_service_;

        public InboundServerNameFilterService(
            BlockRulesRepository block_rules_repo,
            BlockedEndpointsRepository blocked_endpoints_repo,
            SniffingServerNameService sniffing_server_name_service
        )
        {
            block_rules_repo_ = block_rules_repo;
            blocked_endpoints_repo_ = blocked_endpoints_repo;
            sniffing_server_name_service_ = sniffing_server_name_service;

            // Here is the structure of the A2S_INFO response packet.
            // The first five bytes are fixed to be 0xFF, 0xFF, 0xFF, 0xFF, 0x49.
            // 
            // That's where the filters come from.
            windivert_instance_ = new WinDivert(
                Filter.True
                    .And(f => f.Network.Inbound)
                    .And(f => f.IsUdp)
                    .And(f => f.Udp.Payload32[0] == 0xFFFFFFFF)
                    .And(f => f.Udp.Payload[4] == 0x49),
                WinDivertLayer.Network
            );
            packet_ = new WinDivertPacket();
            address_ = new WinDivertAddress();

            Task.Run(
                async () => {
                    while (true)
                    {
                        await windivert_instance_.RecvAsync(packet_, address_);
                        Program.processmode?.check_process();

                        var parsed_packet = packet_.GetParseResult();

                        IPEndPoint? server_endpoint = null;
                        int? local_port = null;
                        unsafe
                        {
                            server_endpoint = new IPEndPoint(parsed_packet.IPV4Header->SrcAddr, parsed_packet.UdpHeader->SrcPort);
                            if (server_endpoint == null)
                            {
                                LogService.Error("Error occurred when reading server endpoint. Please contact developer for further help.");
                                LogService.Error($"IPV4Header->SrcAddr: {parsed_packet.IPV4Header->SrcAddr.MapToIPv4()}, UdpHeader->SrcPort: {parsed_packet.UdpHeader->SrcPort}");
                                LogService.Debug($"parsed_packet.IPV4Header: {(int)parsed_packet.IPV4Header}");
                                continue;
                            }

                            local_port = parsed_packet.UdpHeader->DstPort;
                        }
                        if (blocked_endpoints_repo_.Contains(server_endpoint))
                        {
                            LogService.Debug($"Block incoming traffic from an endpoint in black list: {server_endpoint.Address.MapToIPv4()}:{server_endpoint.Port}");
                            continue;
                        }


                        var server_name = Encoding.UTF8.GetString(parsed_packet.DataSpan.Slice(6, parsed_packet.DataSpan.IndexOf((byte)0x00) - 6));
                        if (
                            !block_rules_repo_.Get().Any(
                        (i) => {
                            if (i.IsMatch(server_name) && Program.processmode.ProcessExists == true)
                            {
                                blocked_endpoints_repo_.Add(server_endpoint);
                                LogService.Info($"Block server with name: {server_name}");
                                return true;
                            }
                            if (Program.processmode.ProcessExists == false)
                            {
                                blocked_endpoints_repo_.Clear();
                            }
                            return false;
                                }
                            )
                        )
                        {
                            await windivert_instance_.SendAsync(packet_, address_);
                        }
                        else
                        {
                            if (sniffing_server_name_service_.GetSocketPort() == local_port)
                            {
                                // We sent a query to the endpoint whose response matches the rules.
                                // Block ALL CONNECTION TO THIS ENDPOINT.
                                // This is an urgent block as we are connecting to the server.
                                // If we don't do like that, we will connect to server completely.
                                var temporary_block_windivert = new WinDivert(
                                    Filter.True
                                        .And(f => f.IsUdp)
                                        .And(f => f.Network.RemoteAddr == server_endpoint.Address.MapToIPv4().ToString())
                                        .And(f => f.Network.RemotePort == server_endpoint.Port),
                                    WinDivertLayer.Network,
                                    100,
                                    0
                                );
                                _ = Task.Run(
                                    async () => {
                                        // This is an arbitrary argument ;)
                                        // As is known to all, the connection will time out after 1 min.
                                        // Just sleep additional 1 min in case of accident.
                                        // Then close the handle to release resources.
                                        await Task.Delay(2 * 60 * 1000);
                                        temporary_block_windivert.Close();
                                    }
                                );
                                LogService.Info($"Block connection to endpoint: {server_endpoint.Address.MapToIPv4()}:{server_endpoint.Port}");
                            }
                        }
                    }
                }
            );
        }
    }
}
