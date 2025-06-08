// Scripts/RouletteGenerator.cs

using UnityEngine;

public static class RouletteGenerator
{
    /// <summary>
    /// 仕様に基づいたルーレット盤面のメッシュを生成します。
    /// </summary>
    /// <param name="colors">360個のセグメントの色を定義する配列</param>
    /// <param name="radius">盤面の半径</param>
    /// <returns>生成されたメッシュ</returns>
    public static Mesh CreateRouletteMesh(Color[] colors, float radius = 5f)
    {
        if (colors.Length != 360)
        {
            Debug.LogError("Color array must have 360 elements.");
            return null;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Roulette Wheel Mesh";

        // 各三角形は3つの独立した頂点を持つ (共有しない)
        // 360個の三角形 * 3頂点/三角形 = 1080頂点
        Vector3[] vertices = new Vector3[1080];
        int[] triangles = new int[1080];
        Color[] vertexColors = new Color[1080];

        for (int i = 0; i < 360; i++)
        {
            float angleStart = Mathf.Deg2Rad * i;
            float angleEnd = Mathf.Deg2Rad * (i + 1);

            int baseVertexIndex = i * 3;

            // 1. 中心点
            vertices[baseVertexIndex] = Vector3.zero;
            // 2. 開始角度の円周上の点
            vertices[baseVertexIndex + 1] = new Vector3(Mathf.Cos(angleStart) * radius, Mathf.Sin(angleStart) * radius, 0);
            // 3. 終了角度の円周上の点
            vertices[baseVertexIndex + 2] = new Vector3(Mathf.Cos(angleEnd) * radius, Mathf.Sin(angleEnd) * radius, 0);

            // この三角形の3頂点すべてに同じ色を割り当てる
            vertexColors[baseVertexIndex] = colors[i];
            vertexColors[baseVertexIndex + 1] = colors[i];
            vertexColors[baseVertexIndex + 2] = colors[i];
            
            // 三角形のインデックスを割り当てる
            triangles[baseVertexIndex] = baseVertexIndex;
            triangles[baseVertexIndex + 1] = baseVertexIndex + 1;
            triangles[baseVertexIndex + 2] = baseVertexIndex + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = vertexColors;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}