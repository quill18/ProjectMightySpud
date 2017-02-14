//-----------------------------------------------------------------------
// <copyright file="DynamicTerrainMaster.cs" company="Quill18 Productions">
//     Copyright (c) Quill18 Productions. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkPosition
{
    public Quaternion Rotation;

    public Vector2[] uvs = new Vector2[4];  
}

/// <summary>
/// The structure definining the textures used to paint the terrain
/// </summary>
[System.Serializable]
public class SplatData
{
    public Texture2D Texture;
    public float Size = 15;
}

/// <summary>
/// Dynamic terrain master is responsible for starting from a Landing Point
/// and spawning the 9-sliced DynamicTerrainChunk objects, as well as
/// despawning/spawning new chunks as the player/camera moves around.
/// </summary>
public class DynamicTerrainMaster : MonoBehaviour 
{
    void Start () 
    {
        BuildFromLandingSpot( new SphericalCoord( 0, 0 ) );
    }

    float DegreesPerChunk = 10f;

    /// <summary>
    /// The image texture used to populate the height map.
    /// Colour data is discarded: Only greyscale matters.
    /// </summary>
    public Texture2D HeightMapTexture;

    /// <summary>
    /// Unused at this time.
    /// </summary>
    public Texture2D StructureMapTexture;

    /// <summary>
    /// The array of splat data used to paint the terrain
    /// </summary>
    public SplatData[] Splats;

    public void BuildFromLandingSpot(SphericalCoord landingSpot)
    {

        //MakeChunk(rot,pos);

        int numRows = 3;
        int numCols = 3;

        float offX = -1024f * numCols/2f;
        float offY = -1024f * numRows/2f;

        float rotOffX = -DegreesPerChunk * numCols/2f;
        float rotOffY = -DegreesPerChunk * numRows/2f;

        for (int x = 0; x < numCols; x++)
        {
            for (int y = 0; y < numRows; y++)
            {
                Vector3 pos = new Vector3(offX + x*1024f, 0, offY + y*1024f);
                Quaternion rot = Quaternion.Euler( landingSpot.Latitude, landingSpot.Longitude, 0 );
                rot = rot * Quaternion.Euler( rotOffY + DegreesPerChunk*y, rotOffX + DegreesPerChunk*x, 0);
                MakeChunk(rot, pos);

            }
        }

        for (int i = 0; i < 24; i++)
        {
            //rot = rot * Quaternion.Euler( -DegreesPerChunk, 0, 0);
            //pos.z -= 1024;
            //MakeChunk(rot, pos);

        }


    }

    void MakeChunk(Quaternion rot, Vector3 pos)
    {
        TerrainChunkPosition tcp = new TerrainChunkPosition();

        tcp.Rotation = rot;

        float halfDegreesPerChunk = DegreesPerChunk / 2f;

        tcp.uvs[0] = CoordHelper.RotationToUV( tcp.Rotation * Quaternion.Euler( -halfDegreesPerChunk, -halfDegreesPerChunk, 0) );
        tcp.uvs[1] = CoordHelper.RotationToUV( tcp.Rotation * Quaternion.Euler( -halfDegreesPerChunk, halfDegreesPerChunk, 0) );
        tcp.uvs[2] = CoordHelper.RotationToUV( tcp.Rotation * Quaternion.Euler( halfDegreesPerChunk, -halfDegreesPerChunk, 0) );
        tcp.uvs[3] = CoordHelper.RotationToUV( tcp.Rotation * Quaternion.Euler( halfDegreesPerChunk, halfDegreesPerChunk, 0) );


/*        tcp.uvs[0] = new Vector2(0.0f  , 0.45f);
        tcp.uvs[1] = new Vector2(0.1f  , 0.45f);
        tcp.uvs[2] = new Vector2(0.0f  , 0.55f);
        tcp.uvs[3] = new Vector2(0.1f  , 0.55f);
*/

        GameObject go = new GameObject();
        go.name = "TC - " + tcp.Rotation.eulerAngles.ToString();
        Debug.Log("Chunk: " + go.name);
        go.transform.position = pos;

        DynamicTerrainChunk dtc = go.AddComponent<DynamicTerrainChunk>();
        dtc.HeightMapTexture = HeightMapTexture;
        dtc.StructureMapTexture = StructureMapTexture;
        dtc.Splats = Splats;
        dtc.BuildTerrain(tcp);

    }

}
