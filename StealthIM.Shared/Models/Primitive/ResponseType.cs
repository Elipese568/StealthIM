namespace StealthIM.Shared.Models.Primitive;

public enum ResponseType
{
    Success,
    Failure,
    NeedSetOptions,
    PlainMessage,
    MarkdownMessage,
    PictureMessage,
    FileMessage,
    ReplyMessage,
    GlobalPostMessage,
    ClickUserMessage,
    PostAllOnlineUsersMessage
}
