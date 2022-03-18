namespace Wholesome_Vendors.Database.Models
{
    public class ModelGameObjectTemplate
    {
        public string name { get; }
        public int entry { get; }

        public ModelGameObject GameObject { get; set; }
    }
}
