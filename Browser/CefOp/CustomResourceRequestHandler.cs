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
			bool needFilter = conf != null && (conf.PreserveDrawingBuffer || conf.LiveAdmiralNameMask);

			if (needFilter && request.Url.Contains(@"/kcs2/index.php"))
			{
				return new ResponseFilterPixiSetting(
					conf.PreserveDrawingBuffer,
					conf.LiveAdmiralNameMask,
					conf.RealAdmiralName,
					conf.LiveAdmiralNameMaskText);
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
