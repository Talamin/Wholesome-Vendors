using robotManager.Helpful;
using wManager.Wow.Enums;

public class PoisonNPC
{
    public int id { get; set; }
    public Vector3 Position { get; set; }
    public string Name { get; set; }
    public string SubName { get; set; }
    public ContinentId Continent { get; set;}

    public PoisonNPC(int ID, Vector3 POSITION,ContinentId CONTINENT, string NAME, string SUBNAME)
    {
        id = ID;
        Position = POSITION;
        Name = NAME;
        Continent = CONTINENT;
        SubName = SUBNAME;
    }
}
