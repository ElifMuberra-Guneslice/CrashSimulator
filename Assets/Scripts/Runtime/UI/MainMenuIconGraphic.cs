using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CrashSimulator.UI
{
    public sealed class MainMenuIconGraphic : MaskableGraphic
    {
        [SerializeField] private MenuIconType iconType;
        [SerializeField] private float stroke = 1.6f;

        public void Configure(MenuIconType type, Color tint)
        {
            iconType = type;
            color = tint;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            float scale = Mathf.Min(rect.width, rect.height) * .42f / 10f;
            Vector2 center = rect.center;
            if (iconType == MenuIconType.Sound) DrawSpeaker(vh, center, scale);
            else DrawGear(vh, center, scale);
        }

        private void DrawSpeaker(VertexHelper vh, Vector2 center, float scale)
        {
            Vector2[] horn = { new(-7,-3), new(-4,-3), new(0,-6), new(0,6), new(-4,3), new(-7,3), new(-7,-3) };
            DrawPolyline(vh, center, scale, horn, false);
            DrawArc(vh, center + new Vector2(0, 0), scale, 4.2f, -48f, 48f, false);
            DrawArc(vh, center, scale, 7.2f, -52f, 52f, false);
        }

        private void DrawGear(VertexHelper vh, Vector2 center, float scale)
        {
            const int count = 48;
            var points = new Vector2[count + 1];
            for (int i = 0; i <= count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                int phase = i % 8;
                float radius = phase == 0 || phase == 1 || phase == 4 || phase == 5 ? 9f : 7.3f;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            DrawPolyline(vh, center, scale, points, true);
            DrawArc(vh, center, scale, 3.1f, 0, 360, true);
        }

        private void DrawArc(VertexHelper vh, Vector2 center, float scale, float radius,
            float startDegrees, float endDegrees, bool closed)
        {
            const int steps = 16;
            var points = new Vector2[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(startDegrees, endDegrees, i / (float)steps) * Mathf.Deg2Rad;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            DrawPolyline(vh, center, scale, points, closed);
        }

        private void DrawPolyline(VertexHelper vh, Vector2 center, float scale, Vector2[] points, bool closed)
        {
            int segments = points.Length - 1;
            for (int i = 0; i < segments; i++)
                DrawSegment(vh, center + points[i] * scale, center + points[i + 1] * scale);
            if (closed && segments > 1) DrawSegment(vh, center + points[segments] * scale, center + points[0] * scale);
        }

        private void DrawSegment(VertexHelper vh, Vector2 a, Vector2 b)
        {
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * stroke * .5f;
            int start = vh.currentVertCount;
            AddVertex(vh, a - normal); AddVertex(vh, a + normal);
            AddVertex(vh, b + normal); AddVertex(vh, b - normal);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private void AddVertex(VertexHelper vh, Vector2 position)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vh.AddVert(vertex);
        }
    }
}
