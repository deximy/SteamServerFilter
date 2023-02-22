using Microsoft.Win32.TaskScheduler;

namespace SteamServerFilter
{
    public static class StartupService
    {
        private static readonly TaskFolder tasks_folder_;
        private static readonly Microsoft.Win32.TaskScheduler.Task task_;

        static StartupService()
        {
            tasks_folder_ = TaskService.Instance.GetFolder("\\");
            var task = tasks_folder_.Tasks.FirstOrDefault(task => task.Name == "SteamServerFilter Startup");
            if (task == null)
            {
                var task_definition =  tasks_folder_.TaskService.NewTask();
                task_definition.Settings.Enabled = false;
                task_definition.RegistrationInfo.Description = "Run SteamServerFilter when system starts";
                task_definition.Principal.RunLevel = TaskRunLevel.Highest;

                LogonTrigger trigger = (LogonTrigger)task_definition.Triggers.AddNew(TaskTriggerType.Logon);
                trigger.Enabled = true;
                trigger.Delay = TimeSpan.FromSeconds(30);

                ExecAction action = (ExecAction)task_definition.Actions.AddNew(TaskActionType.Execute);
                action.Path = $"\"{Application.ExecutablePath}\"";

                task_ = TaskService.Instance.RootFolder.RegisterTaskDefinition("SteamServerFilter Startup", task_definition);
            }
            else
            {
                var action = task.Definition.Actions.First(action => action.ActionType == TaskActionType.Execute);
                if (!((ExecAction)action).Path.Contains(Application.ExecutablePath))
                {
                    ((ExecAction)action).SetValidatedPath(Application.ExecutablePath);
                    task_ = TaskService.Instance.RootFolder.RegisterTaskDefinition(task.Name, task.Definition);
                }
                else
                {
                    task_ = task;
                }
            }
        }

        public static bool Enable
        {
            get
            {
                return task_.Enabled;
            }
            set
            {
                task_.Enabled = value;
            }
        }
    }
}
