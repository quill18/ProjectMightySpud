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

[System.Serializable]
public class StructureColor
{
    public Color32 Color;
    public GameObject StructurePrefab;
}



/// <summary>
/// Dynamic terrain master is responsible for starting from a Landing Point
/// and spawning the 9-sliced DynamicTerrainChunk objects, as well as
/// despawning/spawning new chunks as the player/camera moves around.
/// </summary>
public class DynamicTerrainMaster : MonoBehaviour 
{

    bool rotTest = false;
    void Start () 
    {
        //BuildFromLandingSpot( new SphericalCoord( rotTest ? -4 : 0, 0 ) );
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            TESTING_SlideChunkArray();
        }
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

    public StructureColor[] StructureColors;

    float DegreesPerChunk = 5f;
    float WorldUnitsPerChunk = 1024;

    int numRows = 3;
    int numCols = 3;

    DynamicTerrainChunk[,] terrainChunks;

    public void BuildFromLandingSpot( SphericalCoord landingSpot )
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        terrainChunks = new DynamicTerrainChunk[numCols,numRows];

        // Create the center chunk.

        Quaternion rotation = CoordHelper.SphericalToRotation(landingSpot);

        if(rotTest)
            rotation *= Quaternion.Euler( 0, 0, 45 );

        Vector3 position = new Vector3(
            (-WorldUnitsPerChunk/2f),
            0,
            (-WorldUnitsPerChunk/2f)
        );

        // FIXME: Hardcoded?
        terrainChunks[1, 1] = BuildChunk( rotation, position );

        BuildChunkArray();

        //RebuildChunks();
        sw.Stop();
        Debug.Log("Terrain generation time: " + (sw.ElapsedMilliseconds/1000f));
    }

    void BuildChunkArray()
    {
        if( terrainChunks[1, 1] == null )
        {
            Debug.LogError("No middle chunk!");
            return;
        }

        for (int x = 0; x < numCols; x++)
        {
            for (int y = 0; y < numRows; y++)
            {
                if(terrainChunks[x,y] == null)
                {
                    float xDir = x-1;
                    float yDir = y-1;
                    // Build the missing chunk
                    DynamicTerrainChunk root = terrainChunks[1,1];

                    Quaternion rotation = root.ChunkRotation;
                    rotation *= Quaternion.Euler( 
                        DegreesPerChunk * yDir,
                        DegreesPerChunk * xDir,
                        0
                    );

                    Vector3 position = root.transform.position;
                    position += new Vector3(
                        WorldUnitsPerChunk * xDir,
                        0,
                        WorldUnitsPerChunk * yDir
                    );

                    terrainChunks[x,y] = BuildChunk( rotation, position );
                }
            }
        }
    }

    void TESTING_SlideChunkArray()
    {
        // Move south by one row of chunks

        for (int x = 0; x < numCols; x++)
        {
            // Top row, gets set to the values of the middle
            Destroy( terrainChunks[x, 2].gameObject );
            terrainChunks[x, 2] = terrainChunks[x, 1];

            // The middle gets set to the values of the bottom
            terrainChunks[x, 1] = terrainChunks[x, 0];

            // The bottom gets nulled out
            terrainChunks[x, 0] = null;

        }
        // Then we call BuildChunkArray() -- which will create a new bottom relative to the new middle
        BuildChunkArray();
    }


    void RebuildChunks()
    {
        // This function (at first) will tell each chunk
        // who its neighbours are.

        // Loop through all of our existing chunks.
        for (int x = 0; x < numCols; x++)
        {
            for (int y = 0; y < numRows; y++)
            {
                DynamicTerrainChunk left   = (x > 0)         ? terrainChunks[x-1, y  ] : null;
                DynamicTerrainChunk bottom    = (y > 0)      ? terrainChunks[x  , y-1] : null;
                DynamicTerrainChunk right  = (x < numCols-1) ? terrainChunks[x+1, y  ] : null;
                DynamicTerrainChunk top = (y < numRows-1)    ? terrainChunks[x  , y+1] : null;

                terrainChunks[x,y].SetNeighbors( 
                    left, top, right, bottom
                );

            }
        }
    }

    DynamicTerrainChunk BuildChunk( Quaternion rotation, Vector3 position )
    {

        GameObject go = new GameObject();
        go.transform.position = position;
        go.name = rotation.eulerAngles.ToString(); //position.ToString();

        DynamicTerrainChunk dtc = go.AddComponent<DynamicTerrainChunk>();
        dtc.HeightMapTexture = this.HeightMapTexture;
        dtc.StructureMapTexture = this.StructureMapTexture;
        dtc.Splats = this.Splats;

        dtc.ChunkRotation = rotation;
        dtc.DegreesPerChunk = DegreesPerChunk;
        dtc.WorldUnitsPerChunk = WorldUnitsPerChunk;
        dtc.StructureColors = StructureColors;

        dtc.BuildTerrain();

        return dtc;
    }


    public SphericalCoord WorldToSpherical( Vector3 worldPosition )
    {
        // FIXME: Do a raycast to determine which terrain chunk we are under

        // We are just going to fake it that we're on the middle chunk.

        return terrainChunks[1,1].WorldToSpherical(worldPosition);
    } 

    public void DestroyChunks()
    {
        // TODO: Destroy gameobjects, clear array, etc...

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                Destroy(terrainChunks[x,y].gameObject);
            }
        }
    }

}
