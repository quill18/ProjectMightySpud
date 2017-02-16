//-----------------------------------------------------------------------
// <copyright file="DynamicTerrainMaster.cs" company="Quill18 Productions">
//     Copyright (c) Quill18 Productions. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    float DegreesPerChunk = 10f;
    float WorldUnitsPerChunk = 1024;

    int numRows = 5;
    int numCols = 5;

    public void BuildFromLandingSpot( SphericalCoord landingSpot )
    {
        for (int x = 0; x < numCols; x++)
        {
            for (int y = 0; y < numRows; y++)
            {
                Quaternion rotation = CoordHelper.SphericalToRotation(landingSpot);

                rotation = rotation * Quaternion.Euler(
                    (-DegreesPerChunk * numRows/2f) + (y*DegreesPerChunk),
                    (-DegreesPerChunk * numCols/2f) + (x*DegreesPerChunk),
                    0
                );

                Vector3 position = new Vector3(
                    (-WorldUnitsPerChunk * numCols/2f) + (x*WorldUnitsPerChunk),
                    0,
                    (-WorldUnitsPerChunk * numRows/2f) + (y*WorldUnitsPerChunk)
                );

                BuildChunk( rotation, position );

            }
        }

    }

    void BuildChunk( Quaternion rotation, Vector3 position )
    {

        GameObject go = new GameObject();
        go.transform.position = position;

        DynamicTerrainChunk dtc = go.AddComponent<DynamicTerrainChunk>();
        dtc.HeightMapTexture = this.HeightMapTexture;
        dtc.StructureMapTexture = this.StructureMapTexture;
        dtc.Splats = this.Splats;

        dtc.ChunkRotation = rotation;
        dtc.DegreesPerChunk = DegreesPerChunk;
        dtc.WorldUnitsPerChunk = WorldUnitsPerChunk;
    }

}
