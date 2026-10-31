using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CrashSimulator.UI
{
    public sealed class MainMenuGradientOverlay : MaskableGraphic
    {
        private const int Columns = 24;
        private const int Rows = 12;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            for (var y = 0; y <= Rows; y++)
            {
                float v = y / (float)Rows;
                float bottomAlpha = v < .45f ? Mathf.Lerp(.55f, 0f, v / .45f) : 0f;
                for (var x = 0; x <= Columns; x++)
                {
                    float u = x / (float)Columns;
                    float leftAlpha = u <= .28f ? Mathf.Lerp(1f,.92f,u/.28f)
                        : u <= .56f ? Mathf.Lerp(.92f,.38f,(u-.28f)/.28f)
                        : Mathf.Lerp(.38f,.22f,(u-.56f)/.44f);
                    float alpha = 1f - (1f - leftAlpha) * (1f - bottomAlpha);
                    var vertex = UIVertex.simpleVert;
                    vertex.position = new Vector3(Mathf.Lerp(rect.xMin, rect.xMax, u),
                        Mathf.Lerp(rect.yMin, rect.yMax, v));
                    // First CSS gradient overlays the bottom black gradient.
                    var tint = Color.Lerp(new Color32(9,10,14,255),new Color32(10,10,12,255),Mathf.Clamp01(u/.28f));
                    vertex.color = new Color(tint.r*leftAlpha/alpha,tint.g*leftAlpha/alpha,tint.b*leftAlpha/alpha,alpha);
                    vh.AddVert(vertex);
                }
            }
            int stride = Columns + 1;
            for (var y = 0; y < Rows; y++)
            for (var x = 0; x < Columns; x++)
            {
                int i = y * stride + x;
                vh.AddTriangle(i, i + stride, i + stride + 1);
                vh.AddTriangle(i, i + stride + 1, i + 1);
            }
        }
    }
}
