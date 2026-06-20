using ACS_View.Domain.Enums;

namespace ACS_View.Views;

public class VaccineSectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ChildTemplate { get; set; }
    public DataTemplate? TeenTemplate { get; set; }
    public DataTemplate? AdultTemplate { get; set; }
    public DataTemplate? ElderlyTemplate { get; set; }
    public DataTemplate? PregnantTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item switch
        {
            VaccineSectionType.Child => ChildTemplate ?? EmptyTemplate(),
            VaccineSectionType.Teen => TeenTemplate ?? EmptyTemplate(),
            VaccineSectionType.Adult => AdultTemplate ?? EmptyTemplate(),
            VaccineSectionType.Elderly => ElderlyTemplate ?? EmptyTemplate(),
            VaccineSectionType.Pregnant => PregnantTemplate ?? EmptyTemplate(),
            _ => EmptyTemplate()
        };
    }

    private static DataTemplate EmptyTemplate()
    {
        return new DataTemplate(() => new ContentView());
    }
}
