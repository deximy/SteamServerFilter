using System.Text.RegularExpressions;

namespace SteamServerFilter
{
    public class BlockRulesRepository
    {
        private readonly List<Regex> block_rules_list_;

        public BlockRulesRepository()
        {
            block_rules_list_ = new();
        }

        public IReadOnlyCollection<Regex> Get()
        {
            return block_rules_list_;
        }

        public void Add(string rule)
        {
            if (!block_rules_list_.Any(i => i.ToString() == rule))
            {
                block_rules_list_.Add(new Regex(rule));
            }
        }

        public void Clear()
        {
            block_rules_list_.Clear();
        }
    }
}
