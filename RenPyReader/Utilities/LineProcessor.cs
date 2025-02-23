using RenPyReader.Entities;

namespace RenPyReader.Utilities
{
    internal partial class LineProcessor
    {
        private readonly Dictionary<Func<string, bool>, ProcessorInfo> _processors;

        internal delegate T ProcessLineDelegate<T>(LineContext context);

        internal LineProcessor()
        {
            _processors = [];
        }
    }

    internal class ProcessorInfo
    {
        internal required Func<string, bool> Condition { get; set; }

        internal required Func<LineContext, Task<object?>> Processor { get; set; }

        internal required Type ResultType { get; set; }
    }
}
