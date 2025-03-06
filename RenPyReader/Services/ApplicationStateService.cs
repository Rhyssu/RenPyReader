using RenPyReader.Entities;

namespace RenPyReader.Services
{
    internal class ApplicationStateService : IApplicationStateService
    {
        private RenPyDataRepository? DataRepository { get; set; }

        private string? Version { get; set; }

        RenPyDataRepository? IApplicationStateService.GetDataRepository()
        {
            return DataRepository;
        }

        void IApplicationStateService.SetDataRepository(RenPyDataRepository dataRepository)
        {
            DataRepository = dataRepository;
        }

        public string? GetVersion()
        {
            return Version;
        }

        public void SetVersion(string version)
        {
            Version = version;
        }
    }
}