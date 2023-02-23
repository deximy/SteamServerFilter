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

        public InboundServerNameFilterService(BlockRulesRepository block_rules_repo, BlockedEndpointsRepository blocked_endpoints_repo)
        {
            block_rules_repo_ = block_rules_repo;
            blocked_endpoints_repo_ = blocked_endpoints_repo;

            // Here is the structure of the A2S_Info response packet.
            // The first five bytes are fixed to be 0xFF, 0xFF, 0xFF, 0xFF, 0x49.
            // The sixth byte is the version number, currently set to 0x11, which can also be considered fixed for now.
            // 
            // That's where the filters come from.
            windivert_instance_ = new WinDivert(
                Filter.True
                    .And(f => f.Network.Inbound)
                    .And(f => f.IsUdp)
                    .And(f => f.Udp.Payload32[0] == 0xFFFFFFFF)
                    .And(f => f.Udp.Payload[4] == 0x49)
                    .And(f => f.Udp.Payload[5] == 0x11),
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
                            endpoint = new IPEndPoint(parsed_packet.IPV4Header->SrcAddr, parsed_packet.UdpHeader->SrcPort);
                            if (endpoint == null)
                            {
                                LogService.Error("Error occurred when reading server endpoint. Please contact developer for further help.");
                                LogService.Error($"IPV4Header->SrcAddr: {parsed_packet.IPV4Header->SrcAddr.MapToIPv4()}, UdpHeader->SrcPort: {parsed_packet.UdpHeader->SrcPort}");
                                LogService.Debug($"parsed_packet.IPV4Header: {(int)parsed_packet.IPV4Header}");
                                continue;
                            }
                        }
                        if (blocked_endpoints_repo_.Contains(endpoint))
                        {
                            LogService.Debug($"Block incoming traffic from an endpoint in black list: {endpoint.Address.MapToIPv4()}:{endpoint.Port}");
                            continue;
                        }


                        var server_name = Encoding.UTF8.GetString(parsed_packet.DataSpan.Slice(6, parsed_packet.DataSpan.IndexOf((byte)0x00) - 6));
                        if (
                            !block_rules_repo_.Get().Any(
                                (i) => {
                                    if (i.IsMatch(server_name))
                                    {
                                        blocked_endpoints_repo_.Add(endpoint);
                                        LogService.Info($"Block server with name: {server_name}");
                                        return true;
                                    }
                                    return false;
                                }
                            )
                        )
                        {
                            await windivert_instance_.SendAsync(packet_, address_);
                        }
                    }
                }
            );
        }
    }
}
