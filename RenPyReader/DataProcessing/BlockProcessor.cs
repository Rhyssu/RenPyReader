namespace RenPyReader.DataProcessing
{
    internal class BlockProcessor
    {
        internal List<Block> Blocks = new();

        internal bool BlocksReadSuccessfull => OpenBlocks.Count == 0;

        private List<(int indent, Block block)> OpenBlocks = new();

        internal async Task ProcessFileContentAsync(string content)
        {
            using StringReader reader = new(content);
            {
                int lastNotNullOrEmptyLineIndex = 0;
                string lastNotNullOrEmptyLineContent = string.Empty;

                string? line; int index = 1;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.EndsWith(':'))
                    {
                        int indent = CountIndentLevel(line);
                        var blocks = OpenBlocks.Where(b => b.indent >= indent).ToArray();
                        if (blocks.Length != 0)
                        {
                            foreach (var block in blocks)
                            {
                                CloseAndRemoveBlock(block.block, lastNotNullOrEmptyLineIndex, lastNotNullOrEmptyLineContent);
                            }

                            OpenNewBlock(indent, index, line);
                        }
                        else
                        {
                            OpenNewBlock(indent, index, line);
                        }
                    }

                    if (!string.IsNullOrEmpty(line))
                    {
                        lastNotNullOrEmptyLineIndex = index;
                        lastNotNullOrEmptyLineContent = line;
                    }

                    index++;
                }

                var blocksOpenCount = OpenBlocks.Count();
                foreach (var block in OpenBlocks)
                {
                    CloseBlock(block.block, lastNotNullOrEmptyLineIndex, lastNotNullOrEmptyLineContent);
                }

                OpenBlocks.RemoveRange(0, blocksOpenCount);
            }
        }

        private void OpenNewBlock(int indent, int index, string line)
        {
            var newBlock = new Block()
            {
                Indent = indent,
                OpeningLineIndex = index,
                OpeningLineContent = line.Trim()
            };

            OpenBlocks.Add((indent, newBlock));
        }

        private void CloseAndRemoveBlock(Block block, int index, string line)
        {
            block.ClosingLineIndex = index;
            block.ClosingLineContent = line.Trim();
            block.Length = 
                (block.ClosingLineIndex - block.OpeningLineIndex);

            OpenBlocks.Remove((block.Indent, block));
            Blocks.Add(block);
        }

        private void CloseBlock(Block block, int index, string line)
        {
            block.ClosingLineIndex = index;
            block.ClosingLineContent = line.Trim();
            block.Length =
                (block.ClosingLineIndex - block.OpeningLineIndex);
            Blocks.Add(block);
        }

        private int CountIndentLevel(string line)
        {
            int indentLevel = line.Length - line.TrimStart().Length;
            return indentLevel;
        }
    }

    internal class Block
    {
        internal int Indent { get; set; }

        internal int OpeningLineIndex { get; set; }

        internal int ClosingLineIndex { get; set; }
        
        internal int Length { get; set; }

        internal string? OpeningLineContent { get; set; }

        internal string? ClosingLineContent { get; set; }
    }
}