namespace EventDesk.Constants;

public static class DatabaseConstants
{
    public const string ApplicationDataDirectoryName = "EventDesk";
    public const string DataDirectoryName = "Data";
    public const string DatabaseFileName = "EventDesk.db";
    public const string WalJournalMode = "wal";
    public const string SetWalJournalModeSql = "PRAGMA journal_mode=WAL;";
    public const string WalModeNotEnabled = "SQLite WAL journal mode was not enabled.";
}
