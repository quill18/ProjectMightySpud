//-----------------------------------------------------------------------
// <copyright file="DynamicTerrain.cs" company="Quill18 Productions">
//     Copyright (c) Quill18 Productions. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicTerrain : MonoBehaviour 
{
    void Start () 
    {
        BuildTerrain();
    }

    void BuildTerrain()
    {

        Terrain terrain = gameObject.AddComponent<Terrain>();
        TerrainCollider terrainCollider = gameObject.AddComponent<TerrainCollider>();

        TerrainData terrainData = new TerrainData();

        BuildTerrainData(terrainData);

        BuildSplats(terrainData);

        PaintCliffs(terrainData);

        terrain.terrainData = terrainData;
        terrainCollider.terrainData = terrainData;
    }

    public Texture2D HeightMapTexture;
    public Texture2D StructureMapTexture;

    public Texture2D[] SplatTextures;
    public float[] SplatSizes;

    public Rect ViewingRectangle = new Rect(0, 0, 1, 1);

    void BuildTerrainData(TerrainData terrainData)
    {

        terrainData.heightmapResolution = 512 + 1;
        terrainData.baseMapResolution = 512 + 1;
        terrainData.SetDetailResolution( 1024, 32 );

        // Set the size of the terrain AFTER you set the resolution.

        terrainData.size = new Vector3( 2048, 512 , 1024 );

        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        float perlinScaleFactor = 0.1f;

        Color[] heightMapPixels = HeightMapTexture.GetPixels();

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                // NOTE: We are INVERTING the x and y
                float yPos = (float)x / (float)terrainData.heightmapWidth;
                float xPos = (float)y / (float)terrainData.heightmapHeight;

                // Let's zoom in to the middle of the image map

                xPos = xPos * ViewingRectangle.width  + ViewingRectangle.xMin;
                yPos = yPos * ViewingRectangle.height + ViewingRectangle.yMin;

                Color pix = HeightMapTexture.GetPixelBilinear( xPos, yPos );

                heights[x,y] = pix.grayscale / 2f;
            }
        }

        terrainData.SetHeights(0, 0, heights);

    }

    void BuildSplats(TerrainData terrainData)
    {
        SplatPrototype[] splatPrototypes = new SplatPrototype[ SplatTextures.Length ];

        for (int i = 0; i < SplatTextures.Length; i++)
        {
            splatPrototypes[i] = new SplatPrototype();
            splatPrototypes[i].texture = SplatTextures[i];
            splatPrototypes[i].tileSize = new Vector2(SplatSizes[i], SplatSizes[i]);
        }

        terrainData.splatPrototypes = splatPrototypes;
    }

    void PaintCliffs(TerrainData terrainData)
    {
        float[,,] splatMaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        for (int aX = 0; aX < terrainData.alphamapWidth; aX++)
        {
            for (int aY = 0; aY < terrainData.alphamapHeight; aY++)
            {
                
                float x = (float)aX / terrainData.alphamapWidth;
                float y = (float)aY / terrainData.alphamapHeight;

                float angle = terrainData.GetSteepness( y, x ); // NOTE: x and y are flipped

                // 0 is "flat ground", 1 is "cliff ground"
                float cliffiness = angle / 90.0f;
                splatMaps[aX, aY, 0] = 1 - cliffiness;
                splatMaps[aX, aY, 1] =     cliffiness;
            }
        }

        terrainData.SetAlphamaps(0, 0, splatMaps);
    }
}
