using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoordinateManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    // TODO: Implement a system whereby "ThePlanetoid" gets set to
    // whatever planet/moon/etc... you are actually closest to.
    public GameObject ThePlanetoid;
    public GameObject TheTerrain;
    public Transform TheShip;

    bool isTerrain = false;

    public void SwitchToTerrain()
    {
        if(isTerrain)
        {
            Debug.LogError("Already in terrain mode.");
            return;
        }

        // We are in space mode, but have to switch to terrain mode.
        isTerrain = true;

        // What are the coordinates of the ship relative to the planetoid
        SphericalCoord sphereCoord = CoordHelper.TransformToSphericalCoord( TheShip.position, ThePlanetoid.transform.position );
        string s = string.Format("Landing ship at coordinates: {0}\n", sphereCoord.ToString());
        Debug.Log(s);

        ThePlanetoid.SetActive(false);
        TheTerrain.SetActive(true);

        // NOTE: This will freeze the game for a few seconds depending on processor speed.
        // Consider solutions like CoRoutines or Threading.
        TheTerrain.GetComponent<DynamicTerrainMaster>().BuildFromLandingSpot( sphereCoord );

        // Now that the terrain exists, move the ship to be in the same reference system.
        // Rotate the ship -90 around the X axis
        TheShip.transform.RotateAround( Vector3.zero, Vector3.right, -90 );

        Vector3 pos = TheShip.transform.position;
        float planetRadius = ThePlanetoid.transform.localScale.x / 2f; // Pick any one axis of the scale

        // Subtract the radius of the planetoid
        pos = pos.normalized * (pos.magnitude - planetRadius);

        // In space, 1 unit = 1km. On ground, 1 unit = 1m. So mult scales/distance by 1000.
        TheShip.transform.position = pos * 1000f;
        Vector3 scale = TheShip.transform.localScale * 1000f;
        TheShip.transform.localScale = scale;

    }

    public void SwitchToSpace()
    {
        if(isTerrain == false)
        {
            Debug.LogError("Already in space mode.");
            return;
        }

        // We are in terrain mode, but have to switch to space mode.
        isTerrain = false;


        // What are the coordinates of the ship relative to the planetoid
        SphericalCoord sphereCoord = TheTerrain.GetComponent<DynamicTerrainMaster>().WorldToSpherical( TheShip.transform.position );
        string s = string.Format("Taking off from coordinates: {0}\n", sphereCoord.ToString());
        Debug.Log(s);

        TheTerrain.GetComponent<DynamicTerrainMaster>().DestroyChunks();

        ThePlanetoid.SetActive(true);
        TheTerrain.SetActive(false);


        // Now that the terrain exists, move the ship to be in the same reference system.
        // Rotate the ship -90 around the X axis
        TheShip.transform.RotateAround( Vector3.zero, Vector3.right, 90 );

        Vector3 pos = TheShip.transform.position;
        float planetRadius = ThePlanetoid.transform.localScale.x / 2f; // Pick any one axis of the scale

        // In space, 1 unit = 1km. On ground, 1 unit = 1m. So DIVIDE scales/distance by 1000.
        pos /= 1000f;
        // Re-add the radius of the planetoid
        pos = pos.normalized * (pos.magnitude + planetRadius);
        TheShip.transform.position = pos;

        Vector3 scale = TheShip.transform.localScale / 1000f;
        TheShip.transform.localScale = scale;


    }
}
