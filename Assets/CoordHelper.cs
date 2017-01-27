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

        coord.Latitude = quatToTarget.eulerAngles.x;
        coord.Longitude = quatToTarget.eulerAngles.y;

        return coord;
    }

    public static Vector2 SphericalToUV( SphericalCoord sphereCoord )
    {
        Vector2 uv = new Vector2(
            1 - (sphereCoord.Longitude / 360f),
            sphereCoord.Latitude / 180f
        );

        return uv;
    }

}
