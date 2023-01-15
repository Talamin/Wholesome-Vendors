using robotManager.Helpful;

namespace WholesomeVendors.Database.Models
{
    public class ModelCreature
    {
        public int id { get; set; }
        public int map { get; set; }
        public int zoneid { get; set; }
        public int areaid { get; set; }
        public float position_x { get; set; }
        public float position_y { get; set; }
        public float position_z { get; set; }
        public Vector3 GetSpawnPosition => new Vector3(position_x, position_y, position_z);
    }
}
