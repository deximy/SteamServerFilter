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

        public void Add(Regex rule)
        {
            if (!block_rules_list_.Contains(rule))
            {
                block_rules_list_.Add(rule);
            }
        }

        public void Add(string rule)
        {
            Add(new Regex(rule));
        }
    }
}
