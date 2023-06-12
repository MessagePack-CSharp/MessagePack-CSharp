# Performance analysis

Build and use the release configuration of the project so you're measuring real perf with optimizations turned on:

    dotnet build -c release  .\sandbox\PerfNetFramework\

Use [PerfView](https://github.com/Microsoft/perfview/blob/master/documentation/Downloading.md) to analyze performance
to look for opportunities to improve.
When collecting ETL traces, use these settings in the Collect->Run dialog:

| Setting     | Value |
|-------------|-------|
| Command     | `dotnet run -c release -p .\sandbox\PerfNetFramework\ -f net472 --no-build`
| Current Dir | `d:\git\messagepack-csharp` (or wherever your enlistment is)
| Additional Providers | `*MessagePack-Benchmark`
| No V3.X NGen | Checked

Start your investigation using the Events window to find the scenario that you're interested in,
with these settings:

| Setting    | Value |
|------------|-------|
| Filter     | `MessagePack`
| Columns to display | `count DURATION_MSEC impl`

Select the two `Time MSec` values that bound the scenario you're interested in, right-click and select CPU Stacks.
