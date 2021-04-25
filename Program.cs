using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
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
         NullLoggerFactory nullLoggerFactory = new NullLoggerFactory();
         SocketTransportOptions socketTransportOptions = new SocketTransportOptions();
         SocketTransportFactory socketTransportFactory = new SocketTransportFactory(
                                new OptionsWrapper<SocketTransportOptions>(socketTransportOptions), nullLoggerFactory);
         KestrelServerOptions kestrelServerOptions = new KestrelServerOptions();

         //kestrelServerOptions.AllowSynchronousIO = true;
         kestrelServerOptions.ListenLocalhost(5000);
         kestrelServerOptions.ApplicationServices = new ServiceProvider();
         kestrelServerOptions.ListenLocalhost(5001, listenOptions =>
         {
            X509Certificate2 serverCertificate = new X509Certificate2("certificate.pfx", "xxxx");
            listenOptions.UseHttps(serverCertificate);
         });

         using (KestrelServer kestrelServer =
                              new KestrelServer(new OptionsWrapper<KestrelServerOptions>(kestrelServerOptions),
                                                socketTransportFactory,
                                                nullLoggerFactory
                                               )
               )
         {
            await kestrelServer.StartAsync(new HttpApplication(), CancellationToken.None);
            Console.ReadLine();
         }
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

   public class HttpApplication : IHttpApplication<IFeatureCollection>
   {
      public IFeatureCollection CreateContext(IFeatureCollection contextFeatures)
      {
         return contextFeatures;
      }

      public void DisposeContext(IFeatureCollection context, Exception exception)
      {
      }

      public async Task ProcessRequestAsync(IFeatureCollection features)
      {
         bool emptyBody = false;
         IHttpRequestFeature request = (IHttpRequestFeature)features[typeof(IHttpRequestFeature)];
         IHttpResponseFeature response = (IHttpResponseFeature)features[typeof(IHttpResponseFeature)];
         IHttpResponseBodyFeature responseBody = (IHttpResponseBodyFeature)features[typeof(IHttpResponseBodyFeature)];

         Dictionary<string, object> jsonObjects = new Dictionary<string, object>();

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
            using (StreamReader streamReader = new StreamReader(request.Body))
            {
               //string requestBodyString = await streamReader.ReadToEndAsync();
               //byte[] utf8JsonBytes = Encoding.UTF8.GetBytes(requestBodyString);
               //Utf8JsonReader utf8JsonReader = new Utf8JsonReader(utf8JsonBytes);

               using (JsonDocument document = await JsonDocument.ParseAsync(streamReader.BaseStream))
               {
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

         JsonWriterOptions jsonWriterOptions = new JsonWriterOptions
         {
            Indented = true
         };

         MemoryStream memoryStream = new MemoryStream();

         using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(memoryStream, jsonWriterOptions))
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

         //byte[] responseBodyBytes = Encoding.UTF8.GetBytes(responseString);
         response.Headers.ContentLength = memoryStream.Length; //responseBodyBytes.Length;
         response.Headers.Add("Content-Type", "application/json");
         await responseBody.Stream.WriteAsync(memoryStream.ToArray(), 0, (int)memoryStream.Length);
      }
   }
}