//-----------------------------------------------------------------------
// <copyright file="DynamicTerrain.cs" company="Quill18 Productions">
//     Copyright (c) Quill18 Productions. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicTerrainChunk : MonoBehaviour 
{
    void Start () 
    {
        //BuildTerrain(  );
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

    /// <summary>
    /// We're going to dampen the heightmap by this amount. Larger values means a flatter/smoother map.
    /// </summary>
    float heightMapTextureScaling = 2f;

    TerrainChunkPosition terrainChunkPosition;

    public void BuildTerrain( TerrainChunkPosition tcp )
    {
        terrainChunkPosition = tcp;

        // Create Terrain and TerrainCollider components and add them to the GameObject
        Terrain terrain = gameObject.AddComponent<Terrain>();
        TerrainCollider terrainCollider = gameObject.AddComponent<TerrainCollider>();

        // Everything about the terrain is actually stored in a TerrainData
        TerrainData terrainData = new TerrainData();

        // Create the landscape
        BuildTerrainData(terrainData);

        // Define the "Splats" (terrain textures)
        BuildSplats(terrainData);

        // Apply the splats based on cliffiness
        PaintCliffs(terrainData);

        // Apply the data to the terrain and collider
        terrain.terrainData = terrainData;
        terrainCollider.terrainData = terrainData;

        // NOTE: If you make any changes to the terrain data after
        //       this, you should call terrain.Flush()
    }

    void BuildTerrainData(TerrainData terrainData)
    {
        // Define the size of the arrays that Unity's terrain will
        // use internally to represent the terrain. Bigger numbers
        // mean more fine details.


        // "Heightmap Resolution": "Pixel resolution of the terrain’s heightmap (should be a power of two plus one, eg, 513 = 512 + 1)."
        // AFAIK, this defines the size of the 2-dimensional array that holds the information about the terrain (i.e. terrainData.GetHeights())
        // Larger numbers lead to finer terrain details (if populated by a suitable source image heightmap).
        // As for actual physical size of the terrain (in Unity world space), this is defined as:
        //              terrainData.Size = terrainData.heightmapScale * terrainData.heightmapResolution
        terrainData.heightmapResolution = 512 + 1;

        // "Base Texture Resolution": "Resolution of the composite texture used on the terrain when viewed from a distance greater than the Basemap Distance"
        // AFAIK, this doesn't affect the terrain mesh -- only how the terrain texture (i.e. Splats) are rendered
        terrainData.baseMapResolution = 512 + 1;

        // "Detail Resolution" and "Detail Resolution Per Patch"
        // (used for Details -- i.e. grass/flowers/etc...)
        terrainData.SetDetailResolution( 1024, 32 );

        // Set the Unity worldspace size of the terrain AFTER you set the resolution.
        // This effectively just sets terrainData.heightmapScale for you, depending on the value of terrainData.heightmapResolution
        terrainData.size = new Vector3( 1024, 512 , 1024 );

        // Get the 2-dimensional array of floats that defines the actual height data for the terrain.
        // Each float has a value from 0..1, where a value of 1 means the maximum height of the terrain as defined by terrainData.size.y
        //
        // AFAIK, terrainData.heightmapWidth and terrainData.heightmapHeight will always be equal to terrainData.heightmapResolution
        //
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        Vector2[] quads = new Vector2[4];
        terrainChunkPosition.uvs.CopyTo(quads, 0);

        for (int i = 0; i < quads.Length; i++)
        {
            Vector2 q = quads[i];
            Debug.Log(string.Format("{0:F5}, {1:F5}", q.x, q.y));
        }

        FixQuads(quads);

        // Loop through each point in the terrainData heightmap.
        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                // Normalize x and y to a value from 0..1
                // NOTE: We are INVERTING the x and y because internally Unity does this
                float yPos = (float)x / (float)terrainData.heightmapWidth;
                float xPos = (float)y / (float)terrainData.heightmapHeight;

                // uv will contain correct position of the pixel in the heightmap texture
                // that we will be reading from
                Vector3 uv = new Vector3(
                    xPos, yPos, 0
                );

                uv = UVFromQuads( new Vector2(xPos, yPos ), quads );

                // Get the pixel from the heightmap image texture at the appropriate position
                Color pix = HeightMapTexture.GetPixelBilinear( uv.x, uv.y );

                // Update the heights array
                heights[x,y] = pix.grayscale / heightMapTextureScaling;
            }
        }

        // Update the terrain data based on our changed heights array
        terrainData.SetHeights(0, 0, heights);

    }

    void FixQuads(Vector2[] quads)
    {
        // THIS IS NOT A REAL SOLUTION -- Just a bit of brute force to
        // get the current demo going.
        // In particular, probably doesn't deal with the seam at the poles
        if(
            quads[0].x > quads[1].x && 
            quads[0].x > quads[3].x 
        )
        {
            quads[0].x -= 1;
        }
        if(
            quads[2].x > quads[1].x && 
            //quads[2].x > quads[0].x && 
            quads[2].x > quads[3].x 
        )
        {
            // 2 is odd man out
            quads[2].x -= 1;
        }
        if(
            quads[1].x < quads[0].x && 
            quads[1].x < quads[3].x && 
            quads[1].x < quads[2].x 
        )
        {
            // 1 is odd man out
            quads[1].x += 1;
        }
        if(
            quads[3].x < quads[0].x && 
            quads[3].x < quads[1].x && 
            quads[3].x < quads[2].x 
        )
        {
            // 3 is odd man out
            quads[3].x += 1;
        }


/*        if(quads[0].y > quads[2].y)
            quads[2].y += 1;
        if(quads[1].y > quads[3].y)
            quads[3].y += 1;
*/
/*        for (int i = 0; i < quads.Length; i++)
        {
            if(quads[i].x >= 1f)
                quads[i].x -= 1;
            if(quads[i].y >= 1f)
                quads[i].y -= 1;
        }
*/
    }

    Vector2 UVFromQuads(Vector2 pos, Vector2[] quads)
    {

        Vector2 uv = Vector2.zero;

        float dist0 = 1 - (1-pos.x) * (1-pos.y);
        float dist1 = 1 - (pos.x)   * (1-pos.y);
        float dist2 = 1 - (1-pos.x) * (pos.y);
        float dist3 = 1 - (pos.x)   * (pos.y);

        uv = 
            Vector2.Lerp( quads[0],  Vector2.zero, dist0 ) + 
            Vector2.Lerp( quads[1],  Vector2.zero, dist1 ) + 
            Vector2.Lerp( quads[2],  Vector2.zero, dist2 ) + 
            Vector2.Lerp( quads[3],  Vector2.zero, dist3 );

        return uv;

    }

    Vector2 GetHeightFromMapCorrectedForProjection()
    {
        return Vector2.zero;
    }

    void BuildSplats(TerrainData terrainData)
    {
        SplatPrototype[] splatPrototypes = new SplatPrototype[ Splats.Length ];

        for (int i = 0; i < Splats.Length; i++)
        {
            splatPrototypes[i] = new SplatPrototype();
            splatPrototypes[i].texture = Splats[i].Texture;
            splatPrototypes[i].tileSize = new Vector2(Splats[i].Size, Splats[i].Size);
        }

        terrainData.splatPrototypes = splatPrototypes;
    }

    void PaintCliffs(TerrainData terrainData)
    {
        // splatMaps is a three dimensional array in the form of:
        //     splatMaps[ x, y, splatTextureID ] = (opacity from 0..1)
        float[,,] splatMaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        // Loop through each pixel in the alpha maps
        for (int aX = 0; aX < terrainData.alphamapWidth; aX++)
        {
            for (int aY = 0; aY < terrainData.alphamapHeight; aY++)
            {
                // Normal to 0..1
                float x = (float)aX / terrainData.alphamapWidth;
                float y = (float)aY / terrainData.alphamapHeight;

                // Find the steepness of the terrain at this point
                float angle = terrainData.GetSteepness( y, x ); // NOTE: x and y are flipped

                // 0 is "flat ground", 1 is "cliff ground"
                float cliffiness = angle / 90.0f;

                splatMaps[aX, aY, 0] = 1 - cliffiness; // Ground texture is inverse of cliffiness
                splatMaps[aX, aY, 1] =     cliffiness; // Cliff texture is cliffiness
            }
        }

        // Update the terrain texture maps
        terrainData.SetAlphamaps(0, 0, splatMaps);
    }
}
