using Xunit;

namespace Finance.Tests;

// Коллекция для синхронизации тестов
[CollectionDefinition("Console I/O", DisableParallelization = true)]
public class ConsoleCollection { }