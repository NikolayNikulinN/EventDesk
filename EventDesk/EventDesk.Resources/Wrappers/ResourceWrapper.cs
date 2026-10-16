using System.Globalization;
using EventDesk.Resources.Abstract;
using EventDesk.Resources.Resources;

namespace EventDesk.Resources.Wrappers;

/// <summary>Provides localized application resources to bindings and services.</summary>
public sealed class ResourceWrapper : IResourceWrapper
{
    private const string ImagePath = "/EventDesk.Resources;component/Images/";

    public string AppTitle => GetString(nameof(AppTitle));

    public string MinimizeToolTip => GetString(nameof(MinimizeToolTip));

    public string MaximizeToolTip => GetString(nameof(MaximizeToolTip));

    public string RestoreToolTip => GetString(nameof(RestoreToolTip));

    public string CloseToolTip => GetString(nameof(CloseToolTip));

    public string AppIconPath => ImagePath + "appIcon.png";

    public string MinimizeIconPath => ImagePath + "minimize.png";

    public string MaximizeIconPath => ImagePath + "maximize.png";

    public string RestoreIconPath => ImagePath + "restore.png";

    public string CloseIconPath => ImagePath + "close.png";

    /// <inheritdoc />
    public string NoButton => GetString(nameof(NoButton));

    /// <inheritdoc />
    public string OkButton => GetString(nameof(OkButton));              

    private static string GetString(string key) => AppResource.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
