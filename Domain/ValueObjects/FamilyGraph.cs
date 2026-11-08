using ACS_View.Domain.Entities;
namespace ACS_View.Domain.ValueObjects;

public sealed record FamilyGraphNode(int PatientId, string Name, string Detail, float X, float Y, int Generation);
public sealed record FamilyGraphEdge(int From, int To, string Label, string Nature, string Category, bool IsActive, int Generation, bool MissingIntermediate = false, string ReverseLabel = "");
public sealed record FamilyGraphPoint(float X, float Y);
public sealed record FamilyGraphStroke(FamilyGraphPoint Start, FamilyGraphPoint End, string Nature, string Category, bool IsActive, int[] People, bool Arrow = false);
public sealed record FamilyGraphAnnotation(float X, float Y, string Text, int[] People, bool AlwaysVisible = false);
public sealed record FamilyGraphGroup(int[] People, float X, float Y, float Width, string Label);
public sealed record FamilyGraph(IReadOnlyList<FamilyGraphNode> Nodes, IReadOnlyList<FamilyGraphEdge> Edges, bool Truncated)
{
    public IReadOnlyList<FamilyGraphStroke> Strokes { get; init; } = [];
    public IReadOnlyList<FamilyGraphAnnotation> Annotations { get; init; } = [];
    public IReadOnlyList<FamilyGraphGroup> Groups { get; init; } = [];
    public int SimplifiedEdges { get; init; }
}
public sealed record FamilyGraphOptions(bool Network = false, int Ancestors = 3, int Descendants = 2, int NetworkDepth = 2, string Nature = "Todas", string Category = "Todas", int MaxNodes = 1000);

public static class FamilyGraphBuilder
{
    private const float CardSpacing = 230, UnitGap = 90;
    private sealed class Unit(int id, int rank, int[] people)
    {
        public int Id = id, Rank = rank;
        public int[] People = people;
        public float X;
        public float Width => People.Length * CardSpacing;
        public string SiblingKey = "";
    }
    private sealed class Sets(IEnumerable<int> ids)
    {
        private readonly Dictionary<int, int> roots = ids.ToDictionary(x => x, x => x);
        public int Find(int id)
        {
            var root = id;
            while (roots[root] != root) root = roots[root];
            while (roots[id] != id) { var next = roots[id]; roots[id] = root; id = next; }
            return root;
        }
        public int Size(int id) => roots.Keys.Count(k => Find(k) == Find(id));
        public bool Join(int a, int b)
        {
            a = Find(a); b = Find(b); if (a == b) return false;
            roots[Math.Max(a, b)] = Math.Min(a, b); return true;
        }
    }
    private sealed record Bundle(int[] Parents, int[] Children, string Nature, bool Active, int Generation, bool Missing);

    public static FamilyGraph Build(KinshipSnapshot s, int center, FamilyGraphOptions options)
    {
        if (!s.People.ContainsKey(center)) throw new InvalidOperationException("Pessoa indisponível no perfil atual.");
        var links = s.Relationships.Where(r => r.FromPatientId != r.ToPatientId && s.People.ContainsKey(r.FromPatientId) && s.People.ContainsKey(r.ToPatientId)
            && (options.Nature == "Todas" || r.TypeCode != "parent" || r.Nature == options.Nature)
            && (options.Category == "Todas" || KinshipCatalog.Get(r.TypeCode).Category == options.Category)
            && (options.Network || KinshipCatalog.Get(r.TypeCode).Generation > 0 || r.TypeCode == "sibling" || KinshipCatalog.Get(r.TypeCode).Category == "Conjugal"))
            .OrderBy(r => KinshipCatalog.Get(r.TypeCode).Generation > 0 ? 0 : KinshipCatalog.Get(r.TypeCode).Category == "Conjugal" ? 1 : 2)
            .ThenByDescending(r => r.IsActive).ThenBy(r => r.FromPatientId).ThenBy(r => r.ToPatientId).ThenBy(r => r.TypeCode).ThenBy(r => r.Id).ToArray();
        var adjacency = links.SelectMany(r => new[] { (Id: r.FromPatientId, R: r), (Id: r.ToPatientId, R: r) }).ToLookup(x => x.Id, x => x.R);
        var levels = new Dictionary<int, int> { [center] = 0 };
        var distances = new Dictionary<int, int> { [center] = 0 };
        var queue = new Queue<int>(); queue.Enqueue(center); bool truncated = false;
        while (queue.TryDequeue(out var id))
        {
            foreach (var r in adjacency[id])
            {
                var other = r.FromPatientId == id ? r.ToPatientId : r.FromPatientId;
                if (levels.ContainsKey(other)) continue;
                var generation = KinshipCatalog.Get(r.TypeCode).Generation;
                int level = levels[id] + (r.FromPatientId == id ? generation : -generation), distance = distances[id] + 1;
                if (options.Network ? distance > Math.Clamp(options.NetworkDepth, 1, 12)
                    : level < -Math.Clamp(options.Ancestors, 0, 12) || level > Math.Clamp(options.Descendants, 0, 12) || distance > options.Ancestors + options.Descendants + 2) continue;
                if (levels.Count >= Math.Clamp(options.MaxNodes, 1, 5000)) { truncated = true; continue; }
                levels[other] = level; distances[other] = distance; queue.Enqueue(other);
            }
        }
        var visible = links.Where(r => levels.ContainsKey(r.FromPatientId) && levels.ContainsKey(r.ToPatientId)).ToArray();
        var filteredSnapshot = s with { Relationships = links };
        var queries = new Dictionary<int, IReadOnlyList<KnownRelative>>();
        bool Demonstrated(FamilyRelationship r)
        {
            if (r.TypeCode is not ("grandparent" or "greatgrandparent" or "sibling")) return false;
            if (!queries.TryGetValue(r.ToPatientId, out var relatives)) queries[r.ToPatientId] = relatives = KinshipEngine.Query(filteredSnapshot, r.ToPatientId);
            return relatives.Any(k => k.PatientId == r.FromPatientId && k.TypeCode == r.TypeCode && k.IsDerived);
        }
        FamilyGraphEdge Edge(FamilyRelationship r) => new(r.FromPatientId, r.ToPatientId, r.ForwardLabel, r.Nature,
            KinshipCatalog.Get(r.TypeCode).Category, r.IsActive, KinshipCatalog.Get(r.TypeCode).Generation,
            KinshipCatalog.Get(r.TypeCode).Generation > 1 && !Demonstrated(r), r.ReverseLabel);
        var edges = visible.Select(Edge).ToArray();
        var drawingLinks = options.Network ? visible.ToList() : visible.Where(r => !Demonstrated(r)).ToList();
        if (!options.Network)
        {
            // One union connector per pair; all episodes remain in Edges and in the selected person's summary.
            drawingLinks = drawingLinks.Where(r => KinshipCatalog.Get(r.TypeCode).Category != "Conjugal")
                .Concat(drawingLinks.Where(r => KinshipCatalog.Get(r.TypeCode).Category == "Conjugal")
                    .GroupBy(r => (Math.Min(r.FromPatientId, r.ToPatientId), Math.Max(r.FromPatientId, r.ToPatientId)))
                    .Select(g => g.OrderByDescending(r => r.IsActive).ThenByDescending(r => r.StartDate).First())).ToList();
            var forest = new Sets(levels.Keys);
            foreach (var r in drawingLinks.Where(r => r.TypeCode != "sibling")) forest.Join(r.FromPatientId, r.ToPatientId);
            drawingLinks.RemoveAll(r => r.TypeCode == "sibling" && !forest.Join(r.FromPatientId, r.ToPatientId));
        }

        // A unit groups known partners or confirmed co-parents for layout only. It never creates a union or parent.
        var sets = new Sets(levels.Keys);
        foreach (var r in visible.Where(r => KinshipCatalog.Get(r.TypeCode).Category == "Conjugal"))
            if ((levels[r.FromPatientId] == levels[r.ToPatientId]) && (sets.Find(r.FromPatientId)==sets.Find(r.ToPatientId)||sets.Size(r.FromPatientId)+sets.Size(r.ToPatientId)<=8)) sets.Join(r.FromPatientId, r.ToPatientId);
        foreach (var family in visible.Where(r => r.TypeCode == "parent" && r.IsActive).GroupBy(r => r.ToPatientId))
        {
            var parents = family.Select(r => r.FromPatientId).Distinct().Order().ToArray();
            for (int i = 1; i < parents.Length; i++)
                if ((levels[parents[0]] == levels[parents[i]]) && (sets.Find(parents[0])==sets.Find(parents[i])||sets.Size(parents[0])+sets.Size(parents[i])<=8)) sets.Join(parents[0], parents[i]);
        }
        var partners = visible.Where(r => KinshipCatalog.Get(r.TypeCode).Category == "Conjugal").OrderByDescending(r => r.IsActive).ToArray();
        int[] OrderPartners(IEnumerable<int> ids)
        {
            var remaining = ids.Order().ToHashSet(); var ordered = new List<int>();
            int current = remaining.Contains(center) ? center : remaining.Min();
            while (remaining.Count > 0)
            {
                ordered.Add(current); remaining.Remove(current);
                var next = partners.Where(r => r.FromPatientId == current || r.ToPatientId == current)
                    .Select(r => r.FromPatientId == current ? r.ToPatientId : r.FromPatientId).FirstOrDefault(remaining.Contains);
                current = next != 0 ? next : remaining.DefaultIfEmpty().Min();
            }
            return ordered.ToArray();
        }
        var parentMap = visible.Where(r => r.TypeCode == "parent" && r.IsActive).ToLookup(r => r.ToPatientId, r => r.FromPatientId);
        var units = levels.Keys.GroupBy(sets.Find).Select(g => new Unit(g.Key, levels[g.First()], OrderPartners(g))
        { SiblingKey = string.Join(",", g.SelectMany(id => parentMap[id]).Distinct().Order().Select(id => sets.Find(id)).Distinct()) }).ToArray();
        var byUnit = units.ToDictionary(u => u.Id);
        var layoutLinks=visible.Where(r=>!Demonstrated(r)).ToArray();
        var unitNeighbors = layoutLinks.SelectMany(r => new[] { (From: sets.Find(r.FromPatientId), To: sets.Find(r.ToPatientId)), (From: sets.Find(r.ToPatientId), To: sets.Find(r.FromPatientId)) })
            .Where(p => p.From != p.To).Distinct().ToLookup(p => p.From, p => p.To);
        var layers = units.GroupBy(u => u.Rank).OrderBy(g => g.Key).ToDictionary(g => g.Key, g => g.OrderBy(u => u.SiblingKey).ThenBy(u => u.Id).ToList());
        void PlaceLayer(List<Unit> layer)
        {
            var width = layer.Sum(u => u.Width) + Math.Max(0, layer.Count - 1) * UnitGap; var x = -width / 2;
            foreach (var u in layer) { u.X = x + u.Width / 2; x += u.Width + UnitGap; }
        }
        foreach (var layer in layers.Values) PlaceLayer(layer);
        // Sweep complete sibling blocks and align parents above their children's subtrees.
        // Isotonic compaction preserves the desired centers without overlapping family units.
        void AlignLayer(List<Unit> layer, Func<Unit,float> target)
        {
            var offsets = new float[layer.Count];
            for (int i = 1; i < layer.Count; i++) offsets[i] = offsets[i - 1] + (layer[i - 1].Width + layer[i].Width) / 2 + UnitGap;
            var pools = new List<(int Start,int End,float Sum,int Count)>();
            for (int i = 0; i < layer.Count; i++)
            {
                pools.Add((i,i,target(layer[i])-offsets[i],1));
                while (pools.Count > 1 && pools[^2].Sum / pools[^2].Count > pools[^1].Sum / pools[^1].Count)
                {
                    var right=pools[^1];pools.RemoveAt(pools.Count-1);var left=pools[^1];pools.RemoveAt(pools.Count-1);
                    pools.Add((left.Start,right.End,left.Sum+right.Sum,left.Count+right.Count));
                }
            }
            foreach (var pool in pools) for (int i=pool.Start;i<=pool.End;i++) layer[i].X=pool.Sum/pool.Count+offsets[i];
        }
        for (int sweep = 0; sweep < 8; sweep++)
            foreach (var rank in (sweep % 2 == 0 ? layers.Keys.Order() : layers.Keys.OrderDescending()))
            {
                float Barycenter(Unit u)
                {
                    var neighbors=unitNeighbors[u.Id].Select(id=>byUnit[id]).Where(n=>sweep%2==0?n.Rank<u.Rank:n.Rank>u.Rank).ToArray();
                    return neighbors.Length==0?u.X:neighbors.Average(n=>n.X);
                }
                var blocks = layers[rank].GroupBy(u => u.SiblingKey == "" ? "unit:" + u.Id : "parents:" + u.SiblingKey)
                    .OrderBy(g => g.Average(Barycenter)).ThenBy(g => g.Min(u => u.Id));
                layers[rank] = blocks.SelectMany(g => g.OrderBy(Barycenter).ThenBy(u => u.Id)).ToList();
                AlignLayer(layers[rank],Barycenter);
            }
        var positioned = new Dictionary<int, (float X, int Row)>(); var rows = new List<List<Unit>>();
        // Keep the same genealogical backbone in both modes. Wrapping generations into a
        // narrow column forces long links through unrelated families and destroys lineage.
        foreach (var layer in layers.Values) rows.Add(layer);
        for (int row = 0; row < rows.Count; row++)
        {
            foreach (var u in rows[row]) for (int i = 0; i < u.People.Length; i++) positioned[u.People[i]] = (u.X + (i - (u.People.Length - 1) / 2f) * CardSpacing, row);
        }
        var drawEdges = drawingLinks.Select(Edge).ToArray();
        var bundles = drawEdges.Where(e => e.Generation > 0).GroupBy(e => new { e.To, e.Nature, e.IsActive, e.Generation, e.MissingIntermediate })
            .Select(g => new Bundle(g.Select(e => e.From).Distinct().Order().ToArray(), [g.Key.To], g.Key.Nature, g.Key.IsActive, g.Key.Generation, g.Key.MissingIntermediate))
            .GroupBy(b => (Parents: string.Join(",", b.Parents), Row: positioned[b.Children[0]].Row, b.Nature, b.Active, b.Generation, b.Missing))
            .Select(g => g.First() with { Children = g.SelectMany(b => b.Children).Distinct().OrderBy(id => positioned[id].X).ToArray() }).ToArray();
        // Distinct parental natures/sets enter different ports on the same child card,
        // so biological and adoptive paths remain distinguishable up to the endpoint.
        var childPorts=bundles.SelectMany(b=>b.Children.Select(child=>(Bundle:b,Child:child)))
            .GroupBy(p=>p.Child).SelectMany(g=>g.Select((p,index)=>(p.Bundle,p.Child,Offset:(index-(g.Count()-1)/2f)*18)))
            .ToDictionary(p=>(p.Bundle,p.Child),p=>p.Offset);
        float ChildX(Bundle b,int child)=>positioned[child].X+childPorts[(b,child)];
        bool CanBundle(Bundle b) => b.Parents.Select(id => positioned[id].Row).Distinct().Count() == 1
            && Math.Abs(positioned[b.Parents[0]].Row - positioned[b.Children[0]].Row) == 1;
        var bundledPairs = bundles.Where(CanBundle).SelectMany(b => b.Parents.SelectMany(p => b.Children.Select(c => (p, c, b.Nature, b.Active, b.Generation)))).ToHashSet();
        var remainingEdges = drawEdges.Where(e => !bundledPairs.Contains((e.From, e.To, e.Nature, e.IsActive, e.Generation))).ToArray();
        // Non-overlapping family branches reuse a corridor lane; only crossing spans need
        // another lane. A large row of independent couples must not create a huge vertical gap.
        var planned = new Dictionary<(int Row,int Side),List<List<(float Min,float Max)>>>();
        static int Reserve(Dictionary<(int Row,int Side),List<List<(float Min,float Max)>>> lanes,(int Row,int Side) key,float min,float max)
        {
            if(!lanes.TryGetValue(key,out var tracks))lanes[key]=tracks=[];
            int lane=tracks.FindIndex(track=>track.All(span=>max+12<span.Min||min-12>span.Max));
            if(lane<0){lane=tracks.Count;tracks.Add([]);}tracks[lane].Add((min,max));return lane;
        }
        foreach(var b in bundles.Where(CanBundle))
        {
            int from=b.Parents[0],to=b.Children[0],sign=Math.Sign(positioned[to].Row-positioned[from].Row);
            var originX=b.Parents.Average(id=>positioned[id].X);
            Reserve(planned,(positioned[from].Row,sign),b.Parents.Min(id=>positioned[id].X),b.Parents.Max(id=>positioned[id].X));
            Reserve(planned,(positioned[to].Row,-sign),Math.Min(originX,b.Children.Min(id=>ChildX(b,id))),Math.Max(originX,b.Children.Max(id=>ChildX(b,id))));
        }
        var plannedUnions=new HashSet<(int,int)>();
        foreach(var e in remainingEdges.OrderBy(e=>e.Category=="Conjugal"?0:1).ThenBy(e=>Math.Abs(positioned[e.From].X-positioned[e.To].X)))
        {
            var a=positioned[e.From];var b=positioned[e.To];
            var adjacent=a.Row==b.Row&&!positioned.Any(n=>n.Key!=e.From&&n.Key!=e.To&&n.Value.Row==a.Row&&n.Value.X>Math.Min(a.X,b.X)&&n.Value.X<Math.Max(a.X,b.X));
            if(e.Category=="Conjugal"&&adjacent&&plannedUnions.Add((Math.Min(e.From,e.To),Math.Max(e.From,e.To))))continue;
            if(a.Row==b.Row)Reserve(planned,(a.Row,-1),Math.Min(a.X,b.X),Math.Max(a.X,b.X));
            else{var sign=Math.Sign(b.Row-a.Row);Reserve(planned,(a.Row,sign),float.NegativeInfinity,float.PositiveInfinity);Reserve(planned,(b.Row,-sign),float.NegativeInfinity,float.PositiveInfinity);}
        }
        int LaneCount(int row,int side)=>planned.TryGetValue((row,side),out var tracks)?tracks.Count:0;
        var rowY = new float[rows.Count];
        for (int i = 1; i < rowY.Length; i++) rowY[i] = rowY[i - 1] + 180 + 14 * (LaneCount(i-1,1)+LaneCount(i,-1));
        var centerY = rowY[positioned[center].Row]; var centerX = positioned[center].X;
        var nodes = positioned.OrderBy(p => p.Key).Select(pair =>
        {
            var p = s.People[pair.Key]; return new FamilyGraphNode(p.Id, p.Name, $"{p.BirthDate:dd/MM/yyyy}",
                pair.Value.X - centerX, rowY[pair.Value.Row] - centerY, options.Network ? byUnit[sets.Find(p.Id)].Rank : levels[p.Id]);
        }).ToArray();
        var nodeMap = nodes.ToDictionary(n => n.PatientId);
        var strokes = new List<FamilyGraphStroke>(); var annotations = new List<FamilyGraphAnnotation>();
        var lanes = new Dictionary<(int Row,int Side),List<List<(float Min,float Max)>>>();
        float Lane(int id, int side,float min,float max)
        {
            var lane=Reserve(lanes,(positioned[id].Row,side),min,max);
            return nodeMap[id].Y + side * (62 + lane * 14);
        }
        void Line(float x1, float y1, float x2, float y2, string nature, string category, bool active, int[] people, bool arrow = false)
        {
            if (Math.Abs(x1 - x2) + Math.Abs(y1 - y2) < .01f) return;
            strokes.Add(new(new(x1, y1), new(x2, y2), nature, category, active, people, arrow));
        }
        foreach (var b in bundles.Where(CanBundle))
        {
            var people = b.Parents.Concat(b.Children).ToArray(); var parentY = nodeMap[b.Parents[0]].Y; var childY = nodeMap[b.Children[0]].Y; var sign = Math.Sign(childY - parentY);
            var originX = b.Parents.Average(id => nodeMap[id].X); var originY = Lane(b.Parents[0], sign,b.Parents.Min(id=>nodeMap[id].X),b.Parents.Max(id=>nodeMap[id].X)); var busY = Lane(b.Children[0], -sign,Math.Min(originX,b.Children.Min(id=>ChildX(b,id)-centerX)),Math.Max(originX,b.Children.Max(id=>ChildX(b,id)-centerX)));
            foreach (var p in b.Parents)
            {
                var n = nodeMap[p]; Line(n.X, n.Y + sign * 38, n.X, originY, b.Nature, "Genealógico", b.Active, people);
                Line(n.X, originY, originX, originY, b.Nature, "Genealógico", b.Active, people);
            }
            Line(originX, originY, originX, busY, b.Nature, "Genealógico", b.Active, people);
            var min = Math.Min(originX, b.Children.Min(id => ChildX(b,id)-centerX)); var max = Math.Max(originX, b.Children.Max(id => ChildX(b,id)-centerX));
            Line(min, busY, max, busY, b.Nature, "Genealógico", b.Active, people);
            foreach (var child in b.Children) { var n = nodeMap[child]; var port=ChildX(b,child)-centerX; Line(port, busY, port, n.Y - sign * 38, b.Nature, "Genealógico", b.Active, people, true); }
            if (b.Missing) annotations.Add(new(originX, busY - 16, "Geração intermediária desconhecida", people, true));
        }
        int gutter = 0;
        var drawnUnions = new HashSet<(int,int)>();
        foreach (var e in remainingEdges.OrderBy(e => e.Category == "Conjugal" ? 0 : 1).ThenBy(e => Math.Abs(nodeMap[e.From].X - nodeMap[e.To].X)))
        {
            var a = nodeMap[e.From]; var b = nodeMap[e.To]; int[] people = [e.From, e.To];
            void Segment(float x1, float y1, float x2, float y2, bool arrow = false) => Line(x1, y1, x2, y2, e.Nature, e.Category, e.IsActive, people, arrow);
            var sameRow = Math.Abs(a.Y - b.Y) < .01f;
            var adjacent = sameRow && !nodes.Any(n => n.PatientId != a.PatientId && n.PatientId != b.PatientId && Math.Abs(n.Y - a.Y) < .01f && n.X > Math.Min(a.X, b.X) && n.X < Math.Max(a.X, b.X));
            if (e.Category == "Conjugal" && adjacent && drawnUnions.Add((Math.Min(e.From,e.To),Math.Max(e.From,e.To))))
            {
                var sign = Math.Sign(b.X - a.X); Segment(a.X + sign * 100, a.Y, b.X - sign * 100, b.Y);
                annotations.Add(new((a.X + b.X) / 2, a.Y - 15, e.IsActive ? "União" : "União encerrada", people)); continue;
            }
            if (sameRow)
            {
                var y = Lane(a.PatientId, -1,Math.Min(a.X,b.X),Math.Max(a.X,b.X)); Segment(a.X, a.Y - 38, a.X, y); Segment(a.X, y, b.X, y); Segment(b.X, y, b.X, b.Y - 38, e.Generation > 0);
                annotations.Add(new((a.X + b.X) / 2, y - 16, e.Label, people)); continue;
            }
            // Long lateral links travel outside cards; each receives its own channel.
            var side = Math.Sign(b.Y - a.Y);
            var range = nodes.Where(n => n.Y >= Math.Min(a.Y, b.Y) && n.Y <= Math.Max(a.Y, b.Y)).ToArray();
            bool left = a.X + b.X < 0; var x = left ? range.Min(n => n.X) - 130 - gutter * 16 : range.Max(n => n.X) + 130 + gutter * 16; gutter++;
            var ay=Lane(a.PatientId,side,Math.Min(a.X,x),Math.Max(a.X,x));var by=Lane(b.PatientId,-side,Math.Min(b.X,x),Math.Max(b.X,x));
            Segment(a.X, a.Y + side * 38, a.X, ay); Segment(a.X, ay, x, ay); Segment(x, ay, x, by); Segment(x, by, b.X, by); Segment(b.X, by, b.X, b.Y - side * 38, e.Generation > 0);
            annotations.Add(new(x, (ay + by) / 2, e.Label + (e.MissingIntermediate ? " • intermediários desconhecidos" : ""), people, e.MissingIntermediate));
        }
        var groups = units.Where(u => u.People.Length > 1).Select(u =>
        {
            var ns = u.People.Select(id => nodeMap[id]).ToArray(); var union = partners.Any(r => u.People.Contains(r.FromPatientId) && u.People.Contains(r.ToPatientId));
            return new FamilyGraphGroup(u.People, ns.Min(n => n.X) - 112, ns[0].Y - 48, ns.Max(n => n.X) - ns.Min(n => n.X) + 224, union ? "Casal / uniões conhecidas" : "Parentalidade compartilhada");
        }).ToArray();
        return new(nodes, edges, truncated) { Strokes = MergeStrokes(strokes), Annotations = annotations, Groups = groups, SimplifiedEdges = visible.Length - drawingLinks.Count };
    }

    private static FamilyGraphStroke[] MergeStrokes(List<FamilyGraphStroke> strokes)
    {
        var result = new List<FamilyGraphStroke>();
        foreach (var group in strokes.GroupBy(s => (Vertical: Math.Abs(s.Start.X - s.End.X) < .01f,
            Axis: Math.Abs(s.Start.X - s.End.X) < .01f ? s.Start.X : s.Start.Y, s.Nature, s.Category, s.IsActive, s.Arrow)))
        {
            if (group.Key.Arrow) { result.AddRange(group); continue; }
            float Start(FamilyGraphStroke s) => group.Key.Vertical ? Math.Min(s.Start.Y, s.End.Y) : Math.Min(s.Start.X, s.End.X);
            float End(FamilyGraphStroke s) => group.Key.Vertical ? Math.Max(s.Start.Y, s.End.Y) : Math.Max(s.Start.X, s.End.X);
            FamilyGraphStroke? current = null; float begin = 0, end = 0; var people = new HashSet<int>();
            void Flush()
            {
                if (current == null) return;
                var a = group.Key.Vertical ? new FamilyGraphPoint(group.Key.Axis, begin) : new(begin, group.Key.Axis);
                var b = group.Key.Vertical ? new FamilyGraphPoint(group.Key.Axis, end) : new(end, group.Key.Axis);
                result.Add(current with { Start = a, End = b, People = people.Order().ToArray() });
            }
            foreach (var stroke in group.OrderBy(Start))
            {
                if (current == null || Start(stroke) > end + .01f) { Flush(); current = stroke; begin = Start(stroke); end = End(stroke); people = stroke.People.ToHashSet(); }
                else { end = Math.Max(end, End(stroke)); people.UnionWith(stroke.People); }
            }
            Flush();
        }
        return result.ToArray();
    }
}
