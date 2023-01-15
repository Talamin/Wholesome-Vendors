namespace WholesomeVendors.Database.Models
{
    public class ModelSpell
    {
        public ModelItemTemplate AssociatedItem { get; set; }
        public ModelNpcTrainer NpcTrainer { get; set; }

        public int Id { get; set; }
        public int effectBasePoints_2 { get; set; }
        public string name_lang_1 { get; set; }
    }
}
