﻿using log4net;
using Microsoft.Win32;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WindivertDotnet;
using static SteamServerFilter.WinDivertUtils;

namespace SteamServerFilter
{
    class Program
    {
        static void Main(string[] args)
        {
            var tray_icon = new NotifyIcon() {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Text = "SteamServerFilter",
                Visible = true,
                ContextMenuStrip = new(),
            };
            RegistryKey? registry_key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            var auto_start_item = new ToolStripMenuItem() {
                Text = "开机自动启动",
                Checked = registry_key?.GetValue("SteamServerFilter") == null ? false : true,
            };
            auto_start_item.Click += (sender, e) => {
                auto_start_item.Checked = !auto_start_item.Checked;
            };
            auto_start_item.CheckedChanged += (sender, e) => {
                if (auto_start_item.Checked)
                {
                    registry_key?.SetValue("SteamServerFilter", Application.ExecutablePath);
                }
                else
                {
                    registry_key?.DeleteValue("SteamServerFilter");
                }
            };
            tray_icon.ContextMenuStrip.Items.Add(auto_start_item);
            var exit_item = new ToolStripMenuItem() {
                Text = "退出",
            };
            exit_item.Click += (sender, e) => {
                UninitializeWinDivert();
                Application.Exit();
            };
            tray_icon.ContextMenuStrip.Items.Add(exit_item);
            

            log4net.Config.XmlConfigurator.Configure();
            var log = LogManager.GetLogger("log");

            log.Info("=================================================");
            log.Info("Program starts.");

            var block_rules_file_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "block_rules.txt");
            if (!File.Exists(block_rules_file_path))
            {
                File.Create(block_rules_file_path).Close();
            }

            List<Regex> regex_rules_list = new List<Regex>();
            using (StreamReader stream_reader = new StreamReader(block_rules_file_path))
            {
                while (!stream_reader.EndOfStream)
                {
                    string? rule = stream_reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(rule))
                    {
                        continue;
                    }
                    Regex regex = new Regex(rule);
                    regex_rules_list.Add(regex);
                }
            }
            log.Info($"{regex_rules_list.Count} rules have been read.");

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
                    var server_name = Encoding.UTF8.GetString(packet.DataSpan.Slice(6, packet.DataSpan.IndexOf((byte)0x00) - 6));
                    
                    foreach (var rule in regex_rules_list)
                    {
                        if (rule.IsMatch(server_name))
                        {
                            log.Info($"Block server with name: {server_name}");
                            return true;
                        }
                    }
                    
                    return false;
                }
            );

            Application.Run(new ApplicationContext());
        }
    }
}