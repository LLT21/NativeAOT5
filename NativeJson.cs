using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace tinykestrel
{
   public static class NativeJson //: JsonConverter<Dictionary<string, object>>
   {
      public static void Read(Stream body)
      {
         var buffer = new byte[4096];

         // Fill the buffer.
         // For this snippet, we're assuming the stream is open and has data.
         // If it might be closed or empty, check if the return value is 0.
         body.Read(buffer);

         Utf8JsonReader utf8JsonReader = new Utf8JsonReader(buffer, isFinalBlock: false, state: default);
         Console.WriteLine($"String in buffer is: {Encoding.UTF8.GetString(buffer)}");

         // Search for "Summary" property name
         while (utf8JsonReader.TokenType != JsonTokenType.PropertyName || !utf8JsonReader.ValueTextEquals("lastname"))
         {
            if (!utf8JsonReader.Read())
            {
               // Not enough of the JSON is in the buffer to complete a read.
               GetMoreBytesFromStream(body, ref buffer, ref utf8JsonReader);
            }
         }

         // Found the "Summary" property name.
         Console.WriteLine($"String in buffer is: {Encoding.UTF8.GetString(buffer)}");

         while (!utf8JsonReader.Read())
         {
            // Not enough of the JSON is in the buffer to complete a read.
            GetMoreBytesFromStream(body, ref buffer, ref utf8JsonReader);
         }

         // Display value of Summary property, that is, "Hot".
         Console.WriteLine($"Got property value: {utf8JsonReader.GetString()}");
      }

      private static void GetMoreBytesFromStream(Stream stream, ref byte[] buffer, ref Utf8JsonReader utf8JsonReader)
      {
         int bytesRead;

         if (utf8JsonReader.BytesConsumed < buffer.Length)
         {
            ReadOnlySpan<byte> leftover = buffer.AsSpan((int)utf8JsonReader.BytesConsumed);

            if (leftover.Length == buffer.Length)
            {
               Array.Resize(ref buffer, buffer.Length * 2);
               Console.WriteLine($"Increased buffer size to {buffer.Length}");
            }

            leftover.CopyTo(buffer);
            bytesRead = stream.Read(buffer.AsSpan(leftover.Length));
         }
         else
         {
            bytesRead = stream.Read(buffer);
         }

         Console.WriteLine($"String in buffer is: {Encoding.UTF8.GetString(buffer)}");
         utf8JsonReader = new Utf8JsonReader(buffer, isFinalBlock: bytesRead == 0, utf8JsonReader.CurrentState);
      }

      public static Dictionary<string, object> Read(ref Utf8JsonReader reader) //, Type typeToConvert, JsonSerializerOptions options)
      {
         Console.WriteLine($"TokenType={reader.TokenType}");

         while (reader.Read())
         {
            switch (reader.TokenType)
            {
               case JsonTokenType.StartObject:
               case JsonTokenType.EndObject:
               case JsonTokenType.StartArray:
               case JsonTokenType.EndArray:
                  Console.WriteLine($"TokenType={reader.TokenType}");
                  break;
               case JsonTokenType.String:
                  Console.WriteLine($"TokenType=String Value={reader.GetString()}");
                  break;
               case JsonTokenType.Number:
                  Console.WriteLine($"TokenType=Number Value={reader.GetInt32()}");
                  break;
               case JsonTokenType.PropertyName:
                  Console.WriteLine($"TokenType=PropertyName Value={reader.GetString()}");
                  break;
            }
         }

         return null;
      }

      public static void Write(Utf8JsonWriter writer, Dictionary<string, object> dictionary, JsonSerializerOptions options)
      {
         //writer.WriteStringValue(value.Message);
      }
   }
}