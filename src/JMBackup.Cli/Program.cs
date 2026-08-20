using JMBackup.Cli.Commands;

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

return await RunProgramAsync(args, cancellationTokenSource.Token).ConfigureAwait(false);

static async Task<int> RunProgramAsync(string[] arguments, CancellationToken cancellationToken)
{
    if (arguments is ["run", ..])
    {
        return await ExecuteRunAsync(arguments[1..], cancellationToken).ConfigureAwait(false);
    }

    if (arguments is ["credential", "protect", ..])
    {
        return ExecuteProtectCredential(arguments[2..]);
    }

    PrintUsage();
    return 1;
}

static async Task<int> ExecuteRunAsync(string[] arguments, CancellationToken cancellationToken)
{
    string? configPath = null;
    var dryRun = false;

    for (var i = 0; i < arguments.Length; i++)
    {
        switch (arguments[i])
        {
            case "--config" when i + 1 < arguments.Length:
                configPath = arguments[++i];
                break;
            case "--dry-run":
                dryRun = true;
                break;
        }
    }

    if (configPath is null)
    {
        Console.Error.WriteLine("Uso: jmbackup run --config tarea.json [--dry-run]");
        return 1;
    }

    return await RunCommand.ExecuteAsync(configPath, dryRun, cancellationToken).ConfigureAwait(false);
}

static int ExecuteProtectCredential(string[] arguments)
{
    string? username = null;

    for (var i = 0; i < arguments.Length; i++)
    {
        if (arguments[i] == "--username" && i + 1 < arguments.Length)
        {
            username = arguments[++i];
        }
    }

    if (username is null)
    {
        Console.Error.WriteLine("Uso: jmbackup credential protect --username usuario");
        return 1;
    }

    return ProtectCredentialCommand.Execute(username);
}

static void PrintUsage()
{
    Console.WriteLine("Uso:");
    Console.WriteLine("  jmbackup run --config tarea.json [--dry-run]");
    Console.WriteLine("  jmbackup credential protect --username usuario");
}
