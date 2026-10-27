using System.Text;
using Honeycomb.Formats.Sound;
using RedFox.Audio.Wwise;
using RedFox.GameExtraction;
using RedFox.GameExtraction.CommandLine;
using RedFox.GameExtraction.Hashing;
using RedFox.IO.FileSystem;
using Spectre.Console;

namespace Honeycomb.Cli;

internal sealed class NamesCommand : ICommandLineCommand
{
    private const string BankSuffix			= ".sound_bank.resource";
    private const string EventSuffix		= ".sound_event.resource";
    private const string EventStatsSuffix	= ".events.csv";
    private const string EventColumn		= "event";

    public string Name => "names";

    public string Description => "Build the Wwise media name table from mounted sources";

    public async Task ExecuteAsync(CommandLineSession session, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        if (session.Manager.Sources.Count == 0)
        {
            throw new InvalidOperationException("No sources mounted");
        }

        VirtualFile[]				files			= [.. session.Manager.GetRequiredService<AssetFileSystemService>().FileSystem.Root.EnumerateFiles("*", SearchOption.AllDirectories)];
        WwiseMediaNameResolver		resolver		= new();
        Dictionary<uint, string>	eventNames		= [];
        int							bankCount		= 0;
        int							missingBanks	= 0;

        await session.CreateProgress().StartAsync(context =>
        {
            ProgressTask task = context.AddTask("Sound names", maxValue: files.Length);

            foreach (VirtualFile file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                task.Increment(1);

                if (file.Name.EndsWith(BankSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    SoundBankResource resource = SoundBankResource.Parse(ReadText(file));

                    if (file.Parent!.TryGetFile(resource.BankFile, out VirtualFile? bankFile))
                    {
                        resolver.AddBank(resource.Name, WwiseBank.Read(bankFile.ReadAllBytes()));
                        bankCount++;
                    }
                    else
                    {
                        missingBanks++;
                    }
                }
                else if (file.Name.EndsWith(EventSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    AddEventName(eventNames, SoundEventResource.Parse(ReadText(file)).Name);
                }
                else if (file.Name.EndsWith(EventStatsSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    AddEventStats(eventNames, ReadText(file));
                }
            }

            return Task.CompletedTask;
        });

        NameTableManager manager = session.Manager.GetRequiredService<NameListService>().Manager;

        if (manager.TryGetTable(WwiseMediaNameResolver.TableName, out NameTable? existing))
        {
            manager.Remove(existing);
        }

        NameTable table = manager.CreateNameTable(WwiseMediaNameResolver.TableName);

        foreach ((uint mediaId, string mediaName) in resolver.Resolve(eventNames))
        {
            table.Add(mediaId, mediaName);
        }

        string path = Path.Combine(HoneycombAssetManagerFactory.NameTablesDirectory, WwiseMediaNameResolver.TableName + ".namefile");

        Directory.CreateDirectory(HoneycombAssetManagerFactory.NameTablesDirectory);
        NameFile.Save(path, table, NameFileFlags.Checksum);

        if (missingBanks > 0)
        {
            session.WriteWarning($"{missingBanks:N0} sound banks were skipped, load the paks that contain them.");
        }

        session.WriteSuccess($"{WwiseMediaNameResolver.TableName} [{session.Theme.Muted}]· {bankCount:N0} banks · {eventNames.Count:N0} event names · {table.Count:N0} media names[/]");
    }

    private static void AddEventName(Dictionary<uint, string> eventNames, string name)
    {
        if (name.Length > 0)
        {
            eventNames[WwiseId.Compute(name)] = name;
        }
    }

    private static void AddEventStats(Dictionary<uint, string> eventNames, string text)
    {
        string[]	lines	= text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        int			column	= Array.IndexOf(lines[0].Split(','), EventColumn);

        foreach (string line in lines.Skip(1))
        {
            AddEventName(eventNames, line.Split(',')[column]);
        }
    }

    private static string ReadText(VirtualFile file) => Encoding.UTF8.GetString(file.ReadAllBytes());
}
