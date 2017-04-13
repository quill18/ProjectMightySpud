//-----------------------------------------------------------------------
// <copyright file="UICoordinateDisplay.cs" company="Quill18 Productions">
//     Copyright (c) Quill18 Productions. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class UICoordinateDisplay : MonoBehaviour 
{
    void Start () 
    {
        text = GetComponent<Text>();
    }

    private Text text;

    public Transform TargetObject;

    // TODO: Implement a system whereby "ParentObject" gets set to
    // whatever planet/moon/etc... you are actually closest to.
    public Transform ParentObject;

    void Update () 
    {
        string s = "";

        s += string.Format("Transform: {0}\n", TargetObject.position);

        SphericalCoord sphereCoord = CoordHelper.TransformToSphericalCoord( TargetObject.position, ParentObject.position );
        s += string.Format("Spherical Coordinates: {0}\n", sphereCoord.ToString());

        Vector2 uvCoord = CoordHelper.SphericalToUV( sphereCoord );
        s += string.Format("Texture UV: {0}\n", uvCoord.ToString());

        text.text = s;
    }

}
