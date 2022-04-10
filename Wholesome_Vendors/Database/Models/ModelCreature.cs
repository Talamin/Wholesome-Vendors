using robotManager.Helpful;

namespace WholesomeVendors.Database.Models
{
    public class ModelCreature
    {
        public int id { get; }
        public int map { get; }
        public int zoneid { get; }
        public int areaid { get; }
        private float position_x { get; }
        private float position_y { get; }
        private float position_z { get; }
        public Vector3 GetSpawnPosition => new Vector3(position_x, position_y, position_z);
    }
}
