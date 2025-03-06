using RenPyReader.Entities;

namespace RenPyReader.Services
{
    internal interface IApplicationStateService
    {
        void SetDataRepository(RenPyDataRepository dataRepository);

        RenPyDataRepository? GetDataRepository();

        void SetVersion(string version);

        string? GetVersion();
    }
}
