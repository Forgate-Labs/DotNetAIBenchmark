using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var options = CliOptions.Parse(args);

if (options.ShowHelp)
{
    CliOptions.PrintHelp();
    return 0;
}

var app = new BenchmarkApp(options);
return await app.RunAsync();

sealed class BenchmarkApp(CliOptions options)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<int> RunAsync()
    {
        if (options.BuildImage)
        {
            Console.WriteLine($"[docker] building image {options.Image} from {options.Dockerfile}");
            var build = await ProcessRunner.RunAsync("docker", ["build", "-f", options.Dockerfile, "-t", options.Image, "."], Directory.GetCurrentDirectory(), options.GlobalTimeout);
            Console.Write(build.Stdout);
            Console.Error.Write(build.Stderr);
            if (build.ExitCode != 0) return build.ExitCode;
        }

        if (options.Command == "auth-check")
        {
            return await RunAuthChecksAsync() ? 0 : 2;
        }

        var tasks = LoadTasks(options.Dataset);
        if (tasks.Count == 0)
        {
            Console.Error.WriteLine($"Dataset is empty: {options.Dataset}");
            return 1;
        }

        if (!options.SkipAuthCheck)
        {
            var authOk = await RunAuthChecksAsync();
            if (!authOk)
            {
                Console.Error.WriteLine("Auth check failed. Log in to pi on the host or configure API keys before running the benchmark.");
                return 2;
            }
        }

        var runId = $"run-{DateTimeOffset.UtcNow:yyyyMMdd}";
        var modelHistoryName = SafeName(string.Join("__", options.Models));
        var runDir = GetUniqueRunDirectory(options.ResultsDir, BenchmarkMetadata.Version, modelHistoryName, runId);
        Directory.CreateDirectory(runDir);

        var report = new BenchmarkReport
        {
            BenchmarkVersion = BenchmarkMetadata.Version,
            RunId = runId,
            StartedAt = DateTimeOffset.UtcNow,
            Image = options.Image,
            Dataset = Path.GetFullPath(options.Dataset),
            PiAgentDir = ResolvePiAgentDir(options.PiAgentDir),
            Controlled = options.Controlled,
            Results = []
        };

        foreach (var task in tasks)
        foreach (var model in options.Models)
        for (var rep = 1; rep <= options.Repetitions; rep++)
        {
            var result = await RunOneAsync(task, model, rep, runDir);
            report.Results.Add(result);
            WriteReport(runDir, report);
        }

        report.EndedAt = DateTimeOffset.UtcNow;
        WriteReport(runDir, report);
        WriteMarkdownSummary(runDir, report);

        Console.WriteLine($"[done] report: {Path.Combine(runDir, "report.json")}");
        return report.Results.All(r => r.Passed) ? 0 : 3;
    }

    private async Task<bool> RunAuthChecksAsync()
    {
        var ok = true;
        foreach (var model in options.Models)
        {
            Console.WriteLine($"[auth-check] {model}");
            var workspace = CreateTempWorkspace("auth-check");
            const string expectedResponse = "AUTH_CHECK_OK";
            var args = DockerArgs(workspace, [
                "pi", "--mode", "json", "--no-session", "--no-tools", "--no-context-files", "--no-skills", "--no-extensions",
                "--model", model,
                $"Respond exactly: {expectedResponse}"
            ]);

            var result = await ProcessRunner.RunAsync("docker", args, Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(90));
            var parser = ParsePiOutput(result.Stdout);
            var actualResponse = NormalizeAuthCheckResponse(parser.Metrics.AssistantText);
            var success = result.ExitCode == 0
                          && !result.TimedOut
                          && result.Stdout.Contains("agent_end", StringComparison.Ordinal)
                          && string.Equals(actualResponse, expectedResponse, StringComparison.Ordinal);
            Console.WriteLine(success ? $"[auth-check] OK {model}" : $"[auth-check] FAILED {model}");
            if (success && parser.Metrics.AssistantErrors.Count > 0)
                Console.WriteLine($"[auth-check] WARN {model}: transient provider errors before a valid response: {string.Join("; ", parser.Metrics.AssistantErrors)}");
            if (!success)
            {
                ok = false;
                Console.Error.WriteLine($"Expected assistant response: {expectedResponse}");
                Console.Error.WriteLine($"Actual assistant response: {actualResponse}");
                if (parser.Metrics.AssistantErrors.Count > 0)
                    Console.Error.WriteLine($"Provider errors: {string.Join("; ", parser.Metrics.AssistantErrors)}");
                Console.Error.WriteLine(result.Stderr);
                Console.Error.WriteLine(LastLines(result.Stdout, 20));
            }
            TryDelete(workspace);
        }
        return ok;
    }

    private async Task<ModelRunResult> RunOneAsync(BenchmarkTask task, string model, int repetition, string runDir)
    {
        var safeModel = SafeName(model);
        var workspace = Path.Combine(runDir, "workspaces", task.Id, safeModel, $"rep-{repetition}");
        Directory.CreateDirectory(Path.GetDirectoryName(workspace)!);
        CopyDirectory(Path.GetFullPath(task.Fixture), workspace);
        EnsureWorkspaceGitIgnore(workspace);
        await InitGitAsync(workspace);

        Console.WriteLine($"[run] task={task.Id} model={model} rep={repetition}");
        var timeout = TimeSpan.FromSeconds(task.TimeoutSeconds > 0 ? task.TimeoutSeconds : options.TaskTimeoutSeconds);
        var piArgs = new List<string>
        {
            "pi", "--mode", "json", "--no-session",
            "--model", model,
            "--tools", "read,bash,edit,write"
        };

        if (options.Controlled)
        {
            piArgs.AddRange(["--no-context-files", "--no-skills", "--no-extensions"]);
        }

        piArgs.Add(BuildPrompt(task));

        var overallStarted = DateTimeOffset.UtcNow;
        PiRunAttempt attempt = null!;
        var providerRetryErrors = new List<string>();
        for (var attemptNumber = 1; attemptNumber <= options.ProviderRetryAttempts; attemptNumber++)
        {
            attempt = await RunPiAttemptAsync(workspace, piArgs, timeout, attemptNumber);
            if (!attempt.HasProviderError || attemptNumber == options.ProviderRetryAttempts) break;

            providerRetryErrors.AddRange(attempt.ProviderErrors);
            Console.WriteLine($"[retry] provider error for task={task.Id} model={model} attempt={attemptNumber}/{options.ProviderRetryAttempts}: {string.Join("; ", attempt.Parser.Metrics.AssistantErrors)}");
            await ResetWorkspaceAsync(workspace);
        }

        var parser = attempt.Parser;
        var proc = attempt.Process;
        var started = overallStarted;
        var ended = attempt.EndedAt;

        var validation = await ValidateScenarioAsync(workspace, task, model, repetition, timeout);
        var diff = await ProcessRunner.RunAsync("git", ["diff", "--", "."], workspace, TimeSpan.FromSeconds(30));
        var diffMetrics = await CollectDiffMetricsAsync(workspace, diff.Stdout);
        await File.WriteAllTextAsync(Path.Combine(workspace, "diff-metrics.json"), JsonSerializer.Serialize(diffMetrics, JsonOptions));
        await File.WriteAllTextAsync(Path.Combine(workspace, "pi.stdout.jsonl"), proc.Stdout);
        await File.WriteAllTextAsync(Path.Combine(workspace, "pi.stderr.log"), proc.Stderr);
        await File.WriteAllTextAsync(Path.Combine(workspace, "diff.patch"), diff.Stdout);

        var result = new ModelRunResult
        {
            TaskId = task.Id,
            Category = task.Category,
            Model = model,
            Repetition = repetition,
            Workspace = workspace,
            StartedAt = started,
            EndedAt = ended,
            DurationMs = (long)(ended - started).TotalMilliseconds,
            TimeToFirstTokenMs = parser.FirstTokenAt is null ? null : (long)(parser.FirstTokenAt.Value - attempt.StartedAt).TotalMilliseconds,
            TimedOut = proc.TimedOut,
            PiExitCode = proc.ExitCode,
            PiMetrics = parser.Metrics,
            AgentAttempts = attempt.AttemptNumber,
            ProviderRetryErrors = providerRetryErrors,
            ProviderErrors = attempt.ProviderErrors,
            DiffMetrics = diffMetrics,
            Validation = validation,
            Passed = proc.ExitCode == 0 && !proc.TimedOut && validation.Passed
        };

        Console.WriteLine(result.Passed
            ? $"[pass] {task.Id} {model} rep={repetition}"
            : $"[fail] {task.Id} {model} rep={repetition} piExit={proc.ExitCode} validation={validation.Passed}");

        return result;
    }

    private async Task<PiRunAttempt> RunPiAttemptAsync(string workspace, IReadOnlyList<string> piArgs, TimeSpan timeout, int attemptNumber)
    {
        var parser = new PiJsonMetricsParser();
        var started = DateTimeOffset.UtcNow;
        var proc = await ProcessRunner.RunStreamingAsync(
            "docker",
            DockerArgs(workspace, piArgs),
            Directory.GetCurrentDirectory(),
            timeout,
            stdoutLine: line => parser.Observe(line));
        var ended = DateTimeOffset.UtcNow;

        await File.WriteAllTextAsync(Path.Combine(workspace, $"pi.attempt-{attemptNumber}.stdout.jsonl"), proc.Stdout);
        await File.WriteAllTextAsync(Path.Combine(workspace, $"pi.attempt-{attemptNumber}.stderr.log"), proc.Stderr);

        return new PiRunAttempt(attemptNumber, started, ended, proc, parser);
    }

    private static async Task ResetWorkspaceAsync(string workspace)
    {
        await ProcessRunner.RunAsync("git", ["reset", "--hard", "HEAD"], workspace, TimeSpan.FromSeconds(30));
        await ProcessRunner.RunAsync("git", ["clean", "-fdx", "-e", "pi.attempt-*.stdout.jsonl", "-e", "pi.attempt-*.stderr.log"], workspace, TimeSpan.FromSeconds(30));
    }

    private static async Task<DiffMetrics> CollectDiffMetricsAsync(string workspace, string diffText)
    {
        var nameStatus = await ProcessRunner.RunAsync("git", ["diff", "--name-status", "--", "."], workspace, TimeSpan.FromSeconds(30));
        var numstat = await ProcessRunner.RunAsync("git", ["diff", "--numstat", "--", "."], workspace, TimeSpan.FromSeconds(30));
        var packageLines = await ProcessRunner.RunAsync(
            "bash",
            ["-lc", "find . -name '*.csproj' -not -path '*/bin/*' -not -path '*/obj/*' -print0 | xargs -0 grep -h '<PackageReference' 2>/dev/null || true"],
            workspace,
            TimeSpan.FromSeconds(30));
        var projectCount = await ProcessRunner.RunAsync(
            "bash",
            ["-lc", "find . -name '*.csproj' -not -path '*/bin/*' -not -path '*/obj/*' | wc -l"],
            workspace,
            TimeSpan.FromSeconds(30));

        var metrics = new DiffMetrics
        {
            ChangedFiles = [],
            AddedPackages = [],
            ProjectCount = int.TryParse(projectCount.Stdout.Trim(), out var projects) ? projects : 0
        };

        var changed = new Dictionary<string, ChangedFileMetric>(StringComparer.Ordinal);
        foreach (var line in nameStatus.Stdout.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            var status = parts[0];
            var path = parts[^1];
            var item = new ChangedFileMetric
            {
                Path = path,
                Status = status,
                Extension = Path.GetExtension(path)
            };
            changed[path] = item;
        }

        foreach (var line in numstat.Stdout.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length < 3) continue;
            var path = parts[2];
            if (!changed.TryGetValue(path, out var item))
            {
                item = new ChangedFileMetric { Path = path, Status = "M", Extension = Path.GetExtension(path) };
                changed[path] = item;
            }

            item.AddedLines = int.TryParse(parts[0], out var added) ? added : 0;
            item.DeletedLines = int.TryParse(parts[1], out var deleted) ? deleted : 0;
        }

        metrics.ChangedFiles = changed.Values.OrderBy(file => file.Path, StringComparer.Ordinal).ToList();
        metrics.AddedFileCount = metrics.ChangedFiles.Count(file => file.Status.StartsWith('A'));
        metrics.ModifiedFileCount = metrics.ChangedFiles.Count(file => file.Status.StartsWith('M'));
        metrics.DeletedFileCount = metrics.ChangedFiles.Count(file => file.Status.StartsWith('D'));
        metrics.RenamedFileCount = metrics.ChangedFiles.Count(file => file.Status.StartsWith('R'));
        metrics.TotalChangedFileCount = metrics.ChangedFiles.Count;
        metrics.AddedLines = metrics.ChangedFiles.Sum(file => file.AddedLines);
        metrics.DeletedLines = metrics.ChangedFiles.Sum(file => file.DeletedLines);
        metrics.DiffBytes = Encoding.UTF8.GetByteCount(diffText);
        metrics.Extensions = metrics.ChangedFiles
            .GroupBy(file => string.IsNullOrWhiteSpace(file.Extension) ? "<none>" : file.Extension, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        metrics.AddedPackages = ExtractPackageReferences(packageLines.Stdout);
        return metrics;
    }

    private static List<PackageReferenceMetric> ExtractPackageReferences(string text)
    {
        var packages = new SortedDictionary<string, PackageReferenceMetric>(StringComparer.OrdinalIgnoreCase);
        var regex = new Regex("<PackageReference\\s+[^>]*Include=\\\"(?<id>[^\\\"]+)\\\"[^>]*(?:Version=\\\"(?<version>[^\\\"]+)\\\")?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        foreach (Match match in regex.Matches(text))
        {
            var id = match.Groups["id"].Value;
            var version = match.Groups["version"].Success ? match.Groups["version"].Value : string.Empty;
            if (!packages.ContainsKey(id)) packages[id] = new PackageReferenceMetric { Id = id, Version = version };
        }
        return packages.Values.ToList();
    }

    private async Task<ValidationResult> ValidateScenarioAsync(string workspace, BenchmarkTask task, string model, int repetition, TimeSpan timeout)
    {
        var validationContext = CreateValidationContext(workspace, task, model, repetition);
        var usesExplicitEvaluationItems = task.EvaluationItems.Length > 0;
        var items = GetEvaluationItems(task);
        var publicItems = items.Where(item => IsPublicSuite(item.Suite)).ToArray();
        var hiddenItems = items.Where(item => IsHiddenSuite(item.Suite)).ToArray();

        var publicEvaluations = await ValidateEvaluationItemsAsync(workspace, publicItems, timeout, validationContext, stopOnFailure: !usesExplicitEvaluationItems);
        var hiddenEvaluations = new List<EvaluationItemResult>();

        if (publicEvaluations.All(item => item.Passed) && hiddenItems.Length > 0)
        {
            var hiddenSource = Path.Combine(task.Fixture, "hidden-tests");
            if (Directory.Exists(hiddenSource))
            {
                CopyDirectory(hiddenSource, Path.Combine(workspace, "hidden-tests"));
            }

            hiddenEvaluations = await ValidateEvaluationItemsAsync(workspace, hiddenItems, timeout, validationContext, stopOnFailure: !usesExplicitEvaluationItems);
        }

        var evaluations = publicEvaluations.Concat(hiddenEvaluations).ToList();
        var commands = evaluations.Select(ToValidationCommandResult).ToList();
        var publicCommands = publicEvaluations.Select(ToValidationCommandResult).ToList();
        var hiddenCommands = hiddenEvaluations.Select(ToValidationCommandResult).ToList();
        var passed = evaluations.Count == items.Length && evaluations.All(item => item.Passed);
        var totalWeight = usesExplicitEvaluationItems ? items.Sum(item => item.Weight <= 0 ? 1 : item.Weight) : 1;
        var earnedWeight = usesExplicitEvaluationItems ? evaluations.Where(item => item.Passed).Sum(item => item.Weight) : (passed ? 1 : 0);

        return new ValidationResult
        {
            Passed = passed,
            EarnedWeight = earnedWeight,
            TotalWeight = totalWeight,
            EvaluationItems = evaluations,
            Commands = commands,
            PublicCommands = publicCommands,
            HiddenCommands = hiddenCommands
        };
    }

    private async Task<List<EvaluationItemResult>> ValidateEvaluationItemsAsync(string workspace, BenchmarkEvaluationItem[] items, TimeSpan defaultTimeout, ValidationContext validationContext, bool stopOnFailure)
    {
        var outputs = new List<EvaluationItemResult>();
        foreach (var item in items)
        {
            var timeout = TimeSpan.FromSeconds(item.TimeoutSeconds > 0 ? item.TimeoutSeconds : (int)defaultTimeout.TotalSeconds);
            var hostCommand = TryGetHostCommand(item.Command, out var shellCommand);
            Console.WriteLine($"[evaluate:{item.Suite}:{item.Id}{(hostCommand ? ":host" : ":docker")}] {shellCommand}");
            var started = DateTimeOffset.UtcNow;
            var result = hostCommand
                ? await ProcessRunner.RunAsync(
                    "bash",
                    ["-lc", shellCommand],
                    workspace,
                    timeout,
                    validationContext.Environment)
                : await ProcessRunner.RunAsync(
                    "docker",
                    DockerArgs(workspace, ["bash", "-lc", shellCommand]),
                    Directory.GetCurrentDirectory(),
                    timeout);
            var ended = DateTimeOffset.UtcNow;
            var passed = result.ExitCode == 0 && !result.TimedOut;
            var weight = item.Weight <= 0 ? 1 : item.Weight;
            outputs.Add(new EvaluationItemResult
            {
                Id = item.Id,
                Name = item.Name,
                Suite = item.Suite,
                Command = item.Command,
                Weight = weight,
                EarnedWeight = passed ? weight : 0,
                ExitCode = result.ExitCode,
                TimedOut = result.TimedOut,
                DurationMs = (long)(ended - started).TotalMilliseconds,
                StdoutTail = LastLines(result.Stdout, 80),
                StderrTail = LastLines(result.Stderr, 80),
                Passed = passed
            });

            if (!passed && stopOnFailure) break;
        }

        return outputs;
    }

    private static ValidationCommandResult ToValidationCommandResult(EvaluationItemResult item) => new()
    {
        Suite = item.Suite,
        Command = item.Command,
        ExitCode = item.ExitCode,
        TimedOut = item.TimedOut,
        DurationMs = item.DurationMs,
        StdoutTail = item.StdoutTail,
        StderrTail = item.StderrTail
    };

    private static BenchmarkEvaluationItem[] GetEvaluationItems(BenchmarkTask task)
    {
        if (task.EvaluationItems.Length > 0)
        {
            return task.EvaluationItems.Select((item, index) => item.Normalized(index + 1)).ToArray();
        }

        return task.ValidationCommands.Select((command, index) => new BenchmarkEvaluationItem
            {
                Id = $"public-{index + 1}",
                Name = $"Public validation {index + 1}",
                Suite = "public",
                Command = command,
                Weight = 1
            })
            .Concat(task.HiddenValidationCommands.Select((command, index) => new BenchmarkEvaluationItem
            {
                Id = $"hidden-{index + 1}",
                Name = $"Hidden validation {index + 1}",
                Suite = "hidden",
                Command = command,
                Weight = 1
            }))
            .ToArray();
    }

    private static bool IsPublicSuite(string? suite) => !IsHiddenSuite(suite);

    private static bool IsHiddenSuite(string? suite) => string.Equals(suite?.Trim(), "hidden", StringComparison.OrdinalIgnoreCase);

    private List<string> DockerArgs(string workspace, IReadOnlyList<string> command)
    {
        var args = new List<string> { "run", "--rm" };
        args.AddRange(["-v", $"{Path.GetFullPath(workspace)}:/workspace"]);
        args.AddRange(["-w", "/workspace"]);
        args.AddRange(["-e", "PI_SKIP_VERSION_CHECK=1", "-e", "PI_TELEMETRY=0", "-e", "DOTNET_CLI_TELEMETRY_OPTOUT=1", "-e", "DOTNET_NOLOGO=1"]);

        var piAgentDir = ResolvePiAgentDir(options.PiAgentDir);
        if (options.AuthMode == "mount")
        {
            if (!Directory.Exists(piAgentDir))
                throw new DirectoryNotFoundException($"PI agent dir does not exist: {piAgentDir}");

            args.AddRange(["-v", $"{piAgentDir}:/pi-agent"]);
            args.AddRange(["-e", "PI_CODING_AGENT_DIR=/pi-agent"]);
        }

        var codePassDir = ResolveCodePassDir();
        if (codePassDir is not null)
        {
            args.AddRange(["-v", $"{codePassDir}:/codepass:ro"]);
            args.AddRange(["-e", "CODEPASS_DIR=/codepass"]);
        }

        if (options.AuthMode is "mount" or "env")
        {
            foreach (var name in KnownProviderEnvVars.Names)
            {
                var value = Environment.GetEnvironmentVariable(name);
                if (string.IsNullOrWhiteSpace(value)) continue;

                if (name == "GOOGLE_APPLICATION_CREDENTIALS" && File.Exists(value))
                {
                    args.AddRange(["-v", $"{Path.GetFullPath(value)}:/tmp/google-application-credentials.json:ro"]);
                    args.AddRange(["-e", "GOOGLE_APPLICATION_CREDENTIALS=/tmp/google-application-credentials.json"]);
                }
                else
                {
                    args.AddRange(["-e", name]);
                }
            }
        }

        args.Add(options.Image);
        args.AddRange(command);
        return args;
    }

    private static string BuildPrompt(BenchmarkTask task) =>
        $"""
        You are in a disposable workspace inside a Docker container.

        Task: {task.Prompt}

        Rules:
        - You may use read, bash, edit, and write.
        - Do not ask the user for confirmation.
        - Run the validation commands when appropriate.
        - If a .NET project exists, run `dotnet list package --vulnerable --include-transitive` before finishing and avoid vulnerable package versions.
        - Stop when the task is complete.
        - At the end, respond with a short summary in English.
        """;

    private static List<BenchmarkTask> LoadTasks(string dataset)
    {
        var tasks = new List<BenchmarkTask>();
        foreach (var line in File.ReadLines(dataset))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#')) continue;
            var task = JsonSerializer.Deserialize<BenchmarkTask>(line, JsonOptions)
                       ?? throw new InvalidOperationException($"Invalid line in {dataset}: {line}");
            task.Fixture = Path.GetFullPath(task.Fixture);
            tasks.Add(task);
        }
        return tasks;
    }

    private static void WriteReport(string runDir, BenchmarkReport report)
    {
        report.EarnedEvaluationWeight = report.Results.Sum(r => r.Validation.EarnedWeight);
        report.TotalEvaluationWeight = report.Results.Sum(r => r.Validation.TotalWeight);
        File.WriteAllText(Path.Combine(runDir, "report.json"), JsonSerializer.Serialize(report, JsonOptions));
    }

    private static void WriteMarkdownSummary(string runDir, BenchmarkReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {report.RunId}");
        sb.AppendLine();
        sb.AppendLine($"Benchmark version: `{report.BenchmarkVersion}`");
        sb.AppendLine();
        sb.AppendLine("| Task | Model | Rep | Passed | Eval points | Files | Lines +/- | Projects | Packages | Attempts | Provider errors | Duration ms | TTFT ms | Tool calls | Bash | Tokens in | Tokens out | Cost |");
        sb.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var r in report.Results)
        {
            var providerErrorCount = r.ProviderRetryErrors.Count + r.ProviderErrors.Count;
            sb.AppendLine($"| {r.TaskId} | `{r.Model}` | {r.Repetition} | {r.Passed} | {r.Validation.EarnedWeight:0.##}/{r.Validation.TotalWeight:0.##} | {r.DiffMetrics.TotalChangedFileCount} | +{r.DiffMetrics.AddedLines}/-{r.DiffMetrics.DeletedLines} | {r.DiffMetrics.ProjectCount} | {r.DiffMetrics.AddedPackages.Count} | {r.AgentAttempts} | {providerErrorCount} | {r.DurationMs} | {r.TimeToFirstTokenMs?.ToString() ?? ""} | {r.PiMetrics.ToolCalls} | {r.PiMetrics.BashCalls} | {r.PiMetrics.InputTokens} | {r.PiMetrics.OutputTokens} | {r.PiMetrics.TotalCost:0.####} |");
        }

        var earnedWeight = report.Results.Sum(r => r.Validation.EarnedWeight);
        var totalWeight = report.Results.Sum(r => r.Validation.TotalWeight);
        sb.AppendLine();
        sb.AppendLine($"Evaluation points: `{earnedWeight:0.##}/{totalWeight:0.##}`");
        File.WriteAllText(Path.Combine(runDir, "summary.md"), sb.ToString());
    }

    private static async Task InitGitAsync(string workspace)
    {
        await ProcessRunner.RunAsync("git", ["init"], workspace, TimeSpan.FromSeconds(30));
        await ProcessRunner.RunAsync("git", ["add", "."], workspace, TimeSpan.FromSeconds(30));
        await ProcessRunner.RunAsync("git", ["-c", "user.email=benchmark@example.local", "-c", "user.name=Benchmark", "commit", "-m", "baseline"], workspace, TimeSpan.FromSeconds(30));
    }

    private static string? ResolveCodePassDir()
    {
        var raw = Environment.GetEnvironmentVariable("CODEPASS_DIR")
                  ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CodePass");
        if (raw.StartsWith("~/", StringComparison.Ordinal))
            raw = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), raw[2..]);

        var fullPath = Path.GetFullPath(raw);
        return Directory.Exists(fullPath) ? fullPath : null;
    }

    private static string ResolvePiAgentDir(string? configured)
    {
        var localAgentDir = Path.Combine(Directory.GetCurrentDirectory(), ".pi-agent-benchmark");
        var raw = configured
                  ?? Environment.GetEnvironmentVariable("PI_CODING_AGENT_DIR")
                  ?? (Directory.Exists(localAgentDir) ? localAgentDir : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pi", "agent"));
        if (raw.StartsWith("~/", StringComparison.Ordinal))
            raw = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), raw[2..]);
        return Path.GetFullPath(raw);
    }

    private static string SafeName(string value) => Regex.Replace(value, "[^a-zA-Z0-9_.-]+", "_").Trim('_');

    private static string SafeDockerName(string value) => Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9_-]+", "_").Trim('_', '-');

    private static string ShortHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..12];

    private static bool TryGetHostCommand(string command, out string shellCommand)
    {
        const string prefix = "host:";
        if (command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            shellCommand = command[prefix.Length..].TrimStart();
            return true;
        }

        shellCommand = command;
        return false;
    }

    private static ValidationContext CreateValidationContext(string workspace, BenchmarkTask task, string model, int repetition)
    {
        var id = SafeDockerName($"{task.Id}-{SafeName(model)}-rep-{repetition}-{ShortHash(Path.GetFullPath(workspace))}");
        if (string.IsNullOrWhiteSpace(id)) id = $"validation_{ShortHash(workspace)}";

        return new ValidationContext(new Dictionary<string, string>
        {
            ["DOTNET_AI_BENCHMARK_VALIDATION_ID"] = id,
            ["DOTNET_AI_BENCHMARK_DOCKER_IMAGE_TAG"] = $"dotnet-ai-benchmark-validation-{id}:latest",
            ["COMPOSE_PROJECT_NAME"] = $"dotnet_ai_benchmark_{id.Replace('-', '_')}",
            ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
            ["DOTNET_NOLOGO"] = "1"
        });
    }

    private static string GetUniqueRunDirectory(string resultsDir, string benchmarkVersion, string modelHistoryName, string runId)
    {
        var baseDir = Path.GetFullPath(Path.Combine(resultsDir, $"benchmark-{benchmarkVersion}", modelHistoryName));
        var candidate = Path.Combine(baseDir, runId);
        if (!Directory.Exists(candidate)) return candidate;

        for (var index = 2; ; index++)
        {
            candidate = Path.Combine(baseDir, $"{runId}-{index}");
            if (!Directory.Exists(candidate)) return candidate;
        }
    }

    private static string CreateTempWorkspace(string name)
    {
        var dir = Path.Combine(Path.GetTempPath(), "dotnet-ai-benchmark", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CopyDirectory(string source, string destination)
    {
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
        Directory.CreateDirectory(destination);

        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            if (ShouldSkipPath(source, dir, isDirectory: true)) continue;
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, dir)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            if (ShouldSkipPath(source, file, isDirectory: false)) continue;
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static bool ShouldSkipPath(string root, string path, bool isDirectory)
    {
        var relative = Path.GetRelativePath(root, path);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => part is "bin" or "obj" or ".git" or ".vs" or "hidden-tests")
               || (!isDirectory && parts.Any(part => part.EndsWith(".user", StringComparison.OrdinalIgnoreCase)));
    }

    private static void EnsureWorkspaceGitIgnore(string workspace)
    {
        var gitIgnorePath = Path.Combine(workspace, ".gitignore");
        var existing = File.Exists(gitIgnorePath) ? File.ReadAllText(gitIgnorePath) : string.Empty;
        var required = new[]
        {
            "bin/",
            "obj/",
            "**/bin/",
            "**/obj/",
            ".vs/",
            "*.user",
            "TestResults/",
            "**/TestResults/",
            "codepass-quality.json",
            "diff-metrics.json"
        };

        var builder = new StringBuilder(existing);
        if (builder.Length > 0 && !existing.EndsWith('\n')) builder.AppendLine();
        foreach (var entry in required)
        {
            if (!existing.Split('\n').Any(line => line.Trim() == entry)) builder.AppendLine(entry);
        }
        File.WriteAllText(gitIgnorePath, builder.ToString());
    }

    private static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { }
    }

    private static PiJsonMetricsParser ParsePiOutput(string stdout)
    {
        var parser = new PiJsonMetricsParser();
        foreach (var line in stdout.Replace("\r\n", "\n").Split('\n')) parser.Observe(line);
        return parser;
    }

    private static string NormalizeAuthCheckResponse(string text) => text.Trim().Trim('"', '`');

    private static string LastLines(string text, int count)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        return string.Join('\n', lines.Skip(Math.Max(0, lines.Length - count)));
    }
}

static class BenchmarkMetadata
{
    public const string Version = "0.2.0";
}

sealed class PiJsonMetricsParser
{
    public PiMetrics Metrics { get; } = new();
    public DateTimeOffset? FirstTokenAt { get; private set; }

    public void Observe(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeProp)) return;
            var type = typeProp.GetString();

            if (type == "message_update" && root.TryGetProperty("assistantMessageEvent", out var ev))
            {
                var evType = ev.GetProperty("type").GetString();
                if (FirstTokenAt is null && evType is "text_delta" or "thinking_delta") FirstTokenAt = DateTimeOffset.UtcNow;
                if (evType == "text_delta" && ev.TryGetProperty("delta", out var delta)) Metrics.AssistantText += delta.GetString();
            }
            else if (type == "tool_execution_start")
            {
                Metrics.ToolCalls++;
                if (root.TryGetProperty("toolName", out var toolName) && toolName.GetString() == "bash") Metrics.BashCalls++;
            }
            else if (type == "tool_execution_end")
            {
                if (root.TryGetProperty("isError", out var isError) && isError.GetBoolean()) Metrics.ToolErrors++;
            }
            else if (type == "message_end" && root.TryGetProperty("message", out var message))
            {
                if (message.TryGetProperty("usage", out var usage))
                {
                    Metrics.InputTokens += ReadLong(usage, "input");
                    Metrics.OutputTokens += ReadLong(usage, "output");
                    Metrics.CacheReadTokens += ReadLong(usage, "cacheRead");
                    Metrics.CacheWriteTokens += ReadLong(usage, "cacheWrite");
                    if (usage.TryGetProperty("cost", out var cost) && cost.TryGetProperty("total", out var total)) Metrics.TotalCost += total.GetDecimal();
                }

                if (message.TryGetProperty("role", out var role)
                    && role.GetString() == "assistant"
                    && message.TryGetProperty("stopReason", out var stopReason)
                    && stopReason.GetString() == "error")
                {
                    var error = message.TryGetProperty("errorMessage", out var errorMessage)
                        ? errorMessage.GetString()
                        : "Unknown provider error";
                    Metrics.AssistantErrors.Add(error ?? "Unknown provider error");
                }
            }
        }
        catch
        {
            Metrics.MalformedJsonLines++;
        }
    }

    private static long ReadLong(JsonElement element, string name) =>
        element.TryGetProperty(name, out var prop) && prop.TryGetInt64(out var value) ? value : 0;
}

static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout, IReadOnlyDictionary<string, string>? environment = null) =>
        await RunStreamingAsync(fileName, args, workingDirectory, timeout, stdoutLine: null, environment);

    public static async Task<ProcessResult> RunStreamingAsync(string fileName, IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout, Action<string>? stdoutLine, IReadOnlyDictionary<string, string>? environment = null)
    {
        using var cts = new CancellationTokenSource(timeout);
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        using var process = new Process();
        process.StartInfo.FileName = fileName;
        foreach (var arg in args) process.StartInfo.ArgumentList.Add(arg);
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                process.StartInfo.Environment[key] = value;
            }
        }

        process.Start();

        var stdoutTask = ReadLinesAsync(process.StandardOutput, line =>
        {
            stdout.AppendLine(line);
            stdoutLine?.Invoke(line);
        });
        var stderrTask = ReadLinesAsync(process.StandardError, line => stderr.AppendLine(line));

        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            TryKill(process);
        }

        await Task.WhenAll(stdoutTask, stderrTask);
        return new ProcessResult(process.ExitCode, timedOut, stdout.ToString(), stderr.ToString());
    }

    private static async Task ReadLinesAsync(StreamReader reader, Action<string> onLine)
    {
        string? line;
        while ((line = await reader.ReadLineAsync()) is not null) onLine(line);
    }

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
    }
}

sealed record ProcessResult(int ExitCode, bool TimedOut, string Stdout, string Stderr);

sealed record ValidationContext(IReadOnlyDictionary<string, string> Environment);

sealed record PiRunAttempt(int AttemptNumber, DateTimeOffset StartedAt, DateTimeOffset EndedAt, ProcessResult Process, PiJsonMetricsParser Parser)
{
    public bool HasProviderError => ProviderErrors.Count > 0;
    public List<string> ProviderErrors => Parser.Metrics.AssistantErrors;
}

sealed class CliOptions
{
    public string Command { get; init; } = "run";
    public string Dataset { get; init; } = "datasets/poc-dotnet-tasks.jsonl";
    public string ResultsDir { get; init; } = "results";
    public string Image { get; init; } = "dotnet-ai-benchmark-pi:local";
    public string Dockerfile { get; init; } = "docker/pi-runner.Dockerfile";
    public string? PiAgentDir { get; init; }
    public string AuthMode { get; init; } = "mount";
    public string[] Models { get; init; } = [];
    public int Repetitions { get; init; } = 1;
    public int TaskTimeoutSeconds { get; init; } = 600;
    public bool BuildImage { get; init; }
    public bool SkipAuthCheck { get; init; }
    public bool Controlled { get; init; } = true;
    public int ProviderRetryAttempts { get; init; } = 3;
    public bool ShowHelp { get; init; }
    public TimeSpan GlobalTimeout => TimeSpan.FromSeconds(Math.Max(300, TaskTimeoutSeconds));

    public static CliOptions Parse(string[] args)
    {
        var command = args.FirstOrDefault(a => !a.StartsWith('-')) is "auth-check" ? "auth-check" : "run";
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = command == "auth-check" ? 1 : 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--")) continue;
            var key = arg[2..];
            if (key is "build-image" or "skip-auth-check" or "help" or "controlled" or "realistic")
            {
                flags.Add(key);
                continue;
            }
            if (i + 1 >= args.Length) throw new ArgumentException($"Missing value for option: {arg}");
            dict[key] = args[++i];
        }

        var models = Split(dict.GetValueOrDefault("models") ?? Environment.GetEnvironmentVariable("PI_BENCHMARK_MODELS") ?? "");
        return new CliOptions
        {
            Command = command,
            Dataset = dict.GetValueOrDefault("dataset") ?? "datasets/poc-dotnet-tasks.jsonl",
            ResultsDir = dict.GetValueOrDefault("results-dir") ?? "results",
            Image = dict.GetValueOrDefault("image") ?? "dotnet-ai-benchmark-pi:local",
            Dockerfile = dict.GetValueOrDefault("dockerfile") ?? "docker/pi-runner.Dockerfile",
            PiAgentDir = dict.GetValueOrDefault("pi-agent-dir"),
            AuthMode = dict.GetValueOrDefault("auth-mode") ?? "mount",
            Models = models,
            Repetitions = int.Parse(dict.GetValueOrDefault("repetitions") ?? "1"),
            TaskTimeoutSeconds = int.Parse(dict.GetValueOrDefault("timeout-seconds") ?? "600"),
            BuildImage = flags.Contains("build-image"),
            SkipAuthCheck = flags.Contains("skip-auth-check"),
            Controlled = !flags.Contains("realistic"),
            ProviderRetryAttempts = int.Parse(dict.GetValueOrDefault("provider-retries") ?? "3"),
            ShowHelp = flags.Contains("help") || models.Length == 0
        };
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""
        DotNetAIBenchmark.Cli - pi + Docker proof of concept

        Usage:
          dotnet run --project src/DotNetAIBenchmark.Cli -- --models "anthropic/claude-sonnet-4-5:high" --build-image
          dotnet run --project src/DotNetAIBenchmark.Cli -- auth-check --models "openai/gpt-5:high"

        Options:
          --models             Comma-separated model list. Required.
          --dataset            Task JSONL file. Default: datasets/poc-dotnet-tasks.jsonl
          --repetitions        Repetitions per task/model. Default: 1
          --results-dir        Output directory. Default: results
          --image              Docker image. Default: dotnet-ai-benchmark-pi:local
          --dockerfile         Dockerfile. Default: docker/pi-runner.Dockerfile
          --build-image        Build the image before running.
          --pi-agent-dir       pi agent directory to mount. Default: PI_CODING_AGENT_DIR, .pi-agent-benchmark, or ~/.pi/agent
          --auth-mode          mount|env|none. Default: mount
          --skip-auth-check    Do not run an authentication dry run before the tasks.
          --provider-retries   Attempts per scenario when provider errors occur. Default: 3
          --realistic          Load pi skills/extensions/context files. Default is controlled, without skills/extensions/context files.

        Validation commands run inside Docker by default. Prefix a validation command with 'host:' to run it on the host from the workspace directory. Host validation receives unique DOTNET_AI_BENCHMARK_VALIDATION_ID, DOTNET_AI_BENCHMARK_DOCKER_IMAGE_TAG, and COMPOSE_PROJECT_NAME values to avoid Docker resource conflicts during parallel runs.
        """);
    }

    private static string[] Split(string value) => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}

static class KnownProviderEnvVars
{
    public static readonly string[] Names =
    [
        "ANTHROPIC_API_KEY", "OPENAI_API_KEY", "AZURE_OPENAI_API_KEY", "AZURE_OPENAI_ENDPOINT",
        "GOOGLE_API_KEY", "GEMINI_API_KEY", "GOOGLE_GENERATIVE_AI_API_KEY", "GOOGLE_APPLICATION_CREDENTIALS",
        "DEEPSEEK_API_KEY", "GROQ_API_KEY", "CEREBRAS_API_KEY", "OPENROUTER_API_KEY",
        "MISTRAL_API_KEY", "XAI_API_KEY", "VERCEL_AI_GATEWAY_API_KEY", "FIREWORKS_API_KEY",
        "HF_TOKEN", "HUGGINGFACE_API_KEY", "CLOUDFLARE_API_TOKEN", "CLOUDFLARE_ACCOUNT_ID"
    ];
}

sealed class BenchmarkTask
{
    public string Id { get; set; } = "";
    public string Category { get; set; } = "";
    public string Fixture { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string[] ValidationCommands { get; set; } = [];
    public string[] HiddenValidationCommands { get; set; } = [];
    public BenchmarkEvaluationItem[] EvaluationItems { get; set; } = [];
    public int TimeoutSeconds { get; set; } = 600;
}

sealed class BenchmarkEvaluationItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Suite { get; set; } = "public";
    public string Command { get; set; } = "";
    public decimal Weight { get; set; } = 1;
    public int TimeoutSeconds { get; set; }

    public BenchmarkEvaluationItem Normalized(int index) => new()
    {
        Id = string.IsNullOrWhiteSpace(Id) ? $"evaluation-{index}" : Id,
        Name = string.IsNullOrWhiteSpace(Name) ? (string.IsNullOrWhiteSpace(Id) ? $"Evaluation {index}" : Id) : Name,
        Suite = string.IsNullOrWhiteSpace(Suite) ? "public" : Suite.Trim().ToLowerInvariant(),
        Command = Command,
        Weight = Weight <= 0 ? 1 : Weight,
        TimeoutSeconds = TimeoutSeconds
    };
}

sealed class BenchmarkReport
{
    public string BenchmarkVersion { get; set; } = BenchmarkMetadata.Version;
    public string RunId { get; set; } = "";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string Image { get; set; } = "";
    public string Dataset { get; set; } = "";
    public string PiAgentDir { get; set; } = "";
    public bool Controlled { get; set; }
    public decimal EarnedEvaluationWeight { get; set; }
    public decimal TotalEvaluationWeight { get; set; }
    public List<ModelRunResult> Results { get; set; } = [];
}

sealed class ModelRunResult
{
    public string TaskId { get; set; } = "";
    public string Category { get; set; } = "";
    public string Model { get; set; } = "";
    public int Repetition { get; set; }
    public string Workspace { get; set; } = "";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public long DurationMs { get; set; }
    public long? TimeToFirstTokenMs { get; set; }
    public bool TimedOut { get; set; }
    public int PiExitCode { get; set; }
    public PiMetrics PiMetrics { get; set; } = new();
    public int AgentAttempts { get; set; } = 1;
    public List<string> ProviderRetryErrors { get; set; } = [];
    public List<string> ProviderErrors { get; set; } = [];
    public DiffMetrics DiffMetrics { get; set; } = new();
    public ValidationResult Validation { get; set; } = new();
    public bool Passed { get; set; }
}

sealed class PiMetrics
{
    public int ToolCalls { get; set; }
    public int BashCalls { get; set; }
    public int ToolErrors { get; set; }
    public int MalformedJsonLines { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long CacheReadTokens { get; set; }
    public long CacheWriteTokens { get; set; }
    public decimal TotalCost { get; set; }
    public List<string> AssistantErrors { get; set; } = [];
    public string AssistantText { get; set; } = "";
}

sealed class DiffMetrics
{
    public int TotalChangedFileCount { get; set; }
    public int AddedFileCount { get; set; }
    public int ModifiedFileCount { get; set; }
    public int DeletedFileCount { get; set; }
    public int RenamedFileCount { get; set; }
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
    public int DiffBytes { get; set; }
    public int ProjectCount { get; set; }
    public Dictionary<string, int> Extensions { get; set; } = [];
    public List<PackageReferenceMetric> AddedPackages { get; set; } = [];
    public List<ChangedFileMetric> ChangedFiles { get; set; } = [];
}

sealed class ChangedFileMetric
{
    public string Path { get; set; } = "";
    public string Status { get; set; } = "";
    public string Extension { get; set; } = "";
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
}

sealed class PackageReferenceMetric
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
}

sealed class ValidationResult
{
    public bool Passed { get; set; }
    public decimal EarnedWeight { get; set; }
    public decimal TotalWeight { get; set; }
    public List<EvaluationItemResult> EvaluationItems { get; set; } = [];
    public List<ValidationCommandResult> Commands { get; set; } = [];
    public List<ValidationCommandResult> PublicCommands { get; set; } = [];
    public List<ValidationCommandResult> HiddenCommands { get; set; } = [];
}

sealed class EvaluationItemResult
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Suite { get; set; } = "";
    public string Command { get; set; } = "";
    public decimal Weight { get; set; }
    public decimal EarnedWeight { get; set; }
    public int ExitCode { get; set; }
    public bool TimedOut { get; set; }
    public long DurationMs { get; set; }
    public string StdoutTail { get; set; } = "";
    public string StderrTail { get; set; } = "";
    public bool Passed { get; set; }
}

sealed class ValidationCommandResult
{
    public string Suite { get; set; } = "";
    public string Command { get; set; } = "";
    public int ExitCode { get; set; }
    public bool TimedOut { get; set; }
    public long DurationMs { get; set; }
    public string StdoutTail { get; set; } = "";
    public string StderrTail { get; set; } = "";
}
