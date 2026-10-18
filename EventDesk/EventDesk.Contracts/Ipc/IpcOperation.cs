namespace EventDesk.Contracts.Ipc;

public enum IpcOperation
{
    GetEvents = 1,
    CreateEvent = 2,
    UpdateEvent = 3,
    DeleteEvent = 4,
    GetReminderHistory = 5
}
