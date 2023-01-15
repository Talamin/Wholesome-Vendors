using System.Collections.Generic;

namespace WholesomeVendors.Database.Models
{
    public class ModelNpcTrainer
    {
        public List<ModelCreatureTemplate> VendorTemplates = new List<ModelCreatureTemplate>();

        public int ID { get; set; }
        public int SpellID { get; set; }
        public int MoneyCost { get; set; }
        public int ReqSkillLine { get; set; }
        public int ReqSkillRank { get; set; }
        public int ReqLevel { get; set; }
    }
}
