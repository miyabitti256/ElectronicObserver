using CefSharp;
using CefSharp.Handler;
using System;

namespace Browser.CefOp
{
	public class CustomResourceRequestHandler : ResourceRequestHandler
	{
		private readonly Func<BrowserLib.BrowserConfiguration> configProvider;

		public CustomResourceRequestHandler(Func<BrowserLib.BrowserConfiguration> configProvider) : base()
		{
			this.configProvider = configProvider;
		}

		public CustomResourceRequestHandler(bool pixiSettingEnabled) : base()
		{
			this.configProvider = () => new BrowserLib.BrowserConfiguration { PreserveDrawingBuffer = pixiSettingEnabled };
		}

		/// <summary>
		/// レスポンスの置換制御を行います。
		/// </summary>
		protected override IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
		{
			var conf = configProvider?.Invoke();

			// Why not require conf.LiveAdmiralNameMask == true: 起動時にOFFであってもフック機構自体を先行注入しておき、後からONになった際に即時反映可能にするため
			if (request.Url.Contains(@"/kcs2/index.php"))
			{
				return new ResponseFilterPixiSetting(
					conf?.PreserveDrawingBuffer ?? true,
					conf?.LiveAdmiralNameMask ?? false,
					conf?.RealAdmiralName ?? "",
					conf?.LiveAdmiralNameMaskText ?? "提督");
			}

			return base.GetResourceResponseFilter(chromiumWebBrowser, browser, frame, request, response);
		}

		/// <summary>
		/// 特定の通信をブロックします。
		/// </summary>
		protected override CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
		{
			// ログイン直後に勝手に遷移させられ、ブラウザがホワイトアウトすることがあるためブロックする
			if (request.Url.Contains(@"/rt.gsspat.jp/"))
			{
				return CefReturnValue.Cancel;
			}

			return base.OnBeforeResourceLoad(chromiumWebBrowser, browser, frame, request, callback);
		}
	}
}
