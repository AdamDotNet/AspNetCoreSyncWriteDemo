using CsvHelper;
using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AspNetCoreSyncWriteDemo
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                // 1. Demos needs for async dispose.
                endpoints.MapGet("/Dispose", async httpContext =>
                {
                    httpContext.Response.StatusCode = 200;
                    httpContext.Response.ContentType = "text/csv";
                    httpContext.Response.Headers.Add("Content-Disposition", "attachment;filename=demo.csv");

                    var records = new List<DemoCsvData>
                    {
                        new DemoCsvData { Column1 = 1, Column2 = "one" },
                        new DemoCsvData { Column1 = 2, Column2 = "two" },
                        new DemoCsvData { Column1 = 3, Column2 = "three" }
                    };

                    // NOTE: Does not call await using, allows CsvWriter to call synchronous Dispose.
                    var writer = new StreamWriter(httpContext.Response.Body, new UTF8Encoding(false));

                    // CRITICAL: When CsvWriter.Dispose is called, the above StreamWriter is also synchronously Disposed.
                    // Results: InvalidOperationException: Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true instead.
                    // Solution: Add IAsyncDisposable to CsvWriter/CsvReader and supporting types.
                    using var csvWriter = new CsvWriter(writer);
                    csvWriter.WriteHeader<DemoCsvData>();
                    await csvWriter.NextRecordAsync();
                    foreach (var record in records)
                    {
                        csvWriter.WriteRecord(record);
                        await csvWriter.NextRecordAsync();
                    }
                });

                // 2. Demos need for async WriteRecords
                endpoints.MapGet("/WriteRecords", async httpContext =>
                {
                    httpContext.Response.StatusCode = 200;
                    httpContext.Response.ContentType = "text/csv";
                    httpContext.Response.Headers.Add("Content-Disposition", "attachment;filename=demo.csv");

                    // Make collection big enough such that WriteRecords triggers a synchronous flush.
                    var records = new List<DemoCsvData>();
                    for (int i = 0; i < 1000; i++)
                    {
                        records.Add(new DemoCsvData
                        {
                            Column1 = i,
                            Column2 = $"Foo_{i}"
                        });
                    }

                    // Async Dispose writer manually, have CsvWriter pass leaveOpen: true in constructor to avoid synchronous dispose.
                    await using var writer = new StreamWriter(httpContext.Response.Body, new UTF8Encoding(false));

                    // CRITICAL: CsvWriter.WriteRecords ends up calling synchronous Flush when buffer is full.
                    // Results: InvalidOperationException: Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true instead.
                    // Solution: Add WriteRecordsAsync methods.
                    using var csvWriter = new CsvWriter(writer, leaveOpen: true);
                    csvWriter.WriteRecords(records);
                });
            });
        }

        private class DemoCsvData
        {
            public int Column1 { get; set; }

            public string Column2 { get; set; }
        }
    }
}
