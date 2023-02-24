namespace SteamServerFilter
{
    class Program
    {
        static void Main(string[] args)
        {
            LogService.Info("=================================================");
            LogService.Info("Program starts.");

            var block_rules_repo = new BlockRulesRepository();
            var blocked_endpoints_repo = new BlockedEndpointsRepository();

            var block_rules_file_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "block_rules.txt");
            if (!File.Exists(block_rules_file_path))
            {
                File.Create(block_rules_file_path).Close();
            }

            using (StreamReader stream_reader = new StreamReader(block_rules_file_path))
            {
                while (!stream_reader.EndOfStream)
                {
                    string? rule = stream_reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(rule))
                    {
                        continue;
                    }
                    block_rules_repo.Add(rule);
                }
            }
            LogService.Info($"{block_rules_repo.Get().Count} rules have been read.");

            new OutboundRequestFilterService(blocked_endpoints_repo);
            new InboundServerNameFilterService(block_rules_repo, blocked_endpoints_repo);

            InitTrayContextMenu();
            Application.Run(new ApplicationContext());
        }

        static void InitTrayContextMenu()
        {
            var tray_icon = new NotifyIcon() {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Text = "SteamServerFilter",
                Visible = true,
                ContextMenuStrip = new(),
            };
            tray_icon.ContextMenuStrip.Items.Add(InitStartupSettingItem());
            tray_icon.ContextMenuStrip.Items.Add(InitStrictModeItem());
            tray_icon.ContextMenuStrip.Items.Add(InitExitItem(tray_icon));
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
                    throw new NotImplementedException();
                }
                else
                {
                    throw new NotImplementedException();
                }
            };
            return strict_mode_item;
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
    }
}
