using CommunityToolkit.Mvvm.ComponentModel;


namespace Starward.Features.Setting;

public class ToolboxItem : ObservableObject
{


    public string? Icon { get; set; }


    public string? Image { get; set; }


    public string Name { get; set => SetProperty(ref field, value); }


    public string Description { get; set => SetProperty(ref field, value); }


    public string Tag { get; set; }


    public string NameResourceKey { get; set; }


    public string DescriptionResourceKey { get; set; }



    public void UpdateLanguage()
    {
        Name = Lang.ResourceManager.GetString(NameResourceKey) ?? Name;
        Description = Lang.ResourceManager.GetString(DescriptionResourceKey) ?? Description;
    }



    public ToolboxItem(string? icon, string? image, string tag, string nameResourceKey, string descriptionResourceKey)
    {
        Icon = icon;
        Image = image;
        Tag = tag;
        NameResourceKey = nameResourceKey;
        DescriptionResourceKey = descriptionResourceKey;
        UpdateLanguage();
    }



}