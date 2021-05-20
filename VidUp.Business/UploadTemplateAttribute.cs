using System.ComponentModel;

namespace Drexel.VidUp.Business
{
    public enum UploadTemplateAttribute
    {
        [Description("All")]
        All,
        [Description("Title")]
        Title,
        [Description("Description")]
        Description,
        [Description("Tags")]
        Tags,
        [Description("Visibility")]
        Visibility,
        [Description("Video Language")]
        VideoLanguage,
        [Description("Description Language")]
        DescriptionLanguage,
        [Description("Publish At")]
        PublishAt,
        [Description("Playlist")]
        Playlist,
        [Description("Category")]
        Category
    }
}
