using UnityEngine;
using System.IO;
namespace MMG
{
    public static class GeneralMethods
    {
        public static string[] GetStringArrayFromString(string source)
        {
            int count = source.Length;
            string[] returnedStringArray = new string[count];
            for (int i = 0; i < count; i++)
            {
                returnedStringArray[i] = source.Substring(i, 1);
            }
            return returnedStringArray;
        }
        public static Vector3 GetInverseVector(Vector3 vector)
        {
            return new Vector3(1 / vector.x, 1 / vector.y, 1 / vector.z);
        }
    }
}