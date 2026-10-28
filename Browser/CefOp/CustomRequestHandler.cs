using CefSharp;
using CefSharp.Handler;
using System;

namespace Browser.CefOp
{
	public class CustomRequestHandler : RequestHandler
	{
		public delegate void RenderProcessTerminatedEventHandler(string message);
		public event RenderProcessTerminatedEventHandler RenderProcessTerminated;

		private readonly Func<BrowserLib.BrowserConfiguration> configProvider;

		public CustomRequestHandler(Func<BrowserLib.BrowserConfiguration> configProvider) : base()
		{
			this.configProvider = configProvider;
		}

		public CustomRequestHandler(bool pixiSettingEnabled) : base()
		{
			this.configProvider = () => new BrowserLib.BrowserConfiguration { PreserveDrawingBuffer = pixiSettingEnabled };
		}

		protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
		{
			return new CustomResourceRequestHandler(configProvider);
		}

		/// <summary>
		/// 戻る/進む操作をブロックします。
		/// </summary>
		protected override bool OnBeforeBrowse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
		{
			if ((request.TransitionType & TransitionType.ForwardBack) != 0)
			{
				return true;
			}
			return base.OnBeforeBrowse(browserControl, browser, frame, request, userGesture, isRedirect);
		}

		/// <summary>
		/// 描画プロセスが何らかの理由で落ちた際の処理を行います。
		/// </summary>
		protected override void OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status, int errorCode, string errorMessage)
		{
			string ret = "ブラウザの描画プロセスが";
			switch (status)
			{
				case CefTerminationStatus.AbnormalTermination:
					ret += "正常に終了しませんでした。";
					break;
				case CefTerminationStatus.ProcessWasKilled:
					ret += "何者かによって殺害されました。";
					break;
				case CefTerminationStatus.ProcessCrashed:
					ret += "クラッシュしました。";
					break;
				default:
					ret += "謎の死を遂げました。";
					break;
			}
			ret += "再読み込みすると復帰します。";

			RenderProcessTerminated?.Invoke(ret);
		}
	}
}
