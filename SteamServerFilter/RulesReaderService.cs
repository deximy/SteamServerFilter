namespace SteamServerFilter
{
    public class RulesReaderService
    {
        private readonly BlockRulesRepository block_rules_repo_;

        public RulesReaderService(BlockRulesRepository block_rules_repo) 
        {
            block_rules_repo_ = block_rules_repo;
        }

        public void ReadRulesFromFile(string path_to_file)
        {
            using (StreamReader stream_reader = new StreamReader(path_to_file))
            {
                while (!stream_reader.EndOfStream)
                {
                    string? rule = stream_reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(rule))
                    {
                        continue;
                    }
                    block_rules_repo_.Add(rule);
                }
            }
            LogService.Info($"{block_rules_repo_.Get().Count} rules have been read.");
        }

        public void TryReadRulesFromFile(string path_to_file)
        {
            if (!File.Exists(path_to_file))
            {
                File.Create(path_to_file).Close();
            }
            ReadRulesFromFile(path_to_file);
        }
    }
}
