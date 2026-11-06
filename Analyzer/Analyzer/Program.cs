using Analyzer;

// "--web [port]" starts the local web interface; without it the console flow runs.
if (args.Contains("--web"))
{
    int port = WebServer.DefaultPort;
    int portIndex = Array.IndexOf(args, "--web") + 1;
    if (portIndex < args.Length && !int.TryParse(args[portIndex], out port))
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

    return await WebServer.RunAsync(port, new HttpAnalyzer(), Console.Out, cts.Token);
}

return await ConsoleApp.RunAsync(Console.In, Console.Out, Console.Error, new HttpAnalyzer());
