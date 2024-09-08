using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ConstraintComponent : MonoBehaviour
{
    public int id;
    public int body1ID;
    public int body2ID;
    public string type;
    public int space;
    public Transform point1;
    public Transform point2;
}