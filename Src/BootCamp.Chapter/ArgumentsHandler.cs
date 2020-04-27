using System;
using System.IO;

namespace BootCamp.Chapter
{
    internal class ArgumentsHandler
    {
        private const int MinimumArgumentCount = 2;
        private const int InputArgumentIndex = 0;
        private const int CommandArgumentIndex = 1;

        enum FileFormat
        {
            None = 0,
            Csv = 1, 
            Json = 2,
        }

        public static ArgumentsHandler Create(string[] args)
        {
            if (args.Length < MinimumArgumentCount)
            {
                throw new InvalidCommandException();
            }

            var inputFile = new FileInfo(args[InputArgumentIndex]);

            if (NonexistentOrEmptyFile(inputFile))
            {
                throw new NoTransactionsFoundException();
            }

            var format = GetFileFormat(inputFile);

            if (format == FileFormat.None)
            {
                throw new NoTransactionsFoundException();
            }

            var parsedCommand = new CommandHandler(args[CommandArgumentIndex..]).ParseCommand();


            switch (format)
            {
                case FileFormat.Csv:
                    return new ArgumentsHandler(CsvStreamFactory, inputFile, parsedCommand);
                case FileFormat.Json:
                    return new ArgumentsHandler(JsonStreamFactory, inputFile, parsedCommand);
                default:
                    throw new ArgumentException("Unknown input file format.", nameof(args));

            }
        }

        private static ITransactionStream JsonStreamFactory(Stream stream) => new JsonTransactionStream(stream);

        private static ITransactionStream CsvStreamFactory(Stream stream) => new CsvTransactionStream(stream);

        private static FileFormat GetFileFormat(FileInfo inputFile)
        {
            return inputFile.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ?
                FileFormat.Csv : inputFile.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ?
                FileFormat.Json : FileFormat.None;
        }

        private static bool NonexistentOrEmptyFile(FileInfo inputFile)
        {
            return !inputFile.Exists || inputFile.OpenText().ReadToEnd().Length == 0;
        }

        private readonly FileInfo _inputFile;
        private readonly Action<ITransactionStream> _command;
        private readonly Func<Stream, ITransactionStream> _streamFactory;


        public ArgumentsHandler(Func<Stream, ITransactionStream> streamFactory, FileInfo inputFile, Action<ITransactionStream> parsedCommand)
        {
            _streamFactory = streamFactory;
            _inputFile = inputFile;
            _command = parsedCommand;
        }

        public void Execute()
        {
            using var inputStream = _inputFile.OpenRead();
            using var transactionStream = _streamFactory(inputStream);
            _command(transactionStream);
        }
    }
}
