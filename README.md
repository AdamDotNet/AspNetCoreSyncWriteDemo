## Asp.Net Core 3.0 Synchronous IO Exceptions

### Dispose Synchronously
.Net Core 3.0 / .Net Standard 2.1 added [IAsyncDisposable](https://github.com/dotnet/corefx/blob/7216dfaeeab82fc3c2fc65f62b3f28346f76b532/src/Common/src/CoreLib/System/IAsyncDisposable.cs) type, and `TextWriter` implements it.
 - When `CsvWriter.Dispose` is called, its `TextWriter` is also synchronously Disposed.
 - When the `TextWriter` is a `StreamWriter` wrapping `HttpContext.Response.Body`, then `InvalidOperationException: Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true instead.` occurs.

### Flush Synchronously during WriteRecords
 - `CsvWriter.WriteRecords` ends up calling synchronous `Flush` on its` TextWriter` when buffer is full.
 - When the `TextWriter` is a `StreamWriter` wrapping `HttpContext.Response.Body`, then `InvalidOperationException: Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true instead.` occurs.
