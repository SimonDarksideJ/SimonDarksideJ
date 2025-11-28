using Microsoft.Xna.Framework.Content;
using StarSystemData.Models;

namespace StarSystemData.Runtime;

/// <summary>
/// Content type reader for StarSystemDatabase
/// </summary>
public class StarSystemDataReader : ContentTypeReader<StarSystemDatabase>
{
    protected override StarSystemDatabase Read(ContentReader input, StarSystemDatabase existingInstance)
    {
        var database = new StarSystemDatabase();
        
        // Read system count
        int count = input.ReadInt32();
        
        // Read each system
        for (int i = 0; i < count; i++)
        {
            var system = new StarSystemRecord
            {
                Id = input.ReadInt32(),
                Name = input.ReadString(),
                X = input.ReadSingle(),
                Y = input.ReadSingle(),
                Z = input.ReadSingle(),
                RequirePermit = input.ReadBoolean()
            };
            
            // Read nullable permit name
            bool hasPermitName = input.ReadBoolean();
            if (hasPermitName)
            {
                system.PermitName = input.ReadString();
            }
            
            system.CoordsLocked = input.ReadBoolean();
            
            database.Add(system);
        }
        
        return database;
    }
}
