using System.Collections.Generic;

namespace WholesomeVendors.Database.Models
{
    public class FullJSONModel
    {
        public List<ModelItemTemplate> Waters { get; set; }
        public List<ModelItemTemplate> Foods { get; set; }
        public List<ModelItemTemplate> Ammos { get; set; }
        public List<ModelItemTemplate> Poisons { get; set; }
        public List<ModelItemTemplate> Bags { get; set; }
        public List<ModelCreatureTemplate> Sellers { get; set; }
        public List<ModelCreatureTemplate> Repairers { get; set; }
        public List<ModelCreatureTemplate> Trainers { get; set; }
        public List<ModelGameObjectTemplate> Mailboxes { get; set; }
        public List<ModelSpell> Mounts { get; set; }
        public List<ModelSpell> RidingSpells { get; set; }
    }
}
