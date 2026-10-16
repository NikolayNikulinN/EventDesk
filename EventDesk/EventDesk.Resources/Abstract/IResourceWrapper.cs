namespace EventDesk.Resources.Abstract;

/// <summary>Provides localized application strings and shared image paths.</summary>
public interface IResourceWrapper
{
    string AppTitle { get; }

    string MinimizeToolTip { get; }

    string MaximizeToolTip { get; }

    string RestoreToolTip { get; }

    string CloseToolTip { get; }

    string Calendar { get; }

    string History { get; }

    string Settings { get; }

    string About { get; }

    string AppIconPath { get; }

    string MinimizeIconPath { get; }

    string MaximizeIconPath { get; }

    string RestoreIconPath { get; }

    string CloseIconPath { get; }
}
