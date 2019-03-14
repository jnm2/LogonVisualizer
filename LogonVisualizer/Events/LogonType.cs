namespace LogonVisualizer.Events
{
    /// <summary>
    /// https://docs.microsoft.com/windows/security/threat-protection/auditing/event-4624#logon-types-and-descriptions
    /// </summary>
    public enum LogonType : uint
    {
        Interactive = 2,
        Network = 3,
        Batch = 4,
        Service = 5,
        Unlock = 7,
        NetworkCleartext = 8,
        NewCredentials = 9,
        RemoteInteractive = 10,
        CachedInteractive = 11
    }
}
