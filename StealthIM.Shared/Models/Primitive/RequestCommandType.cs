namespace StealthIM.Shared.Models.Primitive;

public enum RequestCommandType
{
    Register,
    LoginByUnPw,
    LoginBySession,
    ChangePassword,
    UserSetting,
    GetUserInformation,
    SwitchSendMethod,
    SendPlainMessage,
    SendMarkdownMessage,
    SendPictureMessage,
    SendFileMessage,
    ReplyMessage,
    GlobalPost,
    PostAllOnlineUsers,
    ClickUser,
    Unregister
}
