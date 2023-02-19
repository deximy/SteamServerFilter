using WindivertDotnet;
using static SteamServerFilter.WinDivertUtils;

namespace SteamServerFilter
{
    class Program
    {
        unsafe static void Main(string[] args)
        {
            // Here is the structure of the A2S_Info response packet.
            // The first five bytes are fixed to be 0xFF, 0xFF, 0xFF, 0xFF, 0x49.
            // The sixth byte is the version number, currently set to 0x11, which can also be considered fixed for now.
            // 
            // That's where the filters come from.
            InitializeWinDivert(
                Filter.True
                    .And(f => f.Network.Inbound)
                    .And(f => f.IsUdp)
                    .And(f => f.Udp.Payload32[0] == 0xFFFFFFFF)
                    .And(f => f.Udp.Payload[4] == 0x49)
                    .And(f => f.Udp.Payload[5] == 0x11),
                (packet) => {
                    foreach (var i in packet.DataSpan.ToArray())
                    {
                        Console.Write($"0x{i.ToString("X2")} ");
                    }
                    Console.WriteLine();
                    return true;
                }
            );
           

            while (true) { }

            UninitializeWinDivert();
        }
    }
}
