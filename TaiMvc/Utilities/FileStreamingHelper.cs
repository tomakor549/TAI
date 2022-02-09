using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TaiMvc.Models;

namespace TaiMvc.Utilities
{
    public static class FileStreamingHelper
    {
        private static readonly FormOptions _defaultFormOptions = new FormOptions();

        public static async Task<string> StreamFiles(this HttpRequest request, string targetDirectory, int bufferSize)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(request.ContentType))
            {
                throw new Exception($"Expected a multipart request, but got {request.ContentType}");
            }
            string newFilePath = "";
            // Used to accumulate all the form url encoded key value pairs in the 
            // request.
            var formAccumulator = new KeyValueAccumulator();

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, request.Body);

            var section = await reader.ReadNextSectionAsync();//For reading Http First of the requests section data
            while (section != null)
            {
                ContentDispositionHeaderValue contentDisposition;
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);

                if (hasContentDispositionHeader)
                {
                    /*
                    section used to process the uploaded file type
                    -----------------------------99614912995
                    Content - Disposition: form - data; name = "files"; filename = "Misc 002.jpg"

                    ASAADSDSDJXCKDSDSDSHAUSAUASAASSDSDFDSFJHSIHFSDUIASUI+/==
                    -----------------------------99614912995
                    */
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        if (!Directory.Exists(targetDirectory))
                        {
                            Directory.CreateDirectory(targetDirectory);
                        }

                        var fileName = MultipartRequestHelper.GetFileName(contentDisposition);
                        var loadBufferBytes = bufferSize;//This is every time Http Requested section The size of the read file data in Byte That is, byte, which is set to 1024, which means that every time the Http Requested section Read and fetch 1024 bytes of data from the data stream to the server memory, and then write to the following targetFileStream This value can be adjusted according to the memory size of the server. This avoids loading the data of all uploaded files into the memory of the server at one time, leading to the server crash.
                        newFilePath = FileHelpers.GetNonExistPath(targetDirectory, fileName.ToString());

                        using (var targetFileStream = System.IO.File.Create(newFilePath))
                        {
                            //section.Body yes System.IO.Stream Type, which means Http One of the requests section Data stream from which each can be read section So we can not use the following data section.Body.CopyToAsync Method, but in a loop section.Body.Read Method (if section.Body.Read Method returns 0, indicating that the data flow has reached the end and all data has been read targetFileStream
                            await section.Body.CopyToAsync(targetFileStream, loadBufferBytes);
                        }

                    }
                    /*
                    section used to process form key data
                    -----------------------------99614912995
                    Content - Disposition: form - data; name = "SOMENAME"

                    Formulaire de Quota
                    -----------------------------99614912995
                    */
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        // Content-Disposition: form-data; name="key"
                        //
                        // value

                        // Do not limit the key name length here because the 
                        // multipart headers length limit is already in effect.
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = GetEncoding(section);
                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: bufferSize,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();
                            if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = String.Empty;
                            }
                            formAccumulator.Append(key.Value, value); // For .NET Core <2.0 remove ".Value" from key

                            if (formAccumulator.ValueCount > _defaultFormOptions.ValueCountLimit)
                            {
                                throw new InvalidDataException($"Form key count limit {_defaultFormOptions.ValueCountLimit} exceeded.");
                            }
                        }
                    }
                }

                // Drains any remaining section body that has not been consumed and
                // reads the headers for the next section.
                section = await reader.ReadNextSectionAsync();//For reading Http Next in request section data
            }
            return newFilePath;
        }

        public static async Task<string> StreamEncodingFiles(this HttpRequest request, string targetDirectory, string password, int bufferSize)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(request.ContentType))
            {
                throw new Exception($"Expected a multipart request, but got {request.ContentType}");
            }
            string newFilePath = "";
            // Used to accumulate all the form url encoded key value pairs in the 
            // request.
            var formAccumulator = new KeyValueAccumulator();

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, request.Body);

            var section = await reader.ReadNextSectionAsync();//For reading Http First of the requests section data
            while (section != null)
            {
                ContentDispositionHeaderValue contentDisposition;
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);

                if (hasContentDispositionHeader)
                {
                    /*
                    section used to process the uploaded file type
                    -----------------------------99614912995
                    Content - Disposition: form - data; name = "files"; filename = "Misc 002.jpg"

                    ASAADSDSDJXCKDSDSDSHAUSAUASAASSDSDFDSFJHSIHFSDUIASUI+/==
                    -----------------------------99614912995
                    */
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        if (!Directory.Exists(targetDirectory))
                        {
                            Directory.CreateDirectory(targetDirectory);
                        }

                        var fileName = MultipartRequestHelper.GetFileName(contentDisposition);
                        //This is every time Http Requested section
                        //The size of the read file data in Byte That is, byte, which is set to 1024,
                        //which means that every time the Http Requested section Read and fetch 1024 bytes of data
                        //from the data stream to the server memory,
                        //and then write to the following targetFileStream
                        //This value can be adjusted according to the memory size of the server.
                        //This avoids loading the data of all uploaded files into the memory of the server at one time,
                        //leading to the server crash.
                        var loadBufferBytes = bufferSize;
                        newFilePath = FileHelpers.GetNonExistPath(targetDirectory, fileName.ToString());
                        //section.Body yes System.IO.Stream Type,
                        //which means Http One of the requests section Data stream from which each can be read section
                        //So we can not use the following data section.Body.CopyToAsync Method,
                        //but in a loop section.Body.Read Method
                        //(if section.Body.Read Method returns 0, indicating that the data flow has reached the end and all data has been read targetFileStream

                        newFilePath = await FileEncryptionOperations.FileEncrypt(newFilePath, section.Body, password, new byte[loadBufferBytes]);

                    }
                    /*
                    section used to process form key data
                    -----------------------------99614912995
                    Content - Disposition: form - data; name = "SOMENAME"

                    Formulaire de Quota
                    -----------------------------99614912995
                    */
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        // Content-Disposition: form-data; name="key"
                        //
                        // value

                        // Do not limit the key name length here because the 
                        // multipart headers length limit is already in effect.
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = GetEncoding(section);
                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: bufferSize,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();
                            if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = String.Empty;
                            }
                            formAccumulator.Append(key.Value, value); // For .NET Core <2.0 remove ".Value" from key

                            if (formAccumulator.ValueCount > _defaultFormOptions.ValueCountLimit)
                            {
                                throw new InvalidDataException($"Form key count limit {_defaultFormOptions.ValueCountLimit} exceeded.");
                            }
                        }
                    }
                }

                // Drains any remaining section body that has not been consumed and
                // reads the headers for the next section.
                section = await reader.ReadNextSectionAsync();//For reading Http Next in request section data
            }
            return newFilePath;
        }

        private static Encoding GetEncoding(MultipartSection section)
        {
            MediaTypeHeaderValue mediaType;
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }
    }
}
