namespace Drexel.VidUp.UI.EventAggregation
{
    public class AutoSettingPlaylistsStateChangedMessage
    {
        public string Message { get; }
        public bool Success { get; }

        public AutoSettingPlaylistsStateChangedMessage(bool success, string message)
        {
            this.Message = message;
            this.Success = success;
        }
    }
}
