// SelectionUtils.cs
using UnityEngine;
using System.Collections.Generic;
public static class SelectionUtils
{
    // 屏幕坐标转世界坐标
    public static Vector3 ScreenToWorldPosition(Vector2 screenPos, Camera camera, float worldY = 0f)
    {
        Ray ray = camera.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.up, new Vector3(0, worldY, 0));
        
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        return Vector3.zero;
    }

    // 检查点是否在多边形内
    public static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;

        for (int i = 0; i < polygon.Length; i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / 
                (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    // 获取单位边界框
    public static Rect GetUnitScreenBounds(Unit unit, Camera camera)
    {
        Vector3 screenPos = camera.WorldToScreenPoint(unit.Position);
        float size = 50f; // 单位选择框大小
        
        return new Rect(
            screenPos.x - size / 2,
            screenPos.y - size / 2,
            size,
            size
        );
    }
}