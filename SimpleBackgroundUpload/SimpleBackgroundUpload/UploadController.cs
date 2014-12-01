
using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.IO;

namespace SimpleBackgroundUpload
{
	public partial class UploadController : UIViewController
	{
		public UploadController () : base ("UploadController", null)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			button1.SetTitle("Start file upload", UIControlState.Normal);
			button1.TouchUpInside += async delegate 
			{
				var appDel = UIApplication.SharedApplication.Delegate as AppDelegate;
				await appDel.PrepareUpload();
			};
		}
	}
}

