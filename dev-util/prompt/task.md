# Task

This file contains tasks to be executed.

1 - At the Financial tools UI few improvements
- on the Operations's grid the quantity need 8 decimal places
- on the Operations's grid when a line is selected the colour is blue, but this colour does not work well with the column Type (green for buy and red for sell), find a more suitable colour 
- on the Summay there also the quantity should use 8 decimal places


2 - LocalJSONRepository and GoogleDriveJSONRepository are very close both inherited from InvestmentsRepositoryBase

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

