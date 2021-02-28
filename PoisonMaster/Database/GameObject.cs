using DatabaseManager.Tables;
using robotManager.Helpful;
using wManager.Wow.Class;

public class GameObjects
{
    public int Id { get; set; }
    public Vector3 Position { get; set; }
    public string Name { get; set; }


    public GameObjects(gameobject objectFromExternalDb)
    {
        Id = objectFromExternalDb.id;
        Position = objectFromExternalDb.Position;
        Name = objectFromExternalDb.Name;
    }
}

