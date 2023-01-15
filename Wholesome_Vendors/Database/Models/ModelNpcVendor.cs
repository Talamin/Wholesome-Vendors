namespace WholesomeVendors.Database.Models
{
    public class ModelNpcVendor
    {
        public int entry { get; set; }
        public int item { get; set; }

        public ModelCreatureTemplate CreatureTemplate { get; set; }
    }
}
