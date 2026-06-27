using ACS_View.Application.DTOs;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class CidDetailsPopup : Popup
{
    public CidDetailsPopup(CidSearchResultDto cid)
    {
        InitializeComponent();
        BindingContext = cid;

        ChapterLabel.Text = FormatHierarchy(cid.ChapterCode, cid.ChapterDescription);
        GroupLabel.Text = FormatHierarchy(cid.GroupCode, cid.GroupDescription);
        CategoryLabel.Text = FormatHierarchy(cid.CategoryCode, cid.CategoryDescription);
    }

    private static string FormatHierarchy(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(description))
        {
            return "Nao informado";
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return description;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return code;
        }

        return $"{code} - {description}";
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }
}
