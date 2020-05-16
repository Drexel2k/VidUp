namespace Drexel.VidUp.UI.DllImport
{
    public enum ExecutionState : uint
    {
        EsAwayModeRequired= 0x00000040,
        EsContinous = 0x80000000,
        EsDisplayRequired = 0x00000002,
        EsSystemRequired = 0x00000001,
    }
}
