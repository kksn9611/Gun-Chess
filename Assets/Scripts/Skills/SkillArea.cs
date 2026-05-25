using UnityEngine;
public enum AreaShapeType
{
    Circle,
    Cone,
    Laser
}

[System.Serializable]
public struct AreaShapeData
{
    public AreaShapeType shapeType;

    [Header("Circle Setting")]
    public float radius; // Circle

    [Header("Cone Setting")]
    public float range;  // Cone range
    public float angle;  // Cone angle (degrees)

    [Header("Laser Setting")]
    public float width;
    public float length;
}