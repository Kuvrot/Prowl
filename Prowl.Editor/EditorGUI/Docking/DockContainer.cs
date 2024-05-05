﻿using Prowl.Runtime;

namespace Prowl.Editor.EditorGUI.Docking
{

    // Based on Alexander Samusev's Hork Engine: https://github.com/Hork-Engine/Hork-Source - MIT Licensed

    public class DockContainer
    {
        public DockNode Root = new DockNode();
        private Vector2 PaddedMins => new Vector2(0, 0);

        public void Update(Rect root)
        {
            Root.UpdateRecursive(root.Min, root.Max);
        }

        public DockNode TraceLeaf(double x, double y)
        {
            x -= PaddedMins.x;
            y -= PaddedMins.y;

            return Root.TraceLeaf(x, y);
        }

        public DockPlacement GetPlacement(double x, double y)
        {
            x -= PaddedMins.x;
            y -= PaddedMins.y;

            DockNode leaf = Root.TraceLeaf(x, y);
            if (leaf == null)
                return default;

            // Translate X, Y to normalized window space 0..1
            x -= leaf.Mins.x;
            y -= leaf.Mins.y;

            double w = leaf.Maxs.x - leaf.Mins.x;
            double h = leaf.Maxs.y - leaf.Mins.y;

            x /= w;
            y /= h;

            double areaWidth = Math.Min(w, h) * 0.3f / w;

            double aspect = w / h;

            double xmin = areaWidth;
            double xmax = 1.0f - xmin;

            double ymin = areaWidth * aspect;
            double ymax = 1.0f - ymin;

            DockPlacement placement = new DockPlacement();
            placement.Leaf = leaf;
            placement.PolygonVerts = new Vector2[4];

            while (true)
            {
                placement.PolygonVerts[0] = new Vector2(0, 0);
                placement.PolygonVerts[1] = new Vector2(xmin, ymin);
                placement.PolygonVerts[2] = new Vector2(xmin, ymax);
                placement.PolygonVerts[3] = new Vector2(0, 1);

                if (PointInPoly(placement.PolygonVerts, 4, x, y))
                {
                    placement.Zone = DockZone.Left;
                    break;
                }

                placement.PolygonVerts[0] = new Vector2(1, 0);
                placement.PolygonVerts[1] = new Vector2(1, 1);
                placement.PolygonVerts[2] = new Vector2(xmax, ymax);
                placement.PolygonVerts[3] = new Vector2(xmax, ymin);

                if (PointInPoly(placement.PolygonVerts, 4, x, y))
                {
                    placement.Zone = DockZone.Right;
                    break;
                }

                placement.PolygonVerts[0] = new Vector2(0, 0);
                placement.PolygonVerts[1] = new Vector2(1, 0);
                placement.PolygonVerts[2] = new Vector2(xmax, ymin);
                placement.PolygonVerts[3] = new Vector2(xmin, ymin);

                if (PointInPoly(placement.PolygonVerts, 4, x, y))
                {
                    placement.Zone = DockZone.Top;
                    break;
                }

                placement.PolygonVerts[0] = new Vector2(xmin, ymax);
                placement.PolygonVerts[1] = new Vector2(xmax, ymax);
                placement.PolygonVerts[2] = new Vector2(1, 1);
                placement.PolygonVerts[3] = new Vector2(0, 1);

                if (PointInPoly(placement.PolygonVerts, 4, x, y))
                {
                    placement.Zone = DockZone.Bottom;
                    break;
                }

                placement.PolygonVerts[0] = new Vector2(0, 0);
                placement.PolygonVerts[1] = new Vector2(1, 0);
                placement.PolygonVerts[2] = new Vector2(1, 1);
                placement.PolygonVerts[3] = new Vector2(0, 1);
                placement.Zone = DockZone.Center;
                break;
            }

            // Convert polygon vertices from normalized coordinates
            for (int i = 0; i < placement.PolygonVerts.Length; i++)
            {
                placement.PolygonVerts[i] *= new Vector2(w, h);
                placement.PolygonVerts[i] += PaddedMins + leaf.Mins;
            }

            return placement;
        }

        static bool PointInPoly(Vector2[] polygon, int vertexCount, double x, double y)
        {
            bool inside = false;
            for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++)
            {
                if (((polygon[i].y > y) != (polygon[j].y > y)) &&
                    (x < (polygon[j].x - polygon[i].x) * (y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        public DockNode AttachWindow(EditorWindow window, DockNode leaf, DockZone zone)
        {
            if (window == null)
                return null;

            if (window.Leaf != null)
            {
                Console.WriteLine("Window already assigned to dock container");
                return null;
            }

            if (leaf == null)
                return null;

            // We can add window only to leafs
            if (leaf.Type != DockNode.NodeType.Leaf)
                return null;

            if (zone == DockZone.Center || leaf.LeafWindows.Count == 0)
            {
                // Just assign new window to leaf
                leaf.LeafWindows.Add(window);
                leaf.WindowNum = leaf.LeafWindows.Count - 1;

                window.Leaf = leaf;

                return leaf;
            }

            DockNode node = leaf;

            node.Child[0] = new DockNode();
            node.Child[1] = new DockNode();

            node.Child[0].Type = DockNode.NodeType.Leaf;
            node.Child[1].Type = DockNode.NodeType.Leaf;

            node.SplitDistance = 0.5f; // Default split distance is 50%

            node.Type = (DockNode.NodeType)(((int)zone >> 1) & 1);

            int child0 = ((int)zone) & 1;
            int child1 = ((int)zone + 1) & 1;

            leaf = node.Child[child0];
            leaf.LeafWindows.Add(window);
            leaf.WindowNum = leaf.LeafWindows.Count - 1;

            window.Leaf = leaf;

            node.Child[child1].LeafWindows = node.LeafWindows.ToList();
            node.Child[child1].WindowNum = node.WindowNum;
            foreach (var w in node.Child[child1].LeafWindows)
                w.Leaf = node.Child[child1];

            return leaf;
        }

        public bool DetachWindow(EditorWindow window)
        {
            if (window == null)
                return false;

            if (window.Leaf == null)
                return false;

            int index = window.Leaf.LeafWindows.IndexOf(window);

            return DetachWindow(window.Leaf, index) != null;
        }

        public EditorWindow DetachWindow(DockNode leaf, int index)
        {
            // Expect leaf node
            if (leaf.Type != DockNode.NodeType.Leaf)
                return null;

            EditorWindow detachedWindow = leaf.LeafWindows[index];
            if (detachedWindow != null)
            {
                detachedWindow.Leaf = null;
            }
            leaf.LeafWindows.RemoveAt(index);
            leaf.WindowNum = Math.Max(0, index - 1);

            if (leaf.LeafWindows.Count == 0)
            {
                DockNode parent = FindParent(leaf);
                if (parent != null)
                {
                    DockNode neighborNode;
                    if (leaf == parent.Child[0])
                        neighborNode = parent.Child[1];
                    else
                        neighborNode = parent.Child[0];

                    parent.Type = neighborNode.Type;
                    parent.LeafWindows = neighborNode.LeafWindows.ToList();
                    parent.WindowNum = neighborNode.WindowNum;
                    parent.SplitDistance = neighborNode.SplitDistance;
                    parent.Child[0] = neighborNode.Child[0];
                    parent.Child[1] = neighborNode.Child[1];

                    foreach (var w in parent.LeafWindows)
                        w.Leaf = parent;
                }
            }

            return detachedWindow;
        }

        public List<EditorWindow> GetWindows()
        {
            List<EditorWindow> windowList = new List<EditorWindow>();
            Root.GetWindows(windowList);
            return windowList;
        }

        public DockNode FindParent(DockNode node)
        {
            if (node == Root)
                return null;

            return Root.FindParent(node);
        }

        public bool AttachWindowAt(EditorWindow window, double x, double y)
        {
            if (window == null)
                return false;

            DockPlacement placement = GetPlacement(x, y);
            if (placement)
                if (AttachWindow(window, placement.Leaf, placement.Zone) != null)
                    return true;

            return false;
        }
    }
}