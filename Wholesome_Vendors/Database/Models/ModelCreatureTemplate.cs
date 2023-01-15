using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.Database.Models
{
    public class ModelCreatureTemplate
    {
        public int entry { get; set; }
        public string name { get; set; }
        public string subname { get; set; }
        public uint faction { get; set; }
        public int minLevel { get; set; }
        public int maxLevel { get; set; }

        public ModelCreature Creature { get; set; }

        public bool IsHostile => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) <= 2;
        public bool IsNeutral => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) == 3;
        public bool IsFriendly => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 4;
        public bool IsNeutralOrFriendly => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 3;
        public Reaction GetRelationTypeTowardsMe => WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate);
    }
}
