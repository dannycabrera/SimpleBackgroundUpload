using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace SimpleBackgroundUpload
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations
		UIWindow window;
		NSUrlSession session;
		int taskId;
		string webApiAddress;

		//
		// This method is invoked when the application has loaded and is ready to run. In this
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			// create a new window instance based on the screen size
			window = new UIWindow (UIScreen.MainScreen.Bounds);
			
			// If you have defined a root view controller, set it here:
			// window.RootViewController = myViewController;

			// Enter your webAPI address below
			webApiAddress = "http://YourWebAPIAddress/SimpleBackgroundUploadWebAPI/File/PostFile";

			window.RootViewController = new UploadController ();

			// make the window visible
			window.MakeKeyAndVisible ();
			
			return true;
		}

		/// <Docs>Reference to the UIApplication that invoked this delegate method.</Docs>
		/// <remarks>Application are allocated approximately 5 seconds to complete this method. Application developers should use this
		/// time to save user data and tasks, and remove sensitive information from the screen.</remarks>
		/// <altmember cref="M:MonoTouch.UIKit.UIApplicationDelegate.WillEnterForeground"></altmember>
		/// <summary>
		/// Dids the enter background.
		/// </summary>
		/// <param name="application">Application.</param>
		public override void DidEnterBackground (UIApplication application)
		{
			Console.WriteLine("DidEnterBackground called...");

			// Ask iOS for additional background time and prepare upload.
			taskId = application.BeginBackgroundTask (delegate {
				if (taskId != 0) {
					application.EndBackgroundTask(taskId);
					taskId = 0;
				}
			});

			new System.Action (async delegate {

				await PrepareUpload();

				application.BeginInvokeOnMainThread(delegate {
					if (taskId != 0) {
						application.EndBackgroundTask(taskId);
						taskId = 0;
					}
				});

			}).BeginInvoke (null, null);
		}

		/// <summary>
		/// Prepares the upload.
		/// </summary>
		/// <returns>The upload.</returns>
		public async Task PrepareUpload()
		{
			try {
				Console.WriteLine("PrepareUpload called...");

				if (session == null)
					session = InitBackgroundSession();

				// Check if task already exits
				var tsk = await GetPendingTask();
				if (tsk != null) {
					Console.WriteLine ("TaskId {0} found, state: {1}", tsk.TaskIdentifier, tsk.State);

					// If our task is suspended, resume it.
					if (tsk.State == NSUrlSessionTaskState.Suspended) {
						Console.WriteLine ("Resuming taskId {0}...", tsk.TaskIdentifier);
						tsk.Resume();
					}

					return; // exit, we already have a task
				}

				// For demo purposes file is attached to project as "Content" and PDF is 8.1MB.
				var fileToUpload = "UIKitUICatalog.pdf";

				if(File.Exists(fileToUpload)) {
					var boundary = "FileBoundary";
					var bodyPath = Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BodyData.tmp");

					// Create request
					NSUrl uploadHandleUrl = NSUrl.FromString (webApiAddress);
					NSMutableUrlRequest request = new NSMutableUrlRequest (uploadHandleUrl);
					request.HttpMethod = "POST";
					request ["Content-Type"] = "multipart/form-data; boundary=" + boundary;
					request ["FileName"] = Path.GetFileName(fileToUpload);

					// Construct the body
					System.Text.StringBuilder sb = new System.Text.StringBuilder("");
					sb.AppendFormat("--{0}\r\n", boundary);
					sb.AppendFormat("Content-Disposition: form-data; name=\"file\"; filename=\"{0}\"\r\n", Path.GetFileName(fileToUpload));
					sb.Append("Content-Type: application/octet-stream\r\n\r\n");

					// Delete any previous body data file
					if (File.Exists(bodyPath))
						File.Delete(bodyPath);

					// Write file to BodyPart
					var fileBytes = File.ReadAllBytes (fileToUpload);
					using (var writeStream = new FileStream (bodyPath, FileMode.Create, FileAccess.Write, FileShare.Read)) {
						writeStream.Write (Encoding.Default.GetBytes (sb.ToString ()), 0, sb.Length);
						writeStream.Write (fileBytes, 0, fileBytes.Length);

						sb.Clear ();
						sb.AppendFormat ("\r\n--{0}--\r\n", boundary);
						writeStream.Write (Encoding.Default.GetBytes (sb.ToString ()), 0, sb.Length);
					}
					sb = null;
					fileBytes = null;

					// Creating upload task
					var uploadTask = session.CreateUploadTask(request, NSUrl.FromFilename(bodyPath));
					Console.WriteLine ("New TaskID: {0}", uploadTask.TaskIdentifier);

					// Start task
					uploadTask.Resume (); 
				}
				else
				{
					Console.WriteLine ("Upload file doesn't exist. File: {0}", fileToUpload);
				}	
			} catch (Exception ex) {
				Console.WriteLine ("PrepareUpload Ex: {0}", ex.Message);
			}
		}

		/// <summary>
		/// Gets the pending task.
		/// </summary>
		/// <returns>The pending task.</returns>
		/// <remarks>For demo purposes we are only starting a single task so that's why we are returning only one.</remarks>
		private async Task<NSUrlSessionUploadTask> GetPendingTask()
		{
			NSUrlSessionUploadTask uploadTask = null;

			if (session != null) {
				var tasks = await session.GetTasksAsync ();

				var taskList = tasks.UploadTasks;
				if (taskList.Count () > 0)
					uploadTask = taskList [0];
			}

			return uploadTask;
		}

		/// <summary>
		/// Processes the completed task.
		/// </summary>
		/// <param name="sessionTask">Session task.</param>
		public void ProcessCompletedTask(NSUrlSessionTask sessionTask)
		{
			try {
				Console.WriteLine (string.Format("Task ID: {0}, State: {1}, Response: {2}", sessionTask.TaskIdentifier, sessionTask.State, sessionTask.Response));

				// Make sure that we have a response to process
				if (sessionTask.Response == null || sessionTask.Response.ToString() == "")
				{
					Console.WriteLine("ProcessCompletedTask no response...");
				} 
				else
				{
					// Get response
					var resp = (NSHttpUrlResponse)sessionTask.Response;

					// Check that our task completed and server returned StatusCode 201 = CREATED.
					if (sessionTask.State == NSUrlSessionTaskState.Completed && resp.StatusCode == 201) 
					{
						// Do something with the uploaded file...
					}
				}
			} catch (Exception ex) {
				Console.WriteLine ("ProcessCompletedTask Ex: {0}", ex.Message);
			}
		}

		/// <summary>
		/// Initializes the background session.
		/// </summary>
		/// <returns>The background session.</returns>
		public NSUrlSession InitBackgroundSession()
		{
			// See URL below for configuration options
			// https://developer.apple.com/library/ios/documentation/Foundation/Reference/NSURLSessionConfiguration_class/index.html

			// Use same identifier for background tasks so in case app terminiated, iOS can resume tasks when app relaunches.
			string identifier = "MyBackgroundTaskId";

			using (var config = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration (identifier)) {
				config.HttpMaximumConnectionsPerHost = 4; //iOS Default is 4
				config.TimeoutIntervalForRequest = 600.0; //30min allowance; iOS default is 60 seconds.
				config.TimeoutIntervalForResource = 120.0; //2min; iOS Default is 7 days
				return NSUrlSession.FromConfiguration (config, new UploadDelegate (), new NSOperationQueue ());
			}
		}
	}

	public class UploadDelegate : NSUrlSessionTaskDelegate
	{
		// Called by iOS when the task finished trasferring data. It's important to note that his is called even when there isn't an error.
		// See: https://developer.apple.com/library/ios/documentation/Foundation/Reference/NSURLSessionTaskDelegate_protocol/index.html#//apple_ref/occ/intfm/NSURLSessionTaskDelegate/URLSession:task:didCompleteWithError:
		public override void DidCompleteWithError (NSUrlSession session, NSUrlSessionTask task, NSError error)
		{
			Console.WriteLine (string.Format("DidCompleteWithError TaskId: {0}{1}", task.TaskIdentifier, (error == null ? "" : " Error: " + error.Description)));

			if (error == null) {
				var appDel = UIApplication.SharedApplication.Delegate as AppDelegate;
				appDel.ProcessCompletedTask (task);
			}
		}

		// Called by iOS when session has been invalidated.
		// See: https://developer.apple.com/library/ios/documentation/Foundation/Reference/NSURLSessionDelegate_protocol/index.html#//apple_ref/occ/intfm/NSURLSessionDelegate/URLSession:didBecomeInvalidWithError:
		public override void DidBecomeInvalid (NSUrlSession session, NSError error)
		{
			Console.WriteLine ("DidBecomeInvalid" + (error == null ? "undefined" : error.Description));
		}

		// Called by iOS when all messages enqueued for a session have been delivered.
		// See: https://developer.apple.com/library/ios/documentation/Foundation/Reference/NSURLSessionDelegate_protocol/index.html#//apple_ref/occ/intfm/NSURLSessionDelegate/URLSessionDidFinishEventsForBackgroundURLSession:
		public override void DidFinishEventsForBackgroundSession (NSUrlSession session)
		{
			Console.WriteLine ("DidFinishEventsForBackgroundSession");
		}

		// Called by iOS to periodically inform the progress of sending body content to the server.
		// See: https://developer.apple.com/library/ios/documentation/Foundation/Reference/NSURLSessionTaskDelegate_protocol/index.html#//apple_ref/occ/intfm/NSURLSessionTaskDelegate/URLSession:task:didSendBodyData:totalBytesSent:totalBytesExpectedToSend:
		public override void DidSendBodyData (NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
		{
			// Uncomment line below to see file upload progress outputed to the console. You can track/manage this in your app to monitor the upload progress.
			//Console.WriteLine ("DidSendBodyData bSent: {0}, totalBSent: {1} totalExpectedToSend: {2}", bytesSent, totalBytesSent, totalBytesExpectedToSend);
		}
	}
}

