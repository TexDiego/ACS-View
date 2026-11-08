using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using System.Text.Json;

internal static class FamilyGraphLayoutTests
{
    public static void Run(Action<bool,string> check)
    {
        var people=Enumerable.Range(1,180).ToDictionary(id=>id,id=>new Patient{Id=id,UserId=1,Name=$"Pessoa {id:000}"});
        var links=new List<FamilyRelationship>();
        void Link(int from,int to,string type="parent",bool active=true)
        {
            var t=KinshipCatalog.Get(type);var r=new FamilyRelationship{Id=links.Count+1,UserId=1,FromPatientId=from,ToPatientId=to,TypeCode=type,ForwardLabel=t.Forward,ReverseLabel=t.Reverse,IsActive=active,Nature=type=="parent"?"Biológico":"Não informado"};
            KinshipEngine.Normalize(r);links.Add(r);
        }
        Link(1,2,"marriage");Link(3,4,"marriage");Link(5,6,"union");
        Link(3,1);Link(4,1);Link(5,2);Link(6,2);
        for(int i=0;i<40;i++)
        {
            int child=7+i,partner=47+i;Link(1,child);Link(2,child);Link(child,partner,"union");
            for(int j=0;j<2;j++){int grandchild=87+i*2+j;Link(child,grandchild);Link(partner,grandchild);}
        }
        Link(3,7,"grandparent");Link(7,8,"sibling");Link(1,2,"marriage",false);
        var snapshot=new KinshipSnapshot(1,people,links);KinshipEngine.Validate(snapshot);
        var tree=FamilyGraphBuilder.Build(snapshot,1,new(Ancestors:3,Descendants:3));
        var network=FamilyGraphBuilder.Build(snapshot,1,new(Network:true,NetworkDepth:4));
        check(tree.Nodes.Count==166,"large tree keeps one identity for all known family members");
        check(tree.Nodes.Select(n=>n.PatientId).Distinct().Count()==tree.Nodes.Count,"large tree never duplicates spouses or ancestors");
        check(tree.SimplifiedEdges>=3,"tree collapses demonstrated grandparents, siblings and repeated union episodes");
        check(network.Edges.Count==links.Count,"network preserves every visible declaration including historical episodes");
        foreach(var graph in new[]{tree,network})
        {
            var parentCenter=graph.Nodes.Where(n=>n.PatientId is 3 or 4).Average(n=>n.X);
            check(Math.Abs(parentCenter-graph.Nodes.Single(n=>n.PatientId==1).X)<600,"redundant grandparent declaration does not pull ancestors away from their child");
            bool overlap=false;
            for(int i=0;i<graph.Nodes.Count;i++)for(int j=i+1;j<graph.Nodes.Count;j++)
                if(Math.Abs(graph.Nodes[i].X-graph.Nodes[j].X)<200&&Math.Abs(graph.Nodes[i].Y-graph.Nodes[j].Y)<76)overlap=true;
            check(!overlap,"large family cards do not overlap in "+(graph==tree?"tree":"network"));
            foreach(var couple in links.Where(r=>r.TypeCode is "marriage" or "union" && r.IsActive))
            {
                var a=graph.Nodes.Single(n=>n.PatientId==couple.FromPatientId);var b=graph.Nodes.Single(n=>n.PatientId==couple.ToPatientId);
                if(Math.Abs(a.X-b.X)>230.1 || Math.Abs(a.Y-b.Y)>.01)throw new Exception("Couple separated by unrelated people");
            }
            check(true,"known couples remain adjacent in "+(graph==tree?"tree":"network"));
            bool crossingCard=false;
            foreach(var stroke in graph.Strokes)foreach(var node in graph.Nodes)
            {
                // Every path, including long lateral links, stays outside the interior of every card.
                if(Math.Abs(stroke.Start.X-stroke.End.X)<.01)
                    crossingCard|=stroke.Start.X>node.X-99&&stroke.Start.X<node.X+99&&Math.Max(stroke.Start.Y,stroke.End.Y)>node.Y-37&&Math.Min(stroke.Start.Y,stroke.End.Y)<node.Y+37;
                else
                    crossingCard|=stroke.Start.Y>node.Y-37&&stroke.Start.Y<node.Y+37&&Math.Max(stroke.Start.X,stroke.End.X)>node.X-99&&Math.Min(stroke.Start.X,stroke.End.X)<node.X+99;
            }
            check(!crossingCard,"routed connections do not pass through person cards in "+(graph==tree?"tree":"network"));
            check(graph.Strokes.All(s=>float.IsFinite(s.Start.X)&&float.IsFinite(s.Start.Y)&&float.IsFinite(s.End.X)&&float.IsFinite(s.End.Y)),"all layout coordinates remain finite");
        }
        var star=FamilyGraphBuilder.Build(snapshot with{Relationships=links.Where(r=>r.FromPatientId<=2&&r.ToPatientId<=46).ToArray()},1,new());
        check(star.Strokes.Count<star.Edges.Count,"forty siblings share a single family branch instead of eighty separate paths");
        var rootChildren=star.Strokes.Where(s=>s.Arrow).SelectMany(s=>s.People).Distinct().ToHashSet();
        check(Enumerable.Range(7,40).All(rootChildren.Contains),"shared branches retain all forty children");
        var adopted=links.Single(r=>r.FromPatientId==2&&r.ToPatientId==7).Copy();adopted.Nature="Adotivo";
        var mixed=FamilyGraphBuilder.Build(snapshot with{Relationships=links.Where(r=>r.Id!=adopted.Id).Append(adopted).ToArray()},1,new());
        check(mixed.Strokes.Any(s=>s.Nature=="Adotivo")&&mixed.Strokes.Any(s=>s.Nature=="Biológico"),"shared branches never merge different parental natures");
        var mixedChild=mixed.Nodes.Single(n=>n.PatientId==7);
        check(mixed.Strokes.Where(s=>s.Arrow&&Math.Abs(s.End.Y-(mixedChild.Y-38))<.01&&Math.Abs(s.End.X-mixedChild.X)<100).Select(s=>s.End.X).Distinct().Count()>=2,"different parental natures use separate ports on the child card");
        var partial=FamilyGraphBuilder.Build(snapshot,1,new(MaxNodes:20));check(partial.Truncated&&partial.Nodes.Count==20,"bounded layout still reports truncated families");
        var repeat=FamilyGraphBuilder.Build(snapshot with{Relationships=links.AsEnumerable().Reverse().ToArray()},1,new(Ancestors:3,Descendants:3));
        check(tree.Nodes.SequenceEqual(repeat.Nodes),"family placement is stable when database row order changes");
        var path=Path.Combine(Directory.GetCurrentDirectory(),"tmp","family-layout-review");Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path,"tree.json"),JsonSerializer.Serialize(tree));
        File.WriteAllText(Path.Combine(path,"network.json"),JsonSerializer.Serialize(network));
    }
}
