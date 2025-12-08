using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using StarSystemData.Models;

namespace StarSystemData.Pipeline;

/// <summary>
/// Content type writer for StarSystemDatabase
/// </summary>
[ContentTypeWriter]
public class StarSystemDataWriter : ContentTypeWriter<StarSystemDatabase>
{
    protected override void Write(ContentWriter output, StarSystemDatabase value)
    {
        // Write system count
        output.Write(value.Count);
        
        // Write each system
        foreach (var system in value.Systems)
        {
            output.Write(system.Id);
            output.Write(system.Name);
            output.Write(system.X);
            output.Write(system.Y);
            output.Write(system.Z);
            output.Write(system.RequirePermit);
            
            // Write nullable permit name
            bool hasPermitName = !string.IsNullOrEmpty(system.PermitName);
            output.Write(hasPermitName);
            if (hasPermitName)
            {
                output.Write(system.PermitName!);
            }
            
            output.Write(system.CoordsLocked);
        }
    }
    
    public override string GetRuntimeReader(TargetPlatform targetPlatform)
    {
        return "StarSystemData.Runtime.StarSystemDataReader, StarSystemData";
    }
    
    public override string GetRuntimeType(TargetPlatform targetPlatform)
    {
        return "StarSystemData.Models.StarSystemDatabase, StarSystemData.Models";
    }
}
