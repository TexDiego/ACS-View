using ACS_View.Application.DTOs;

namespace ACS_View.Controls;

public sealed class VectorControlTemplateSelector : DataTemplateSelector
{
    public DataTemplate SectorTemplate { get; set; } = null!;
    public DataTemplate BlockTemplate { get; set; } = null!;
    public DataTemplate ResidenceTemplate { get; set; } = null!;
    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) => ((VectorControlRow)item).Kind switch
    {
        VectorRowKind.Sector => SectorTemplate,
        VectorRowKind.Block => BlockTemplate,
        _ => ResidenceTemplate
    };
}
