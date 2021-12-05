using MRK.Data;

namespace MRK.Services
{
    public class Service
    {
        private OnDemandDatabase _primaryDatabase;

        protected OnDemandDatabase PrimaryDatabase
        {
            get { return _primaryDatabase; }
        }

        public Service()
        {
        }

        public virtual void Initialize()
        {
        }

        protected void SetupPrimaryDatabase(string db)
        {
            if (_primaryDatabase != null)
            {
                Logger.LogError("Primary db already setup");
                return;
            }

            _primaryDatabase = new OnDemandDatabase(db);
        }

        protected virtual void OnServiceShutdown()
        {
        }

        public void Shutdown()
        {
            OnServiceShutdown();

            if (_primaryDatabase != null)
            {
                _primaryDatabase.Shutdown();
            }
        }

        protected static string GetDatabasePath(string db)
        {
            string dir = $"{EGR.WorkingDirectory}|Data";
            MRKPlatformUtils.CreateRecursiveDirectory(dir);

            return MRKPlatformUtils.LocalizePath($"{dir}|{db}");
        }

        protected static string EscapeValue(string value)
        {
            return value.Replace("'", "''");
        }
    }
}
