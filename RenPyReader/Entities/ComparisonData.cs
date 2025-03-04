using DiffPlex.DiffBuilder.Model;

namespace RenPyReader.Entities
{
    class ComparisonData
    {
        public string Filename { get; private set; }

        public bool HasDifferences { get; private set; }

        public DiffPaneModel? DiffPaneModel { get; private set; }

        public RenPyDataRepository? Repository { get; set; }

        public ComparisonData(string filename)
        {
            Filename = filename;
            HasDifferences = false;
            DiffPaneModel = null;
            Repository = null;
        }

        public ComparisonData(string filename, DiffPaneModel diffPaneModel, RenPyDataRepository? repository)
        {
            Filename = filename;
            HasDifferences = true;
            DiffPaneModel = diffPaneModel;
            Repository = repository;
        }

        public override string ToString() => $"{Filename} | {HasDifferences} | {DiffPaneModel?.Lines.Count}";
    }
}
