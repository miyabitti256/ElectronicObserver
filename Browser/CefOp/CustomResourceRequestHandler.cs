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

			// Why not check LiveAdmiralNameMask here: スクリーンショット用の preserveDrawingBuffer 注入のため index.php のフィルター自体は常時必要であり、マスクの要否判定はフィルター内部で行うため
			if (request.Url.Contains(@"/kcs2/index.php"))
			{
				string fontCss = conf?.EnableCustomGameFont == true
					? BrowserLib.GameFontHelper.GenerateFontCss(conf.GameFontJapanese, conf.GameFontLatin, conf.GameFontCustomCSS)
					: null;

				return new ResponseFilterPixiSetting(
					conf?.PreserveDrawingBuffer ?? true,
					conf?.LiveAdmiralNameMask ?? false,
					conf?.LiveAdmiralNameMaskText ?? "提督",
					conf?.EnableCustomGameFont ?? false,
					fontCss);
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
