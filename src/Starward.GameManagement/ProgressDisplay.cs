namespace Starward.GameManagement;

[Flags]
public enum ProgressDisplay
{
    None = 0,

    Progress = 1,

    NetworkSpeed = 2,

    NetworkBytes = 4,

    StorageBytes = 8,
}