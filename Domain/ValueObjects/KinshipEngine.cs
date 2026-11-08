using ACS_View.Domain.Entities;
namespace ACS_View.Domain.ValueObjects;

public sealed record KinshipType(string Code, string Category, string Forward, string Reverse, bool Symmetric = false, int Generation = 0);
public static class KinshipCatalog
{
    public static readonly string[] Natures = ["Biológico", "Adotivo", "Socioafetivo", "Não informado"];
    public static readonly string[] EndReasons = ["Não informado", "Separação", "Divórcio", "Falecimento"];
    public static readonly KinshipType[] Types = [
        new("parent","Genealógico","Pai/Mãe","Filho/Filha",false,1),
        new("grandparent","Genealógico","Avô/Avó","Neto/Neta",false,2),
        new("greatgrandparent","Genealógico","Bisavô/Bisavó","Bisneto/Bisneta",false,3),
        new("sibling","Genealógico","Irmão/Irmã","Irmão/Irmã",true),
        new("uncle","Genealógico","Tio/Tia","Sobrinho/Sobrinha"),
        new("marriage","Conjugal","Casamento","Casamento",true),
        new("union","Conjugal","União estável","União estável",true),
        new("partner","Conjugal","Companheirismo","Companheirismo",true),
        new("step","Afinidade","Padrasto/Madrasta","Enteado/Enteada"),
        new("inlaw","Afinidade","Sogro/Sogra","Genro/Nora"),
        new("siblinginlaw","Afinidade","Cunhado/Cunhada","Cunhado/Cunhada",true),
        new("cousin","Genealógico","Primo/Prima","Primo/Prima",true),
        new("godparent","Social","Padrinho/Madrinha","Afilhado/Afilhada"),
        new("custom","Social","Personalizado","Personalizado")];
    public static KinshipType Get(string code) => Types.FirstOrDefault(t => t.Code == code) ?? throw new ArgumentException("Tipo de vínculo desconhecido.");
}
public sealed record KinshipEvidence(int[] RelationshipIds, string[] Natures, int[] PatientIds);
public sealed class KnownRelative
{
    public int PatientId { get; init; }
    public string TypeCode { get; init; } = "";
    public string Label { get; set; } = "";
    public List<int> DeclaredIds { get; } = [];
    public List<KinshipEvidence> Paths { get; } = [];
    public bool IsDerived => Paths.Count > 0;
    public string Provenance => DeclaredIds.Count > 0 ? (IsDerived ? "Declarado e demonstrado" : "Declarado") : "Identificado automaticamente";
    public string SiblingClassification { get; set; } = "";
}
public sealed record KinshipSnapshot(int UserId, IReadOnlyDictionary<int, Patient> People, IReadOnlyList<FamilyRelationship> Relationships);
public sealed record KinshipPreview(IReadOnlyList<string> Effects, IReadOnlyList<string> Warnings, IReadOnlyList<FamilyRelationship> Suggestions);

// Pure graph rules: no UI, no database, no assumptions based on sex or cohabitation.
public static class KinshipEngine
{
    public static void Normalize(FamilyRelationship r)
    {
        var t = KinshipCatalog.Get(r.TypeCode);
        if ((t.Symmetric || t.Code=="custom") && r.FromPatientId > r.ToPatientId)
        { (r.FromPatientId,r.ToPatientId)=(r.ToPatientId,r.FromPatientId); (r.ForwardLabel,r.ReverseLabel)=(r.ReverseLabel,r.ForwardLabel); }
        if (r.TypeCode != "parent") r.Nature = "Não informado";
    }
    public static bool Equivalent(FamilyRelationship a, FamilyRelationship b) =>
        a.FromPatientId==b.FromPatientId && a.ToPatientId==b.ToPatientId && a.TypeCode==b.TypeCode &&
        (a.TypeCode!="custom" || (a.ForwardLabel==b.ForwardLabel && a.ReverseLabel==b.ReverseLabel)) &&
        (KinshipCatalog.Get(a.TypeCode).Category!="Conjugal" || (a.IsActive==b.IsActive && a.StartDate==b.StartDate && a.EndDate==b.EndDate));

    public static void Validate(KinshipSnapshot s)
    {
        var rs=s.Relationships;
        foreach(var r in rs)
        {
            var t=KinshipCatalog.Get(r.TypeCode);
            if(r.UserId!=s.UserId || !s.People.ContainsKey(r.FromPatientId) || !s.People.ContainsKey(r.ToPatientId)) throw new InvalidOperationException("As pessoas devem pertencer ao perfil atual.");
            if(r.FromPatientId==r.ToPatientId) throw new InvalidOperationException("Uma pessoa não pode ser vinculada a si mesma.");
            if((t.Symmetric || t.Code=="custom") && r.FromPatientId>r.ToPatientId) throw new InvalidOperationException("Ordem simétrica inválida.");
            if(string.IsNullOrWhiteSpace(r.ForwardLabel)||string.IsNullOrWhiteSpace(r.ReverseLabel)||r.ForwardLabel.Length>200||r.ReverseLabel.Length>200) throw new InvalidOperationException("Informe as descrições dos dois sentidos (até 200 caracteres).");
            if(!KinshipCatalog.Natures.Contains(r.Nature)||!KinshipCatalog.EndReasons.Contains(r.EndReason)) throw new InvalidOperationException("Classificação inválida.");
            if(r.StartDate.HasValue && r.EndDate<r.StartDate) throw new InvalidOperationException("A data de término não pode anteceder o início.");
            if(r.IsActive && r.EndDate.HasValue) throw new InvalidOperationException("Vínculos ativos não possuem data de término.");
        }
        foreach(var group in rs.GroupBy(r=>(r.FromPatientId,r.ToPatientId,r.TypeCode)))
        {
            var list=group.ToArray();
            for(var i=0;i<list.Length;i++) for(var j=i+1;j<list.Length;j++)
                if(Equivalent(list[i],list[j])) throw new InvalidOperationException($"Vínculo duplicado: {s.People[list[i].FromPatientId].Name} / {s.People[list[i].ToPatientId].Name}.");
        }
        var asc=rs.Where(r=>r.IsActive && KinshipCatalog.Get(r.TypeCode).Generation>0).ToLookup(r=>r.FromPatientId);
        var ancestry=rs.Where(r=>r.IsActive && KinshipCatalog.Get(r.TypeCode).Generation>0).ToArray();
        var indegree=ancestry.SelectMany(r=>new[]{r.FromPatientId,r.ToPatientId}).Distinct().ToDictionary(id=>id,_=>0);
        foreach(var r in ancestry)indegree[r.ToPatientId]++;
        var ready=new Queue<int>(indegree.Where(x=>x.Value==0).Select(x=>x.Key));var visited=0;
        while(ready.TryDequeue(out var id)){visited++;foreach(var r in asc[id])if(--indegree[r.ToPatientId]==0)ready.Enqueue(r.ToPatientId);}
        if(visited!=indegree.Count)throw new InvalidOperationException("Ciclo de ancestralidade. Revise os vínculos de "+string.Join(", ",indegree.Where(x=>x.Value>0).Take(10).Select(x=>$"{s.People[x.Key].Name} (#{x.Key})"))+".");
        foreach(var id in rs.Where(r=>r.IsActive).SelectMany(r=>new[]{r.FromPatientId,r.ToPatientId}).Distinct())
        {
            var relatives=Query(s with { Relationships=rs.Where(r=>r.IsActive).ToArray() },id);
            foreach(var (code,limit) in new[]{("parent",2),("grandparent",4),("greatgrandparent",8)})
            {
                var found=relatives.Where(x=>x.TypeCode==code).Select(x=>x.PatientId).Distinct().ToArray();
                if(found.Length>limit) throw new InvalidOperationException($"{s.People[id].Name}: limite de {limit} {KinshipCatalog.Get(code).Forward} excedido. Revise: {string.Join(", ",found.Select(p=>$"{s.People[p].Name} (#{p})"))}.");
            }
            var ancestors=relatives.Where(x=>x.TypeCode is "parent" or "grandparent" or "greatgrandparent").Select(x=>x.PatientId).ToHashSet();
            if(relatives.Any(x=>x.TypeCode=="sibling" && ancestors.Contains(x.PatientId))) throw new InvalidOperationException($"Ancestral não pode ser irmão de {s.People[id].Name}. Revise os vínculos.");
        }
    }

    public static IReadOnlyList<KnownRelative> Query(KinshipSnapshot s, int person)
    {
        var output=new Dictionary<(int,string),KnownRelative>();
        void Add(int id,string code,string label,FamilyRelationship? declared=null,KinshipEvidence? path=null)
        {
            if(id==person)return;
            if(!output.TryGetValue((id,code),out var item))output[(id,code)]=item=new KnownRelative{PatientId=id,TypeCode=code,Label=label};
            if(declared!=null && !item.DeclaredIds.Contains(declared.Id)) {item.DeclaredIds.Add(declared.Id); item.Label=label;}
            if(path!=null && !item.Paths.Any(p=>p.RelationshipIds.SequenceEqual(path.RelationshipIds)))item.Paths.Add(path);
        }
        foreach(var r in s.Relationships.Where(r=>r.FromPatientId==person||r.ToPatientId==person))
        {
            var forward=r.FromPatientId==person;
            var code=r.TypeCode;
            if(forward) code=code switch{"parent"=>"child","grandparent"=>"grandchild","greatgrandparent"=>"greatgrandchild","uncle"=>"nephew",_=>code};
            Add(forward?r.ToPatientId:r.FromPatientId,code,forward?r.ReverseLabel:r.ForwardLabel,r);
        }
        var active=s.Relationships.Where(r=>r.IsActive).ToArray();
        var parent=active.Where(r=>r.TypeCode=="parent").ToArray();
        var up=active.Where(r=>KinshipCatalog.Get(r.TypeCode).Generation>0).ToLookup(r=>r.ToPatientId);
        var down=active.Where(r=>KinshipCatalog.Get(r.TypeCode).Generation>0).ToLookup(r=>r.FromPatientId);
        void Walk(int id,bool upward,int depth,List<FamilyRelationship> path,List<int> people)
        {
            foreach(var r in (upward?up[id]:down[id]))
            {
                int next=upward?r.FromPatientId:r.ToPatientId, d=depth+KinshipCatalog.Get(r.TypeCode).Generation;
                if(d>3 || people.Contains(next))continue;
                var pp=path.Append(r).ToList();var nodes=people.Append(next).ToList();
                // A declared grandparent can extend known ancestry, but cannot supply missing people.
                var code=(upward,d) switch{(true,1)=>"parent",(true,2)=>"grandparent",(true,3)=>"greatgrandparent",(false,1)=>"child",(false,2)=>"grandchild",_=>"greatgrandchild"};
                if(pp.Count>1) Add(next,code,code switch{"parent"=>"Pai/Mãe","grandparent"=>"Avô/Avó","greatgrandparent"=>"Bisavô/Bisavó","child"=>"Filho/Filha","grandchild"=>"Neto/Neta",_=>"Bisneto/Bisneta"},path:new(pp.Select(x=>x.Id).ToArray(),pp.Select(x=>x.Nature).ToArray(),nodes.ToArray()));
                Walk(next,upward,d,pp,nodes);
            }
        }
        Walk(person,true,0,[],[person]);Walk(person,false,0,[],[person]);
        var parents=parent.ToLookup(r=>r.ToPatientId);var children=parent.ToLookup(r=>r.FromPatientId);
        var siblingPaths=new Dictionary<(int,int),List<FamilyRelationship[]>>();
        void Sib(int a,int b,FamilyRelationship[] path)
        { if(a==b)return; var key=(a,b);if(!siblingPaths.TryGetValue(key,out var ps))siblingPaths[key]=ps=[];ps.Add(path); }
        var relevant=parents[person].Select(r=>r.FromPatientId).Append(person).ToHashSet();
        foreach(var p in children) foreach(var a in p.Where(a=>relevant.Contains(a.ToPatientId))) foreach(var b in p) Sib(a.ToPatientId,b.ToPatientId,[a,b]);
        foreach(var r in active.Where(r=>r.TypeCode=="sibling")){if(relevant.Contains(r.FromPatientId))Sib(r.FromPatientId,r.ToPatientId,[r]);if(relevant.Contains(r.ToPatientId))Sib(r.ToPatientId,r.FromPatientId,[r]);}
        foreach(var (pair,paths) in siblingPaths)
        {
            foreach(var path in paths)
            {
                var ev=new KinshipEvidence(path.Select(x=>x.Id).ToArray(),path.Select(x=>x.Nature).ToArray(),[pair.Item1,pair.Item2]);
                if(pair.Item1==person && path.Length>1) Add(pair.Item2,"sibling","Irmão/Irmã",path:ev);
                foreach(var p in children[pair.Item1]) if(p.ToPatientId==person)
                    Add(pair.Item2,"uncle","Tio/Tia",path:new(path.Append(p).Select(x=>x.Id).ToArray(),path.Append(p).Select(x=>x.Nature).ToArray(),[person,pair.Item1,pair.Item2]));
                if(pair.Item1==person) foreach(var p in children[pair.Item2])
                    Add(p.ToPatientId,"nephew","Sobrinho/Sobrinha",path:new(path.Append(p).Select(x=>x.Id).ToArray(),path.Append(p).Select(x=>x.Nature).ToArray(),[person,pair.Item2,p.ToPatientId]));
            }
        }
        foreach(var relative in output.Values.Where(x=>x.TypeCode=="sibling"))
        {
            var a=parents[person].Select(x=>x.FromPatientId).ToHashSet();var b=parents[relative.PatientId].Select(x=>x.FromPatientId).ToHashSet();
            var shared=a.Intersect(b).Count();
            relative.SiblingClassification=shared==2?"Irmãos completos na parentalidade cadastrada (dois vínculos em comum)":shared==1&&a.Count==2&&b.Count==2&&parents[person].Concat(parents[relative.PatientId]).All(r=>r.Nature=="Biológico")?"Meio-irmãos biológicos (um vínculo parental em comum)":shared==1?"Um vínculo parental em comum; demais ascendentes desconhecidos":"Irmãos declarados";
        }
        return output.Values.ToArray();
    }
    public static KinshipPreview Preview(KinshipSnapshot before, KinshipSnapshot after, FamilyRelationship change)
    {
        Validate(after);
        var effects=new List<string>();var warnings=new List<string>();var suggestions=new List<FamilyRelationship>();
        foreach(var id in after.Relationships.SelectMany(r=>new[]{r.FromPatientId,r.ToPatientId}).Distinct())
        {
            var old=Query(before,id).ToDictionary(r=>(r.PatientId,r.TypeCode));
            foreach(var r in Query(after,id))
                if(!old.ContainsKey((r.PatientId,r.TypeCode)))effects.Add($"{after.People[id].Name}: {r.Label} — {after.People[r.PatientId].Name} ({r.Provenance})");
            var now=Query(after,id).Select(r=>(r.PatientId,r.TypeCode)).ToHashSet();
            foreach(var r in old.Values.Where(r=>!now.Contains((r.PatientId,r.TypeCode))))effects.Add($"Deixa de ser demonstrado: {before.People[id].Name} / {r.Label} / {before.People[r.PatientId].Name}");
        }
        if(change.TypeCode=="parent" && after.People.TryGetValue(change.FromPatientId,out var p) && after.People.TryGetValue(change.ToPatientId,out var child) && (p.BirthDate>=child.BirthDate || (change.Nature=="Biológico" && child.BirthDate.Year>12 && p.BirthDate>child.BirthDate.AddYears(-12))))
            warnings.Add("Revise as datas de nascimento: o ascendente não é mais velho que o descendente. A natureza do vínculo deve ser considerada.");
        if(change.TypeCode=="sibling") foreach(var (a,b) in new[]{(change.FromPatientId,change.ToPatientId),(change.ToPatientId,change.FromPatientId)})
            foreach(var r in after.Relationships.Where(r=>r.IsActive&&r.TypeCode=="parent"&&r.ToPatientId==a))
                if(!after.Relationships.Any(x=>x.IsActive&&x.TypeCode=="parent"&&x.FromPatientId==r.FromPatientId&&x.ToPatientId==b))
                {var suggestion=r.Copy();suggestion.Id=0;suggestion.ToPatientId=b;suggestions.Add(suggestion);}
        return new(effects,warnings,suggestions);
    }
}
