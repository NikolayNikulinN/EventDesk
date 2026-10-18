namespace EventDesk.Application.Email;

public sealed class EmailDeliveryException(string errorCode, bool isPermanent) : Exception(errorCode)
{
    public string ErrorCode { get; } = errorCode;
    public bool IsPermanent { get; } = isPermanent;
}
