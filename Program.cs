using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace tinykestrel
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var socketTransportOptions = new SocketTransportOptions();
            var socketTransportFactory = new SocketTransportFactory(Options.Create(socketTransportOptions), NullLoggerFactory.Instance);
            var kestrelServerOptions = new KestrelServerOptions();

            kestrelServerOptions.ListenLocalhost(5000);
            kestrelServerOptions.ApplicationServices = new ServiceProvider();
            kestrelServerOptions.ListenLocalhost(5001, listenOptions =>
            {
                var serverCertificate = new X509Certificate2("certificate.pfx", "xxxx");
                listenOptions.UseHttps(serverCertificate);
            });

            using var kestrelServer = new KestrelServer(Options.Create(kestrelServerOptions), socketTransportFactory, NullLoggerFactory.Instance);

            await kestrelServer.StartAsync(new HttpApplication(), CancellationToken.None);

            Console.WriteLine("Listening on:");
            foreach (var address in kestrelServer.Features.Get<IServerAddressesFeature>().Addresses)
            {
                Console.WriteLine(" - " + address);
            }

            Console.WriteLine("Process CTRL+C to quit");
            var wh = new ManualResetEventSlim();
            Console.CancelKeyPress += (sender, e) => wh.Set();
            wh.Wait();
        }

        private class ServiceProvider : ISupportRequiredService, IServiceProvider
        {
            public object GetRequiredService(Type serviceType)
            {
                return GetService(serviceType);
            }

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(ILoggerFactory))
                {
                    return NullLoggerFactory.Instance;
                }

                return null;
            }
        }
    }

    public class HttpApplication : IHttpApplication<HttpContext>
    {
        public HttpContext CreateContext(IFeatureCollection contextFeatures)
        {
            return new DefaultHttpContext(contextFeatures);
        }

        public void DisposeContext(HttpContext context, Exception exception)
        {
        }

        public async Task ProcessRequestAsync(HttpContext context)
        {
            bool emptyBody = false;

            var jsonObjects = new Dictionary<string, object>();

            // This uses reflection
            // jsonObjects = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(request.Body);

            // This has internal Assembly call
            // JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
            // serializerOptions.Converters.Add(new NativeJson());
            // jsonObjects = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(request.Body, serializerOptions);

            // More complex
            // NativeJson.Read(request.Body);

            try
            {
                using var document = await JsonDocument.ParseAsync(context.Request.Body);

                JsonElement root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty property in root.EnumerateObject())
                    {
                        jsonObjects.Add(property.Name, property.Value.ToString());
                    }

                    jsonObjects.Add("feedback", "your json has just gone through a native kestrel");
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement element in root.EnumerateArray())
                    {
                    }
                }
            }
            catch
            {
                emptyBody = true;
            }

            if (emptyBody)
            {
                jsonObjects.Add("firstname", "Natty");
                jsonObjects.Add("lastname", "de Balancet");
                jsonObjects.Add("info", "copy this json and post it back");
            }

            var jsonWriterOptions = new JsonWriterOptions
            {
                Indented = true
            };

            var memoryStream = new MemoryStream();

            using (var utf8JsonWriter = new Utf8JsonWriter(memoryStream, jsonWriterOptions))
            {
                utf8JsonWriter.WriteStartObject();

                foreach (KeyValuePair<string, object> keyValuePair in jsonObjects)
                {
                    utf8JsonWriter.WriteString(keyValuePair.Key, keyValuePair.Value.ToString());
                    //utf8JsonWriter.WriteNumber("temp", 42);
                }

                utf8JsonWriter.WriteEndObject();
                utf8JsonWriter.Flush();
                //string responseString = Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            /* Database call test
            string names = string.Empty;

            //Odbc pool test
            for (int i = 0; i < 2; i++)
            {
               using (IDbConnection dbConnection = new NativeConnection())
               {
                  names = OdbcPerformance.ReadContacts(dbConnection);
               }
            }

            jsonObjects.Add("names", names);
            */

            context.Response.ContentLength = memoryStream.Length;
            context.Response.ContentType = "application/json";

            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(context.Response.Body);
        }
    }
}