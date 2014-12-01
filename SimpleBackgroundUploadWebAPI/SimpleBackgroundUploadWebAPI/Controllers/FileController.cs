using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace SimpleBackgroundUploadWebAPI.Controllers
{
    public class FileController : ApiController
    {
        [AcceptVerbs("POST")]
        public Task<HttpResponseMessage> PostFile()
        {
            String fileName, filePath;

            // TODO: Set your local file path
            filePath = @"C:\Temp\";

            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.UnsupportedMediaType));
            else
            {
                try
                {
                    fileName = Request.Headers.GetValues("FileName").First();
                }
                catch { throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "File name missing.")); }
            }

            var provider = new MultipartFormDataStreamProvider(filePath);

            // Read the form data and return an async task.
            var task = Request.Content.ReadAsMultipartAsync(provider).
            ContinueWith<HttpResponseMessage>(readTask =>
            {
                try
                {
                    #region Check for any Fault/Cancel
                    if (readTask.IsFaulted || readTask.IsCanceled)
                    {
                        if (readTask.IsFaulted)
                            Console.WriteLine("PostFile IsFaulted");
                        if (readTask.IsCanceled)
                            Console.WriteLine("PostFile IsCanceled");

                        if (readTask.Exception == null)
                            Console.WriteLine("PostFile: No readTask.Exception");
                        else
                        {
                            Console.WriteLine("PostFile Exception: " + readTask.Exception.StackTrace);
                            if (readTask.Exception.InnerException != null)
                                Console.WriteLine("PostFile InnerException: " + readTask.Exception.InnerException.StackTrace);
                        }

                        return Request.CreateResponse(HttpStatusCode.InternalServerError);
                    }
                    #endregion

                    foreach (var file in provider.FileData)
                    {
                        Console.WriteLine("Server file path: {0}", file.LocalFileName);

                        // Rename file since web api renames upload file
                        String sourcePath = Path.Combine(filePath, file.LocalFileName);
                        String destPath = Path.Combine(filePath, fileName);

                        if (File.Exists(destPath))
                            File.Delete(destPath);

                        Console.WriteLine("File rename from {0} to {1}", file.LocalFileName, fileName);
                        File.Move(sourcePath, destPath);
                    }

                    return Request.CreateResponse(HttpStatusCode.Created);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("PostFile Exception: {0}", ex.Message);
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
            });

            return task;
        }
    }
}
