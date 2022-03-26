using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Vendors.Database.Models
{
    public class ModelCreatureTemplate
    {
        public int entry { get; }
        public string name { get; }
        public string subname { get; }
        public uint faction { get; }
        public int minLevel { get; }
        public int maxLevel { get; }

        public ModelCreature Creature;

        public bool IsHostile => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) <= 2;
        public bool IsNeutral => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) == 3;
        public bool IsFriendly => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 4;
        public bool IsNeutralOrFriendly => (int)WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate) >= 3;
        public Reaction GetRelationTypeTowardsMe => WoWFactionTemplate.FromId(faction).GetReactionTowards(ObjectManager.Me.FactionTemplate);
    }
}
