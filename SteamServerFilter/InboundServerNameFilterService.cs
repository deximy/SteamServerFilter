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

        public InboundServerNameFilterService(BlockRulesRepository block_rules_repo)
        {
            block_rules_repo_ = block_rules_repo;

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
                        var server_name = Encoding.UTF8.GetString(parsed_packet.DataSpan.Slice(6, parsed_packet.DataSpan.IndexOf((byte)0x00) - 6));

                        if (
                            !block_rules_repo_.Get().Any(
                                (i) => {
                                    if (i.IsMatch(server_name))
                                    {
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

        ~InboundServerNameFilterService()
        {
            windivert_instance_.Close();
        }
    }
}
