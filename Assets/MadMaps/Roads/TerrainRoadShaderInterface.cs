using MadMaps.Common;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class TerrainRoadShaderInterface : MonoBehaviour
{
    [FormerlySerializedAs("HeightMap")]
    public Texture2D TerrainData;
    public Bounds TerrainBounds;

    [ContextMenu("Capture")]
    public void Capture()
    {
        var terrain = GetComponent<Terrain>();
        TerrainBounds = terrain.GetBounds();
        var hRes = terrain.terrainData.heightmapResolution - 1;
        var heights = terrain.terrainData.GetHeights(0, 0, hRes, hRes);
        if (!TerrainData)
        {
            TerrainData = new Texture2D(hRes, hRes);
        }
        else if (TerrainData.height != hRes || TerrainData.width != hRes)
        {
            TerrainData.Reinitialize(hRes, hRes);
        }
        for (int u = 0; u < hRes + 1; u++)
        {
            var uF = u / (float)(hRes + 1);
            for (int v = 0; v < hRes + 1; v++)
            {
                var vF = v / (float)(hRes + 1);
                var height = heights.BilinearSample(new Vector2(uF, vF));
                TerrainData.SetPixel(u, v, new Color(height, 0, 0, 0));
            }
        }
        TerrainData.Apply();
    }

	// Update is called once per frame
	void Update () 
    {
	    if (TerrainData)
	    {
            Shader.SetGlobalTexture("_TerrainData", TerrainData);
	    }
		
        Shader.SetGlobalVector("_TerrainMin", TerrainBounds.min);
        Shader.SetGlobalVector("_TerrainSize", TerrainBounds.size);
	}
}
