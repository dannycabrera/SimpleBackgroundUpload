// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;
using System.CodeDom.Compiler;

namespace SimpleBackgroundUpload
{
	[Register ("UploadController")]
	partial class UploadController
	{
		[Outlet]
		MonoTouch.UIKit.UIButton button1 { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (button1 != null) {
				button1.Dispose ();
				button1 = null;
			}
		}
	}
}
