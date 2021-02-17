using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wManager.Wow.Enums;

public class PoisonNPC
{
    public int id { get; set; }
    public Vector3 Position { get; set; }
    public string Name { get; set; }

    public ContinentId Continent { get; set;}

    public PoisonNPC(int ID, Vector3 POSITION, string NAME, ContinentId CONTINENT)
    {
        id = ID;
        Position = POSITION;
        Name = NAME;
        Continent = CONTINENT;
    }
}
