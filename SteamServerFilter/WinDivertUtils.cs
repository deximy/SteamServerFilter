using System.Net.Sockets;
using WindivertDotnet;

namespace SteamServerFilter
{
    public class WinDivertUtils
    {
        private static WinDivert? windivert_instance_;
        private static WinDivertPacket? packet_;
        private static WinDivertAddress? address_;

        public static void InitializeWinDivert(Filter filter, Func<WinDivertParseResult, bool> func_handle_packet)
        {
            windivert_instance_ = new WinDivert(filter, WinDivertLayer.Network);
            packet_ = new WinDivertPacket();
            address_= new WinDivertAddress();

            Task.Run(
                async () => {
                    while (true)
                    {
                        await windivert_instance_.RecvAsync(packet_, address_);

                        if (func_handle_packet(packet_.GetParseResult()))
                        {
                            continue;
                        }

                        await windivert_instance_.SendAsync(packet_, address_);
                    }
                }
            );
        }

        public static void UninitializeWinDivert()
        {
            windivert_instance_?.Close();
        }
    }
}
