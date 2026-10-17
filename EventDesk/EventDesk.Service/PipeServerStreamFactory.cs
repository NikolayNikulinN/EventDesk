using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using EventDesk.Constants;

namespace EventDesk.Service;

internal static class PipeServerStreamFactory
{
    public static NamedPipeServerStream Create(string pipeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        PipeSecurity security = new();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        SecurityIdentifier serviceIdentity = identity.User
            ?? throw new InvalidOperationException(IpcConstants.PipeAccessDenied);
        security.AddAccessRule(new PipeAccessRule(
            serviceIdentity,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        PipeAccessRights clientRights =
            PipeAccessRights.ReadData
            | PipeAccessRights.WriteData
            | PipeAccessRights.ReadAttributes
            | PipeAccessRights.WriteAttributes
            | PipeAccessRights.ReadExtendedAttributes
            | PipeAccessRights.WriteExtendedAttributes
            | PipeAccessRights.ReadPermissions
            | PipeAccessRights.Synchronize;
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.InteractiveSid, null),
            clientRights,
            AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            pipeName,
            PipeDirection.InOut,
            IpcConstants.MaxConcurrentConnections,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            IpcConstants.PipeBufferBytes,
            IpcConstants.PipeBufferBytes,
            security);
    }
}
