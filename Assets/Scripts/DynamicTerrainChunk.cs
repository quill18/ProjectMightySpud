//-----------------------------------------------------------------------
// <copyright file="DynamicTerrainChunk.cs" company="Quill18 Productions">
//     Copyright (c) Quill18 Productions. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicTerrainChunk : MonoBehaviour 
{
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

    /// <summary>
    /// We're going to dampen the heightmap by this amount. Larger values means a flatter/smoother map.
    /// </summary>
    float heightMapTextureScaling = 2f;

    // The rotation that represents the point in the center of this terrain chunk
    // Reminder: You can easily convert this to a SphericalCoord (i.e. Lat/Long)
    public Quaternion ChunkRotation;

    // Degrees per chunk is used to determine the Rotation (or SphericalCoord)
    // of any point on our terrain. This can be used to tell the player what
    // his/her lat/long is, but ALSO we can be UV information from this
    public float DegreesPerChunk;

    public float WorldUnitsPerChunk;

    Terrain terrain;
    TerrainData terrainData;

    /// <summary>
    /// The height of the tallest possible peak in our terrain
    /// </summary>
    public float TerrainHeight = 512;

    public void BuildTerrain(  )
    {
        // Create Terrain and TerrainCollider components and add them to the GameObject
        terrain = gameObject.AddComponent<Terrain>();
        TerrainCollider terrainCollider = gameObject.AddComponent<TerrainCollider>();

        // Everything about the terrain is actually stored in a TerrainData
        terrainData = new TerrainData();

        // Create the landscape
        BuildTerrainData(terrainData);

        // Define the "Splats" (terrain textures)
        BuildSplats(terrainData);

        // Apply the data to the terrain and collider
        terrain.terrainData = terrainData;
        terrainCollider.terrainData = terrainData;


        // NOTE: If you make any changes to the terrain data after
        //       this, you should call terrain.Flush()


        // Now make the structures

        BuildStructures();

        // Apply the splats based on cliffiness
        PaintCliffs(terrainData);


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
        terrainData.heightmapResolution = 128 + 1;

        // "Base Texture Resolution": "Resolution of the composite texture used on the terrain when viewed from a distance greater than the Basemap Distance"
        // AFAIK, this doesn't affect the terrain mesh -- only how the terrain texture (i.e. Splats) are rendered
        terrainData.baseMapResolution = 512 + 1;

        // "Detail Resolution" and "Detail Resolution Per Patch"
        // (used for Details -- i.e. grass/flowers/etc...)
        terrainData.SetDetailResolution( 1024, 32 );

        // Set the Unity worldspace size of the terrain AFTER you set the resolution.
        // This effectively just sets terrainData.heightmapScale for you, depending on the value of terrainData.heightmapResolution
        terrainData.size = new Vector3( WorldUnitsPerChunk, TerrainHeight , WorldUnitsPerChunk );

        // Get the 2-dimensional array of floats that defines the actual height data for the terrain.
        // Each float has a value from 0..1, where a value of 1 means the maximum height of the terrain as defined by terrainData.size.y
        //
        // AFAIK, terrainData.heightmapWidth and terrainData.heightmapHeight will always be equal to terrainData.heightmapResolution
        //
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        float halfDegreesPerChunk = DegreesPerChunk / 2f;

        // Caching these dimensions and...
        int w = terrainData.heightmapWidth;
        int h = terrainData.heightmapHeight;

        // Replacing loop divisions with mults cuts this function by about 10%
        //  -- Shout out to Karl Goodloe
        float widthAdjust = 1f / (w-1f);
        float heightAdjust = 1f / (h-1f);

        // Loop through each point in the terrainData heightmap.
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                // Normalize x and y to a value from 0..1
                // NOTE: We are INVERTING the x and y because internally Unity does this
                float xPos = (float)x * widthAdjust;
                float yPos = (float)y * heightAdjust;

                // This converts our chunk position to a latitude/longitude,
                // which we can then use to get UV coordinates from the heightmap
                // FIXME: I think this is doing a pincushion effect
                //    Someone smarter than me will have to figure this out.
                Quaternion pointRotation = ChunkRotation *
                    Quaternion.Euler( 
                        xPos * DegreesPerChunk - halfDegreesPerChunk,
                        yPos * DegreesPerChunk - halfDegreesPerChunk,
                        0
                    );

                Vector2 uv = CoordHelper.RotationToUV( pointRotation );

                // Get the pixel from the heightmap image texture at the appropriate position
                Color pix = HeightMapTexture.GetPixelBilinear( uv.x, uv.y );

                // Update the heights array
                heights[x,y] = pix.grayscale / heightMapTextureScaling;
            }
        }

        // Update the terrain data based on our changed heights array
        terrainData.SetHeights(0, 0, heights);

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

        // Caching these dimensions and...
        int w = terrainData.alphamapWidth;
        int h = terrainData.alphamapHeight;

        // Replacing loop divisions with mults cuts this function by about 10%
        //  -- Shout out to Karl Goodloe
        float widthAdjust =  1f / (w-1f);
        float heightAdjust = 1f / (h-1f);
        float angleAdjust =  1f / 90f;

        // Loop through each pixel in the alpha maps
        for (int aX = 0; aX < w; aX++)
        {
            for (int aY = 0; aY < h; aY++)
            {
                // Normal to 0..1
                float x = (float)aX * widthAdjust;
                float y = (float)aY * heightAdjust;

                // Find the steepness of the terrain at this point
                float angle = terrainData.GetSteepness( y, x ); // NOTE: x and y are flipped

                // 0 is "flat ground", 1 is "cliff ground"
                float cliffiness = angle * angleAdjust;

                splatMaps[aX, aY, 0] = 1 - cliffiness; // Ground texture is inverse of cliffiness
                splatMaps[aX, aY, 1] =     cliffiness; // Cliff texture is cliffiness
            }
        }

        // Update the terrain texture maps
        terrainData.SetAlphamaps(0, 0, splatMaps);
    }

    public void SetNeighbors( DynamicTerrainChunk left, DynamicTerrainChunk top, DynamicTerrainChunk right, DynamicTerrainChunk bottom )
    {
        // TODO: Fix the seams between chunks




        Terrain t = GetComponent<Terrain>();

        Terrain leftTerrain = left == null ? null : left.terrain;
        Terrain topTerrain = top == null ? null : top.terrain;
        Terrain rightTerrain = right == null ? null : right.terrain;
        Terrain bottomTerrain = bottom == null ? null : bottom.terrain;

        // Hint to the terrain engine about how to improve LOD calculations
        t.SetNeighbors( leftTerrain, topTerrain, rightTerrain, bottomTerrain );
        t.Flush();
    }

    void BuildStructures()
    {
        // Loop through each pixel of the structures map
        // if we find pixels that aren't transparent (or whatever our criteria is)
        // then we will spawn a structure based on the color code.

        // IDEALLY -- We don't want to have to parse the building map for every chunk.
        // It would be nice instead if we did this once and just cached where all the
        // buildings should be. -- This is very easy.

        Color32[] pixels = StructureMapTexture.GetPixels32();

        Color32 c32 = new Color32(255, 0, 0, 255);

        // Holy crap, it turns out that these .width and .height calls are SUUUUUUUUUPER expensive.
        // I cut my ENTIRE terrain-generation time in half by caching these.
        //  -- quill18
        int w = StructureMapTexture.width;
        int h = StructureMapTexture.height;


        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color32 p = pixels[x + y * w];
                if( p.a < 128 )
                {
                    // transparent pixel, ignore.
                    continue;
                }

                //Debug.Log("Not transparent!: " + p.ToString());

                foreach(StructureColor sc in StructureColors)
                {
                    if(sc.Color.r == p.r && sc.Color.g == p.g && sc.Color.b == p.b)
                    {
                        //Debug.Log("Color match!");
                        // What is the position of the building?
                        SphericalCoord buildingLatLon = CoordHelper.UVToSpherical( new Vector2((float)x/w, (float)y/h) );


                        Vector3 localPosition = SphericalToLocalPosition( buildingLatLon );

                        if(localPosition.x < 0 || localPosition.x > WorldUnitsPerChunk || localPosition.z < 0 || localPosition.z > WorldUnitsPerChunk)
                        {
                            // Not in our chunk!
                            continue;

                        }


                        // Spawn the correct building.
                        Vector3 globalPosition = localPosition + this.transform.position;

                        // Fix the building's height
                        float heightAtGlobalPosition = terrain.SampleHeight( globalPosition );
                        globalPosition.y = heightAtGlobalPosition;

                        // Our rotation is going to be a factor of our longitude and the Z rotation of this chunk
                        // FIXME: Test me -- especially near the poles and with different chunk rotations
                        Quaternion rot = Quaternion.Euler( 0,  ChunkRotation.eulerAngles.z + Mathf.Sin(Mathf.Deg2Rad * buildingLatLon.Latitude)*buildingLatLon.Longitude, 0 );

                        GameObject theStructure = (GameObject)Instantiate(sc.StructurePrefab, globalPosition, rot, this.transform);

                        SmoothTerrainUnderStructure( theStructure );

                        // Stop looping through structure colors
                        break; // foreach
                    }
                }
            }
                
        }
    }

    /// <summary>
    /// Converts a SphericalCoord (Lat/Lon) into a Vector3 that represents
    /// the position of the Lat/Lon on this terrain chunk
    /// </summary>
    /// <param name="sc">Sc.</param>
    Vector3 SphericalToLocalPosition( SphericalCoord sc )
    {
        //Debug.Log( gameObject.name + " -- " + sc.ToString());

        Quaternion buildingQat = CoordHelper.SphericalToRotation(sc);

        float xAngleDiff = buildingQat.eulerAngles.x - ChunkRotation.eulerAngles.x;

        while(xAngleDiff < -360)
            xAngleDiff += 360;
        while(xAngleDiff > 360)
            xAngleDiff -= 360;

        float yAngleDiff = buildingQat.eulerAngles.y - ChunkRotation.eulerAngles.y;

        //Debug.Log( gameObject.name + " -- xAngleDiff: " + xAngleDiff);
        //Debug.Log( gameObject.name + " -- yAngleDiff: " + yAngleDiff);

        Vector3 distFromCenter = new Vector3(
            ((yAngleDiff / DegreesPerChunk)) * WorldUnitsPerChunk,
            0,  // HEIGHT of building
            ((xAngleDiff / DegreesPerChunk)) * WorldUnitsPerChunk
        );

        // Rotate the vector based on chunk's rotation
        // FIXME:   I AM WRONG HERE. MAKE MATH MORE BETTER PLEASE
        // Do we need to incorporate longitude? I think we do.
        //   I think we need to check the different in longitude between the center of the
        //   terrain chunk and where the building is.
        distFromCenter = Quaternion.Euler(0, -ChunkRotation.eulerAngles.z, 0) * distFromCenter;

        // Now move the vector's origin to the bottom-left corner and return that

        return distFromCenter + new Vector3( WorldUnitsPerChunk/2, 0, WorldUnitsPerChunk/2 );
    }

    void SmoothTerrainUnderStructure( GameObject structureGO )
    {

        // We need to figure out the bounds for this structure.


        Collider[] cols = structureGO.GetComponentsInChildren<Collider>();
        if(cols.Length == 0)
        {
            Debug.LogError("SmoothTerrainUnderStructure - Structure has no colliders?");
            return;
        }

        Bounds structureBounds = new Bounds( cols[0].bounds.center, cols[0].bounds.size );

        foreach(Collider col in cols)
        {
            structureBounds.Encapsulate(col.bounds);
        }

        // structureBounds describes the entire volume occupied by this structure (or at least the colliders anyway)

        // Now we need to figure out which points of our heightmap array are under these bounds.

        Vector3 terrainBottomLeftCorner = this.transform.position;

        Vector3 structureBottomLeftCorner = structureBounds.min;

        Vector3 structurePositionOffset = structureBottomLeftCorner - terrainBottomLeftCorner;

        // Our terrain is 1024 world units wide -- but how many array units wide is it?
        // It has a resolution/vertex count of 129 -- but that means it has a 128 edges/squares/whatever
        // So there would be 8 world units per array index
        // Imagine that our structure is at relative position 512, then 512 / 8 = array position 64
        float worldUnitsPerArrayIndexX = terrainData.size.x / (terrainData.heightmapWidth-1);
        float worldUnitsPerArrayIndexY = terrainData.size.z / (terrainData.heightmapHeight-1);

        int minX = Mathf.FloorToInt( structurePositionOffset.x / worldUnitsPerArrayIndexX );
        int maxX = Mathf.CeilToInt ( (structurePositionOffset.x + structureBounds.size.x) / worldUnitsPerArrayIndexX ) + 1;

        // World space Z is array Y
        int minY = Mathf.FloorToInt( structurePositionOffset.z / worldUnitsPerArrayIndexY );
        int maxY = Mathf.CeilToInt ( (structurePositionOffset.z + structureBounds.size.z) / worldUnitsPerArrayIndexY ) + 1;

        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        // Express our height as a value from 0..1
        float correctHeight = (structureGO.transform.position.y - this.transform.position.y) / terrainData.size.y;

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                // TODO: Consider doing a raycast here to see if this spot is ACTUALLY under the building.
                //  Irregular (or highly "diagonal") buildings will currently result in an apparent mis-match
                // between the terrain and the building shape.

                // Invert x/y
                heights[y,x] = correctHeight;
            }
        }

        terrainData.SetHeights(0, 0, heights);

    }
}
