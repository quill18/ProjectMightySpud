//-----------------------------------------------------------------------
// <copyright file="CoordHelper.cs" company="Quill18 Productions">
//     Copyright (c) Quill18 Productions. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CoordHelper 
{

    public static SphericalCoord TransformToSphericalCoord( Vector3 targetPos, Vector3 parentPos )
    {
        Vector3 dirToTarget = targetPos - parentPos;

        Quaternion quatToTarget = Quaternion.LookRotation( dirToTarget );

        SphericalCoord coord = new SphericalCoord();

        float lat = quatToTarget.eulerAngles.x;

        float lon = quatToTarget.eulerAngles.y;

        coord.Latitude = lat;
        coord.Longitude = lon;

        return coord;
    }

<<<<<<< HEAD
    public static Quaternion SphericalToRotation( SphericalCoord sphereCoord)
    {
        return Quaternion.Euler( sphereCoord.Latitude, sphereCoord.Longitude, 0 );
    }

    public static SphericalCoord RotationToSpherical( Quaternion rotation )
    {
        return new SphericalCoord( rotation.eulerAngles.x, rotation.eulerAngles.y );
    }

    public static Vector2 RotationToUV( Quaternion rotation )
    {
        return SphericalToUV( RotationToSpherical(rotation) );
    }

    public static Vector2 SphericalToUV( SphericalCoord sphereCoord )
    {
        Vector2 uv = new Vector2(
            (sphereCoord.Longitude / 360f),
            (sphereCoord.Latitude + 90) / 180f
        );

        return uv;
    }

}
