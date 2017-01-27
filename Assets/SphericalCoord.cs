//-----------------------------------------------------------------------
// <copyright file="SphericalCoord.cs" company="Quill18 Productions">
//     Copyright (c) Quill18 Productions. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphericalCoord 
{
    /// <summary>
    /// Gets or sets the latitude. 0 is north pole, 180 is south pole
    /// </summary>
    /// <value>The latitude.</value>
    public float Latitude { 
        get
        {
            return _Latitude;
        }
        set
        {
            _Latitude = value;
            if(_Latitude >= 270)
            {
                // We are north of the equator
                _Latitude = _Latitude - 270;
            }
            else if(_Latitude >= 0)
            {
                // We are south of the equator
                _Latitude = _Latitude += 90;
            }
        }
    }
    private float _Latitude;

    /// <summary>
    /// Gets or sets the longitude. 0 is left edge, 360 is right edge
    /// </summary>
    /// <value>The longitude.</value>
    public float Longitude { get; set; }


    /// <summary>
    /// Returns a <see cref="System.String"/> that represents the current <see cref="SphericalCoord"/>.
    /// This string will be in the classic Earthican format where 0° latitude is equator
    /// </summary>
    /// <returns>A <see cref="System.String"/> that represents the current <see cref="SphericalCoord"/>.</returns>
    public override string ToString()
    {
        string latString = string.Format("0°");

        if(Latitude < 90)
        {
            // North
            latString = string.Format("{0}° N", (90 - Latitude) );
        }
        else if(Latitude > 90)
        {
            // South
            latString = string.Format("{0}° S", (Latitude - 90) );
        }

        string longString = string.Format("0°");

        if(Longitude <= 0.0001f)
        {
            // Do nothing
        }
        else if(Longitude <= 180)
        {
            longString = string.Format("{0}° E", (Longitude) );
        }
        else if(Longitude > 180)
        {
            longString = string.Format("{0}° W", (180 - (Longitude - 180)) );
        }


        return string.Format("{0}, {1}", latString, longString);
    }
}
