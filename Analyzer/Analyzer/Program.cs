using Analyzer;

// "--web [port] [--no-browser]" starts the local web interface; without --web the console flow runs.
// Local and private addresses are blocked unless "--allow-private" is given.
HttpAnalyzer httpAnalyzer = new HttpAnalyzer(allowPrivateAddresses: args.Contains("--allow-private"));

if (args.Contains("--web"))
{
    int port = WebServer.DefaultPort;
    int portIndex = Array.IndexOf(args, "--web") + 1;
    if (portIndex < args.Length && !args[portIndex].StartsWith("--") && !int.TryParse(args[portIndex], out port))
    {
        Console.Error.WriteLine("The port must be a number, for example: --web 5080");
        return 1;
    }

    using CancellationTokenSource cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    bool openBrowser = !args.Contains("--no-browser");
    return await WebServer.RunAsync(port, httpAnalyzer, Console.Out, cts.Token, openBrowser);
}

return await ConsoleApp.RunAsync(Console.In, Console.Out, Console.Error, httpAnalyzer);
