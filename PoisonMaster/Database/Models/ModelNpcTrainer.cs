using System.Collections.Generic;

namespace Wholesome_Vendors.Database.Models
{
    public class ModelNpcTrainer
    {
        public List<ModelCreatureTemplate> VendorTemplates = new List<ModelCreatureTemplate>();

        public int ID { get; }
        public int SpellID { get; }
        public int MoneyCost { get; }
        public int ReqSkillLine { get; }
        public int ReqSkillRank { get; }
        public int ReqLevel { get; }
    }
}
