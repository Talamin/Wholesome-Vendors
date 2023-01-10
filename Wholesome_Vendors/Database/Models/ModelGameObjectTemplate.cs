namespace WholesomeVendors.Database.Models
{
    public class ModelGameObjectTemplate
    {
        public string name { get; set; }
        public int entry { get; set; }

        public ModelGameObject GameObject { get; set; }
    }
}
