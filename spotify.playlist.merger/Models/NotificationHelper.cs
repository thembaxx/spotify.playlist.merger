namespace spotify.playlist.merger.Models
{
    public class NotificationHelper : NotificationBase
    {
        private NotificationType _type;
        public NotificationType Type
        {
            get => _type;
            set
            {
                _ = SetProperty(_type, value, () => _type = value);
                UpdateType(value);
            }
        }

        private string _text;
        public string Text
        {
            get => _text;
            set => _ = SetProperty(_text, value, () => _text = value);
        }

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set => _ = SetProperty(_isError, value, () => _isError = value);
        }

        private bool _isWarning;
        public bool IsWarning
        {
            get => _isWarning;
            set => _ = SetProperty(_isWarning, value, () => _isWarning = value);
        }

        private bool _isSuccess;
        public bool IsSuccess
        {
            get => _isSuccess;
            set => _ = SetProperty(_isSuccess, value, () => _isSuccess = value);
        }

        private bool _isInfo;
        public bool IsInfo
        {
            get => _isInfo;
            set => _ = SetProperty(_isInfo, value, () => _isInfo = value);
        }

        private void UpdateType(NotificationType type)
        {
            IsError = false;
            IsWarning = false;
            IsSuccess = false;
            IsInfo = false;
            switch (type)
            {
                case NotificationType.Error:
                    IsError = true;
                    break;
                case NotificationType.Warning:
                    IsWarning = true;
                    break;
                case NotificationType.Success:
                    IsSuccess = true;
                    break;
                case NotificationType.Info:
                    IsInfo = true;
                    break;
            }
        }
    }

    public enum NotificationType
    {
        Error,
        Warning,
        Success,
        Info,
    }
}
