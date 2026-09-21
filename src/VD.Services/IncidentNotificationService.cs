using VD.Core;
namespace VD.Services
{
    // Existing prototype has no SMTP implementation. Do not claim a notification was sent.
    public sealed class IncidentNotificationService
    {
        public object Report(RegionalApplication app,string reason) => new {
            Sent=false, Mode="Demo", ApplicationId=app.Id,
            Message=string.IsNullOrEmpty(reason) ? app.AppNo+" 已設為正常運送" : app.AppNo+" 異常已回報（寄信為示意，尚未串接 SMTP）"
        };
    }
}
