using CefSharp;
using System;
using System.IO;
using System.Text;

namespace Browser.CefOp
{
	/// <summary>
	/// ゲーム画面の HTML を加工し、スクリーンショット用描画設定や提督名マスク用スクリプトを挿入します。
	/// </summary>
	public class ResponseFilterPixiSetting : IResponseFilter
	{
		private readonly bool preserveDrawingBuffer;
		private readonly bool maskEnabled;
		private readonly string maskText;
		private readonly bool customFontEnabled;
		private readonly string customFontCss;

		public ResponseFilterPixiSetting(
			bool preserveDrawingBuffer = true,
			bool maskEnabled = false,
			string maskText = "提督",
			bool customFontEnabled = false,
			string customFontCss = null)
		{
			this.preserveDrawingBuffer = preserveDrawingBuffer;
			this.maskEnabled = maskEnabled;
			this.maskText = maskText;
			this.customFontEnabled = customFontEnabled;
			this.customFontCss = customFontCss;
		}

		public bool InitFilter() => true;

		public FilterStatus Filter(Stream dataIn, out long dataInRead, Stream dataOut, out long dataOutWritten)
		{
			if (dataIn == null)
			{
				dataInRead = 0;
				dataOutWritten = 0;
				return FilterStatus.Done;
			}

			using (var reader = new StreamReader(dataIn))
			{
				string raw = reader.ReadToEnd();

				// Why not modify network/API responses: ゲーム通信データには一切触れず、表示直前のクライアント側描画層のみで完結させるため
				string replaced = BrowserLib.LiveAdmiralNameMaskHelper.InjectIntoHtml(
					raw,
					preserveDrawingBuffer,
					maskEnabled,
					maskText);

				if (customFontEnabled && !string.IsNullOrWhiteSpace(customFontCss))
				{
					replaced = BrowserLib.GameFontHelper.InjectIntoHtml(replaced, customFontCss);
				}

				var bytes = Encoding.UTF8.GetBytes(replaced);
				dataOut.Write(bytes, 0, bytes.Length);

				dataInRead = dataIn.Length;
				dataOutWritten = Math.Min(bytes.Length, dataOut.Length);
			}

			return FilterStatus.Done;
		}

		public void Dispose() { }
	}
}
