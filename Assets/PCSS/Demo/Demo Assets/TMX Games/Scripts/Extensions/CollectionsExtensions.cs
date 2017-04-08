using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CollectionsExtensions
{
    public static void Shuffle (this IList list)
    //public static void Shuffle<T>(this IList<T> list)
    {
        if (list.Count == 0)
            return;

        int r;
        var tmp = list[0];
        for (int i = list.Count - 1; i > 0; i--)
        {
            r = Random.Range(0, i);
            tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }
}
