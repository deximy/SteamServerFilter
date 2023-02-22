using Microsoft.Win32.TaskScheduler;

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
            var auto_start_item = new ToolStripMenuItem() {
                Text = "开机自动启动",
                Checked = TaskService.Instance.GetFolder("\\").Tasks.Any(task => task.Name == "SteamServerFilter Startup"),
            };
            auto_start_item.Click += (sender, e) => {
                auto_start_item.Checked = !auto_start_item.Checked;
            };
            auto_start_item.CheckedChanged += (sender, e) => {
                if (auto_start_item.Checked)
                {
                    EnableAutoRunningWithTaskService();
                }
                else
                {
                    DisableAutoRunningWithTaskService();
                }
            };
            tray_icon.ContextMenuStrip.Items.Add(auto_start_item);
            var exit_item = new ToolStripMenuItem() {
                Text = "退出",
            };
            exit_item.Click += (sender, e) => {
                Application.Exit();
            };
            tray_icon.ContextMenuStrip.Items.Add(exit_item);
            
            LogService.Info("=================================================");
            LogService.Info("Program starts.");
            
            var block_rules_file_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "block_rules.txt");
            if (!File.Exists(block_rules_file_path))
            {
                File.Create(block_rules_file_path).Close();
            }

            var block_rules_repo = new BlockRulesRepository();
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

            var inbound_server_name_filter_service = new InboundServerNameFilterService(block_rules_repo);

            Application.Run(new ApplicationContext());
        }

        static void EnableAutoRunningWithTaskService()
        {
            TaskDefinition task_definition = TaskService.Instance.NewTask();

            task_definition.RegistrationInfo.Description = "Run SteamServerFilter when system starts";
            task_definition.Settings.Enabled = true;
            task_definition.Principal.RunLevel = TaskRunLevel.Highest;

            LogonTrigger trigger = (LogonTrigger)task_definition.Triggers.AddNew(TaskTriggerType.Logon);
            trigger.Enabled = true;
            trigger.Delay = TimeSpan.FromSeconds(30);

            ExecAction action = (ExecAction)task_definition.Actions.AddNew(TaskActionType.Execute);
            action.Path = $"\"{Application.ExecutablePath}\"";

            TaskService.Instance.RootFolder.RegisterTaskDefinition("SteamServerFilter Startup", task_definition);
        }

        static void DisableAutoRunningWithTaskService()
        {
            var folder = TaskService.Instance.GetFolder("\\");
            if (folder.Tasks.Any(task => task.Name == "SteamServerFilter Startup"))
            {
                folder.DeleteTask("SteamServerFilter Startup");
            }
        }
    }
}
