using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SteamServerFilter
{
    class ProcessMode
    {
        public bool Enabled { get; set; }
        public List<string> ProcessNames { get; } = new List<string>();
        private bool processExists;
        private bool alreadyprinted = false;

        public bool ProcessExists
        {
            get => processExists;
        }

        public ProcessMode() 
        {
            processExists = false;
        }

        public void read_processnames_from_file(string path_to_file)
        {
            if (!File.Exists(path_to_file))
            {
                File.Create(path_to_file).Close();
                using (StreamWriter stream_writer = new StreamWriter(path_to_file))
                {
                    //default process is l4d2
                    stream_writer.WriteLine("left4dead2");
                }
            }
            using (StreamReader stream_reader = new StreamReader(path_to_file))
            {
                while (!stream_reader.EndOfStream)
                {
                    string? processname = stream_reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(processname))
                    {
                        continue;
                    }
                    if (!ProcessNames.Contains(processname))
                        ProcessNames.Add(processname);
                }
            }
        }

        public void check_process()
        {
            if (!Enabled)
            {
                processExists = true;
                return;
            }
            var cp = Process.GetProcesses();
            foreach (var p in cp)
            {
                if (ProcessNames.Contains(p.ProcessName))
                {
                    processExists = true;
                    if (!alreadyprinted) LogService.Debug("Process Exists");
                    alreadyprinted = true;
                    return;
                }
            }
            alreadyprinted = false;
            processExists = false;
        }

        public override string ToString() 
        {
            string s = string.Empty;
            ProcessNames.ForEach(
                name => s += (name + ' '));
            return s;
        }
    }
}
