# Task

This file contains tasks to be executed.

1 - LocalJSONRepository and GoogleDriveJSONRepository are very close both inherited from InvestmentsRepositoryBase

I want apply a better design on this.

This is the idea

Create a 
public sealed class LocalJsonStorage : IJsonStorage
{
    // path via ctor
    public Task<string> ReadAsync(...) { /* File.ReadAllTextAsync */ }
    public Task WriteAsync(string json, ...) { /* File.WriteAllTextAsync */ }
}


Implement 
public sealed class LocalJsonStorage : IJsonStorage
{
    // path via ctor
    public Task<string> ReadAsync(...) { /* File.ReadAllTextAsync */ }
    public Task WriteAsync(string json, ...) { /* File.WriteAllTextAsync */ }
}

public sealed class GoogleDriveJsonStorage : IJsonStorage
{
    // drive file id + client via ctor
    public Task<string> ReadAsync(...) { /* download from Drive */ }
    public Task WriteAsync(string json, ...) { /* upload to Drive */ }
}

Go back to have one JSONRepository class, that need a IJsonStorage

Make the storage injection depending on the Repository:Provider

