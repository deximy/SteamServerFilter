using System.Diagnostics;

namespace SteamServerFilter
{
    class Program
    {
        public static readonly string version = "1.4.0";

        private static readonly BlockRulesRepository block_rules_repo_;
        private static readonly BlockedEndpointsRepository blocked_endpoints_repo_;

        private static InboundServerNameFilterService? inbound_server_name_filter_service_;
        private static OutboundRequestFilterService? outbount_request_filter_service_;
        private static SniffingServerNameService? sniffing_server_name_service_;
        private static RulesReaderService? rules_reader_service_;
        public static ProcessMode? processmode;

        static Program()
        {
            block_rules_repo_ = new BlockRulesRepository();
            blocked_endpoints_repo_ = new BlockedEndpointsRepository();
        }

        static void Main(string[] args)
        {
            LogService.Info("=================================================");
            LogService.Info($"Program starts. Current version: {version}");

            processmode = new ProcessMode();
            processmode.read_processnames_from_file(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "process_names.txt"));
            LogService.Info(processmode.ToString());

            rules_reader_service_ = new RulesReaderService(block_rules_repo_);
            rules_reader_service_.TryReadRulesFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "block_rules.txt"));

            sniffing_server_name_service_ = new SniffingServerNameService();
            inbound_server_name_filter_service_ = new InboundServerNameFilterService(block_rules_repo_, blocked_endpoints_repo_, sniffing_server_name_service_);
            outbount_request_filter_service_ = new OutboundRequestFilterService(blocked_endpoints_repo_, sniffing_server_name_service_);

            InitTrayContextMenu();
            Application.Run(new ApplicationContext());
        }

        static void InitTrayContextMenu()
        {
            var tray_icon = new NotifyIcon() {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Text = $"SteamServerFilter {version}",
                Visible = true,
                ContextMenuStrip = new(),
            };
            tray_icon.ContextMenuStrip.Items.AddRange(
                new[] {
                    InitStartupSettingItem(),
                    InitStrictModeItem(),
                    InitProcessItem(),
                    InitReloadRulesItem(),
                    InitOpenContainerFolderItem(),
                    InitExitItem(tray_icon)
                }
            );
        }

        static ToolStripMenuItem InitStartupSettingItem()
        {
            var startup_setting_item = new ToolStripMenuItem() {
                Text = "开机自动启动",
                Checked = StartupService.Enable,
            };
            startup_setting_item.Click += (sender, e) => {
                startup_setting_item.Checked = !startup_setting_item.Checked;
            };
            startup_setting_item.CheckedChanged += (sender, e) => {
                if (startup_setting_item.Checked)
                {
                    StartupService.Enable = true;
                }
                else
                {
                    StartupService.Enable = false;
                }
            };
            return startup_setting_item;
        }

        static ToolStripMenuItem InitStrictModeItem()
        {
            outbount_request_filter_service_?.Start();
            var strict_mode_item = new ToolStripMenuItem() {
                Text = "严格模式",
                Checked = true,
            };
            strict_mode_item.Click += (sender, e) => {
                strict_mode_item.Checked = !strict_mode_item.Checked;
            };
            strict_mode_item.CheckedChanged += (sender, e) => {
                if (strict_mode_item.Checked)
                {
                    outbount_request_filter_service_?.Start();
                }
                else
                {
                    outbount_request_filter_service_?.Pause();
                }
            };
            return strict_mode_item;
        }

        static ToolStripMenuItem InitReloadRulesItem()
        {
            var reload_rules_item = new ToolStripMenuItem() {
                Text = "重载所有规则",
            };
            reload_rules_item.Click += (sender, e) => {
                block_rules_repo_.Clear();
                processmode?.read_processnames_from_file(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "process_names.txt"));
                rules_reader_service_?.ReadRulesFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "block_rules.txt"));
            };
            return reload_rules_item;
        }

        static ToolStripMenuItem InitOpenContainerFolderItem()
        {
            var open_container_folder = new ToolStripMenuItem() {
                Text = "打开所在文件夹",
            };
            open_container_folder.Click += (sender, e) => {
                Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory);
            };
            return open_container_folder;
        }

        static ToolStripMenuItem InitExitItem(NotifyIcon tray_icon)
        {
            var exit_item = new ToolStripMenuItem() {
                Text = "退出",
            };
            exit_item.Click += (sender, e) => {
                tray_icon.Visible = false;
                Application.Exit();
            };
            return exit_item;
        }

        static ToolStripMenuItem InitProcessItem()
        {
            var process_item = new ToolStripMenuItem()
            {
                Text = "进程模式",
                Checked = true
            };
            processmode.Enabled = true;
            process_item.Click += (sender, e) => {
                process_item.Checked = !process_item.Checked;
            };
            process_item.CheckedChanged += (sender, e) => {
                if (processmode != null)
                {
                    if (process_item.Checked)
                    {
                        processmode.Enabled = true;
                    }
                    else
                    {
                        processmode.Enabled = false;
                    }
                }
            };
            return process_item;
        }
    }
}
