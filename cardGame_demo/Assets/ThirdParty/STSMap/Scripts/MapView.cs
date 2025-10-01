using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Map
{
    public class MapView : MonoBehaviour
    {
        public enum MapOrientation
        {
            BottomToTop,
            TopToBottom,
            RightToLeft,
            LeftToRight
        }

        public MapManager mapManager;
        public MapOrientation orientation;

        [Tooltip(
            "List of all the MapConfig scriptable objects from the Assets folder that might be used to construct maps. " +
            "Similar to Acts in Slay The Spire (define general layout, types of bosses.)")]
        public List<MapConfig> allMapConfigs;
        public GameObject nodePrefab;
        [Tooltip("Offset of the start/end nodes of the map from the edges of the screen")]
        public float orientationOffset;
        [Header("Background Settings")]
        [Tooltip("If the background sprite is null, background will not be shown")]
        public Sprite background;
        public Color32 backgroundColor = Color.white;
        public float xSize;
        public float yOffset;
        [Header("Line Settings")]
        public GameObject linePrefab;
        [Tooltip("Line point count should be > 2 to get smooth color gradients")]
        [Range(3, 10)]
        public int linePointsCount = 10;
        [Tooltip("Distance from the node till the line starting point")]
        public float offsetFromNodes = 0.5f;
        [Header("Colors")]
        [Tooltip("Node Visited or Attainable color")]
        public Color32 visitedColor = Color.white;
        [Tooltip("Locked node color")]
        public Color32 lockedColor = Color.gray;
        [Tooltip("Visited or available path color")]
        public Color32 lineVisitedColor = Color.white;
        [Tooltip("Unavailable path color")]
        public Color32 lineLockedColor = Color.gray;

        protected GameObject firstParent;
        protected GameObject mapParent;
        private List<List<Vector2Int>> paths;
        private Camera cam;
        // ALL nodes:
        public readonly List<MapNode> MapNodes = new List<MapNode>();
        protected readonly List<LineConnection> lineConnections = new List<LineConnection>();

    public static MapView Instance { get; private set; }
        public Map Map { get; protected set; }

        private void Awake()
        {
            Instance = this;
            cam = Camera.main ?? FindFirstObjectByType<Camera>();
            if (cam == null) Debug.LogWarning("[MapView] Awake: Camera bulunamadı, SetOrientation fallback kullanacak.");
        }
        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        protected virtual void ClearMap()
        {
            if (firstParent != null)
                Destroy(firstParent);

            MapNodes.Clear();
            lineConnections.Clear();
        }

        public virtual void ShowMap(Map m)
        {
            if (m == null)
            {
                Debug.LogWarning("Map was null in MapView.ShowMap()");
                return;
            }

            Map = m;

            ClearMap();

            CreateMapParent();

            CreateNodes(m.nodes);

            NormalizeToOrigin(NormalizeMode.StartNode); 

            DrawLines();

            SetOrientation();

            ResetNodesRotation();

            SetAttainableNodes();

            SetLineColors();

            CreateMapBackground(m);
        }

        protected virtual void CreateMapBackground(Map m)
        {
            if (background == null) return;

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(mapParent.transform);
            MapNode bossNode = MapNodes.FirstOrDefault(node => node.Node.nodeType == NodeType.Boss);
            float span = m.DistanceBetweenFirstAndLastLayers();
            backgroundObject.transform.localPosition = new Vector3(bossNode.transform.localPosition.x, span / 2f, 0f);
            backgroundObject.transform.localRotation = Quaternion.identity;
            SpriteRenderer sr = backgroundObject.AddComponent<SpriteRenderer>();
            sr.color = backgroundColor;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.sprite = background;
            sr.size = new Vector2(xSize, span + yOffset * 2f);
        }

        protected virtual void CreateMapParent()
        {
            firstParent = new GameObject("OuterMapParent");
            firstParent.transform.position = Vector3.zero;
            firstParent.transform.rotation = Quaternion.identity;

            mapParent = new GameObject("MapParentWithAScroll");
            mapParent.transform.SetParent(firstParent.transform, worldPositionStays: false);
            mapParent.transform.localPosition = Vector3.zero;
            mapParent.transform.localRotation = Quaternion.identity;

            var scrollNonUi = mapParent.AddComponent<ScrollNonUI>();
            scrollNonUi.freezeX = orientation == MapOrientation.BottomToTop || orientation == MapOrientation.TopToBottom;
            scrollNonUi.freezeY = orientation == MapOrientation.LeftToRight || orientation == MapOrientation.RightToLeft;

            var boxCollider = mapParent.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(100, 100, 1);
        }

        protected void CreateNodes(IEnumerable<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                MapNode mapNode = CreateMapNode(node);
                MapNodes.Add(mapNode);
            }
        }

        protected virtual MapNode CreateMapNode(Node node)
        {
            GameObject mapNodeObject = Instantiate(nodePrefab, mapParent.transform);
            MapNode mapNode = mapNodeObject.GetComponent<MapNode>();
            NodeBlueprint blueprint = GetBlueprint(node.blueprintName);
            mapNode.SetUp(node, blueprint, this);
            mapNode.transform.localPosition = node.position;
            return mapNode;
        }

        public void SetAttainableNodes()
        {
            // first set all the nodes as unattainable/locked:
            foreach (MapNode node in MapNodes)
                node.SetState(NodeStates.Locked);

            if (mapManager.CurrentMap.path.Count == 0)
            {
                // we have not started traveling on this map yet, set entire first layer as attainable:
                foreach (MapNode node in MapNodes.Where(n => n.Node.point.y == 0))
                    node.SetState(NodeStates.Attainable);
            }
            else
            {
                // we have already started moving on this map, first highlight the path as visited:
                foreach (Vector2Int point in mapManager.CurrentMap.path)
                {
                    MapNode mapNode = GetNode(point);
                    if (mapNode != null)
                        mapNode.SetState(NodeStates.Visited);
                }

                Vector2Int currentPoint = mapManager.CurrentMap.path[mapManager.CurrentMap.path.Count - 1];
                Node currentNode = mapManager.CurrentMap.GetNode(currentPoint);

                // set all the nodes that we can travel to as attainable:
                foreach (Vector2Int point in currentNode.outgoing)
                {
                    MapNode mapNode = GetNode(point);
                    if (mapNode != null)
                        mapNode.SetState(NodeStates.Attainable);
                }
            }
        }

        public virtual void SetLineColors()
        {
            // set all lines to grayed out first:
            foreach (LineConnection connection in lineConnections)
                connection.SetColor(lineLockedColor);

            // set all lines that are a part of the path to visited color:
            // if we have not started moving on the map yet, leave everything as is:
            if (mapManager.CurrentMap.path.Count == 0)
                return;

            // in any case, we mark outgoing connections from the final node with visible/attainable color:
            Vector2Int currentPoint = mapManager.CurrentMap.path[mapManager.CurrentMap.path.Count - 1];
            Node currentNode = mapManager.CurrentMap.GetNode(currentPoint);

            foreach (Vector2Int point in currentNode.outgoing)
            {
                LineConnection lineConnection = lineConnections.FirstOrDefault(conn => conn.from.Node == currentNode &&
                                                                            conn.to.Node.point.Equals(point));
                lineConnection?.SetColor(lineVisitedColor);
            }

            if (mapManager.CurrentMap.path.Count <= 1) return;

            for (int i = 0; i < mapManager.CurrentMap.path.Count - 1; i++)
            {
                Vector2Int current = mapManager.CurrentMap.path[i];
                Vector2Int next = mapManager.CurrentMap.path[i + 1];
                LineConnection lineConnection = lineConnections.FirstOrDefault(conn => conn.@from.Node.point.Equals(current) &&
                                                                            conn.to.Node.point.Equals(next));
                lineConnection?.SetColor(lineVisitedColor);
            }
        }
    protected  void SetOrientation()
    {
        var currentMap = Map != null ? Map : mapManager != null ? mapManager.CurrentMap : null;
        if (currentMap == null)
        {
            Debug.LogError("[MapView] SetOrientation: Map null");
            return;
        }

        if (cam == null)
        {
            cam = Camera.main ?? FindFirstObjectByType<Camera>();
        }

        var scrollNonUi = mapParent != null ? mapParent.GetComponent<ScrollNonUI>() : null;

        float span = currentMap.DistanceBetweenFirstAndLastLayers();

        // bossNode Y'sini **LOCAL** al
        MapNode bossNode = MapNodes.FirstOrDefault(n => n.Node.nodeType == NodeType.Boss);
        float bossYLocal;
        if (bossNode != null) bossYLocal = bossNode.transform.localPosition.y;
        else
        {
            var lastRow = MapNodes.OrderByDescending(n => n.Node.point.y).FirstOrDefault();
            bossYLocal = lastRow != null ? lastRow.transform.localPosition.y : 0f;
            Debug.LogWarning("[MapView] Boss bulunamadı, son katman referans alındı.");
        }

        // 1) firstParent'ı kameranın önüne **WORLD** olarak koy
        if (cam != null)
            firstParent.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        else
            firstParent.transform.position = Vector3.zero;

        // 2) mapParent rotasyonunu oryantasyona göre ayarla (her seferinde mutlak)
        switch (orientation)
        {
            case MapOrientation.BottomToTop:
                mapParent.transform.localRotation = Quaternion.identity;
                break;
            case MapOrientation.TopToBottom:
                mapParent.transform.localRotation = Quaternion.Euler(0, 0, 180);
                break;
            case MapOrientation.RightToLeft:
                mapParent.transform.localRotation = Quaternion.Euler(0, 0, 90);
                break;
            case MapOrientation.LeftToRight:
                mapParent.transform.localRotation = Quaternion.Euler(0, 0, -90);
                break;
        }

        // 3) Scroll sınırları (mutlak)
        float offset = orientationOffset * (orientation == MapOrientation.LeftToRight || orientation == MapOrientation.RightToLeft
            ? (cam != null ? cam.aspect : 1f)
            : 1f);

        if (scrollNonUi != null)
        {
            switch (orientation)
            {
                case MapOrientation.BottomToTop:
                    scrollNonUi.yConstraints.max = 0;
                    scrollNonUi.yConstraints.min = -(span + 2f * offset);
                    break;
                case MapOrientation.TopToBottom:
                    scrollNonUi.yConstraints.min = 0;
                    scrollNonUi.yConstraints.max =  span + 2f * offset;
                    break;
                case MapOrientation.RightToLeft:
                    scrollNonUi.xConstraints.max =  span + 2f * offset;
                    scrollNonUi.xConstraints.min = 0;
                    break;
                case MapOrientation.LeftToRight:
                    scrollNonUi.xConstraints.max = 0;
                    scrollNonUi.xConstraints.min = -(span + 2f * offset);
                    break;
            }
        }

        // 4) **LOCAL** offset'i tek seferde ata (+= değil!)
        Vector3 local = Vector3.zero;
        switch (orientation)
        {
            case MapOrientation.BottomToTop:
                local = new Vector3(0,  offset, 0);
                break;
            case MapOrientation.TopToBottom:
                local = new Vector3(0, -offset, 0);
                break;
            case MapOrientation.RightToLeft:
                // önceki kod world bossY kullanıyordu, burada **local** kullanıyoruz
                local = new Vector3(-offset, -bossYLocal, 0);
                break;
            case MapOrientation.LeftToRight:
                local = new Vector3( offset,  bossYLocal, 0);
                break;
        }
        firstParent.transform.localPosition = local;

        Debug.Log($"[MapView] SetOrientation span={span} bossYLocal={bossYLocal} camAspect={(cam?cam.aspect:0f)} local={local}");
    }

        private void DrawLines()
        {
            foreach (MapNode node in MapNodes)
            {
                foreach (Vector2Int connection in node.Node.outgoing)
                    AddLineConnection(node, GetNode(connection));
            }
        }

        private void ResetNodesRotation()
        {
            foreach (MapNode node in MapNodes)
                node.transform.rotation = Quaternion.identity;
        }
    private enum NormalizeMode { StartNode, Center }

    private void NormalizeToOrigin(NormalizeMode mode)
    {
        if (MapNodes.Count == 0 || mapParent == null) return;

        Vector3 delta = Vector3.zero;

        if (mode == NormalizeMode.StartNode)
        {
            // row==0’daki en soldaki node'u referans al
            var start = MapNodes
                .Where(n => n.Node.point.y == 0)
                .OrderBy(n => n.Node.point.x)
                .FirstOrDefault();

            if (start != null)
                delta = -start.transform.localPosition;
        }
        else // Center
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var n in MapNodes)
            {
                var p = (Vector2)n.transform.localPosition;
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            var center = (min + max) * 0.5f;
            delta = -(Vector3)center;
        }

        // parent'ı kaydırmak yeterli (nodelara tek tek dokunma)
        mapParent.transform.localPosition += delta;

        // Teşhis
        var sample = MapNodes[0].transform.localPosition;
        Debug.Log($"[MapView] NormalizeToOrigin mode={mode} delta={delta} sampleAfter={sample}");
    }

        protected virtual void AddLineConnection(MapNode from, MapNode to)
        {
            if (linePrefab == null) return;

            GameObject lineObject = Instantiate(linePrefab, mapParent.transform);
            LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
            Vector3 fromPoint = from.transform.position +
                                (to.transform.position - from.transform.position).normalized * offsetFromNodes;

            Vector3 toPoint = to.transform.position +
                              (from.transform.position - to.transform.position).normalized * offsetFromNodes;

            // drawing lines in local space:
            lineObject.transform.position = fromPoint;
            lineRenderer.useWorldSpace = false;

            // line renderer with 2 points only does not handle transparency properly:
            lineRenderer.positionCount = linePointsCount;
            for (int i = 0; i < linePointsCount; i++)
            {
                lineRenderer.SetPosition(i,
                    Vector3.Lerp(Vector3.zero, toPoint - fromPoint, (float)i / (linePointsCount - 1)));
            }

            DottedLineRenderer dottedLine = lineObject.GetComponent<DottedLineRenderer>();
            if (dottedLine != null) dottedLine.ScaleMaterial();

            lineConnections.Add(new LineConnection(lineRenderer, null, from, to));
        }

        protected MapNode GetNode(Vector2Int p)
        {
            return MapNodes.FirstOrDefault(n => n.Node.point.Equals(p));
        }

        protected MapConfig GetConfig(string configName)
        {
            return allMapConfigs.FirstOrDefault(c => c.name == configName);
        }

        protected NodeBlueprint GetBlueprint(NodeType type)
        {
            MapConfig config = GetConfig(mapManager.CurrentMap.configName);
            return config.nodeBlueprints.FirstOrDefault(n => n.nodeType == type);
        }

        protected NodeBlueprint GetBlueprint(string blueprintName)
        {
            MapConfig config = GetConfig(mapManager.CurrentMap.configName);
            return config.nodeBlueprints.FirstOrDefault(n => n.name == blueprintName);
        }
    }
}
