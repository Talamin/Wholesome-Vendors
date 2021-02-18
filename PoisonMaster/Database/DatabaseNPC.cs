using DatabaseManager.Tables;
using robotManager.Helpful;
using wManager.Wow.Class;

public class DatabaseNPC
{
    public int Id { get; set; }
    public Vector3 Position { get; set; }
    public string Name { get; set; }

    public DatabaseNPC(Npc npcFromInternalDB)
    {
        Id = npcFromInternalDB.Entry;
        Position = npcFromInternalDB.Position;
        Name = npcFromInternalDB.Name;
    }

    public DatabaseNPC(creature npcFromExternalDb)
    {
        Id = npcFromExternalDb.id;
        Position = npcFromExternalDb.Position;
        Name = npcFromExternalDb.Name;
    }
}

