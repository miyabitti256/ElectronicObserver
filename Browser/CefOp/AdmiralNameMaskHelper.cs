using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Browser.CefOp
{
	/// <summary>
	/// スクリーンショット撮影時に提督名や司令部情報をマスキングするヘルパーです。
	/// </summary>
	public static class AdmiralNameMaskHelper
	{
		/// <summary>
		/// 母港右上領域（鋼材・ボーキサイトアイコン）の各UIスキン基準 dHash テーブル
		/// </summary>
		private static readonly uint[] BasisHashes0 = new uint[]
		{
			0x4A049A2F, // クラシック (黒)
			0x2E449AAA, // メタル (グレー)
			0xD36598A9, // プレーン (白)
			0x00988e66, // 旧環境互換
		};

		private static readonly uint[] BasisHashes1 = new uint[]
		{
			0x15928B9F,
			0x39924B5C,
			0x489ACB5C,
			0x71888e46,
		};

		public static Bitmap ProcessScreenShot(
			Bitmap original,
			bool maskAdmiralName,
			bool keepHQLevel,
			int maskMode,
			bool maskOnlyOnHomeport)
		{
			if (!maskAdmiralName || original == null)
				return original;

			if (maskOnlyOnHomeport && !IsHomeport(original))
				return original;

			int w = original.Width;
			int h = original.Height;

			// モード 2: 切り取り (画面上部 45px クロップ)
			if (maskMode == 2)
			{
				int cutPosY = (int)Math.Round(0.0625 * h);
				if (cutPosY > 0 && cutPosY < h)
				{
					var cropped = new Bitmap(w, h - cutPosY, original.PixelFormat);
					using (var g = Graphics.FromImage(cropped))
					{
						g.DrawImage(original, new Rectangle(0, 0, w, h - cutPosY), new Rectangle(0, cutPosY, w, h - cutPosY), GraphicsUnit.Pixel);
					}
					original.Dispose();
					return cropped;
				}
				return original;
			}

			// 提督名矩形: (170/1200 ~ 410/1200, Y=0 ~ 36/720)
			var nameRect = new Rectangle(
				(int)Math.Round(w * (170.0 / 1200.0)),
				0,
				(int)Math.Round(w * (240.0 / 1200.0)),
				(int)Math.Round(h * (36.0 / 720.0)));

			// 司令部Lv矩形: (548/1200 ~ 655/1200, Y=14/720 ~ 34/720)
			// Why not include '艦隊司令部Lv.' text: 固定ラベルである青緑文字を残し、可変の個人情報である数字(Lv)と[階級]のみを自然に隠すため
			// Why not Y=0 ~ 36: 司令部Lv上部(Y=0~13)には金色の装飾ラインが存在し、Y=0から塗りつぶすと装飾が削れてしまうため、文字領域(Y=14~34)の高さのみを対象とする
			var lvRect = new Rectangle(
				(int)Math.Round(w * (548.0 / 1200.0)),
				(int)Math.Round(h * (14.0 / 720.0)),
				(int)Math.Round(w * (107.0 / 1200.0)),
				(int)Math.Round(h * (20.0 / 720.0)));

			if (maskMode == 1) // 背景色塗りつぶし（黒塗り）
			{
				// Why not fixed black: 艦これのUIスキンやヘッダー背景色(#202020等)と完全一致させて塗り境界を目立たせないため動的サンプリング色を使用する
				// Why not share bg color: スキンやグラデーションによって提督名枠と司令部枠で背景色が微妙に異なる場合があるため個別にサンプリングする
				Color bgName = GetDominantColor(original, nameRect);
				using (var g = Graphics.FromImage(original))
				{
					using (var brush = new SolidBrush(bgName))
					{
						g.FillRectangle(brush, nameRect);
					}
					if (!keepHQLevel)
					{
						Color bgLv = GetDominantColor(original, lvRect);
						using (var brush = new SolidBrush(bgLv))
						{
							g.FillRectangle(brush, lvRect);
						}
					}
				}
			}
			else // モザイク (mode == 0)
			{
				ApplyMosaic(original, nameRect, 0.11);
				if (!keepHQLevel)
				{
					ApplyMosaic(original, lvRect, 0.11);
				}
			}

			return original;
		}

		private static void ApplyMosaic(Bitmap bmp, Rectangle rect, double scale)
		{
			rect.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
			if (rect.Width <= 0 || rect.Height <= 0)
				return;

			int sw = Math.Max(1, (int)Math.Round(rect.Width * scale));
			int sh = Math.Max(1, (int)Math.Round(rect.Height * scale));

			using (var small = new Bitmap(sw, sh, bmp.PixelFormat))
			{
				using (var gSmall = Graphics.FromImage(small))
				{
					gSmall.InterpolationMode = InterpolationMode.NearestNeighbor;
					gSmall.PixelOffsetMode = PixelOffsetMode.Half;
					gSmall.DrawImage(bmp, new Rectangle(0, 0, sw, sh), rect, GraphicsUnit.Pixel);
				}

				using (var gOrig = Graphics.FromImage(bmp))
				{
					gOrig.InterpolationMode = InterpolationMode.NearestNeighbor;
					gOrig.PixelOffsetMode = PixelOffsetMode.Half;
					gOrig.DrawImage(small, rect, new Rectangle(0, 0, sw, sh), GraphicsUnit.Pixel);
				}
			}
		}

		/// <summary>
		/// 母港画面であるかどうかを dHash (Difference Hash) および青緑ラベル検出により判定します。
		/// </summary>
		public static bool IsHomeport(Bitmap image)
		{
			if (image == null) return false;

			int w = image.Width;
			int h = image.Height;

			// 母港右上のアイコン領域: X=1092/1200, Y=46/720, W=30/1200, H=50/720
			int sx = (int)Math.Round(w * (1092.0 / 1200.0));
			int sy = (int)Math.Round(h * (46.0 / 720.0));
			int sw = (int)Math.Round(w * (30.0 / 1200.0));
			int sh = (int)Math.Round(h * (50.0 / 720.0));

			if (sx + sw <= w && sy + sh <= h && sw > 0 && sh > 0)
			{
				try
				{
					using (var thumb = new Bitmap(8, 8, PixelFormat.Format32bppArgb))
					{
						using (var g = Graphics.FromImage(thumb))
						{
							g.InterpolationMode = InterpolationMode.Bilinear;
							g.PixelOffsetMode = PixelOffsetMode.Half;
							g.DrawImage(image, new Rectangle(0, 0, 8, 8), new Rectangle(sx, sy, sw, sh), GraphicsUnit.Pixel);
						}

						// 64ピクセル (8x8) の輝度値を計算
						int[] lums = new int[64];
						int idx = 0;
						for (int y = 0; y < 8; y++)
						{
							for (int x = 0; x < 8; x++)
							{
								Color c = thumb.GetPixel(x, y);
								lums[idx++] = ((c.B * 18 + c.G * 158 + c.R * 80) >> 8) & 0xFF;
							}
						}

						uint hash0 = 0;
						uint hash1 = 0;
						for (int i = 0; i < 63; i++)
						{
							bool bit = lums[i] > lums[i + 1];
							if (i < 32)
							{
								hash0 = (hash0 << 1) | (bit ? 1u : 0u);
							}
							else
							{
								hash1 = (hash1 << 1) | (bit ? 1u : 0u);
							}
						}

						// Why not single skin hash: UIスキン(黒・グレー・白)によって資材アイコン領域の描画が異なるため各スキンのハッシュと比較する
						for (int i = 0; i < BasisHashes0.Length; i++)
						{
							int dist = PopCount(hash0 ^ BasisHashes0[i]) + PopCount(hash1 ^ BasisHashes1[i]);
							if (dist < 12)
								return true;
						}
					}
				}
				catch
				{
					// Why not throw: 画像解析のエラーでスクショ撮影自体を失敗させないため安全にフォールバックへ進む
				}
			}

			// スキン非依存フォールバック: 「艦隊司令部Lv.」の青緑文字(シアン色)検出 (X: 420..540, Y: 15..25)
			// Why not dHash only: 新規スキン追加時や僅かなレンダリング差異時でも固定ヘッダーの青緑文字で確実に判定するため
			try
			{
				int cyanCount = 0;
				int xStart = (int)Math.Round(w * (420.0 / 1200.0));
				int xEnd = (int)Math.Round(w * (540.0 / 1200.0));
				int yStart = (int)Math.Round(h * (15.0 / 720.0));
				int yEnd = (int)Math.Round(h * (25.0 / 720.0));

				if (xEnd <= w && yEnd <= h && xStart < xEnd && yStart < yEnd)
				{
					for (int y = yStart; y <= yEnd; y++)
					{
						for (int x = xStart; x <= xEnd; x++)
						{
							Color c = image.GetPixel(x, y);
							if (c.G > 60 && c.B > 60 && c.G > c.R * 1.15 && c.B > c.R * 1.15)
							{
								cyanCount++;
								if (cyanCount >= 10)
									return true;
							}
						}
					}
				}
			}
			catch
			{
				// Why not throw: 画像解析エラーで例外を伝播させない
			}

			return false;
		}

		private static int PopCount(uint x)
		{
			x = (x & 0x55555555) + ((x >> 1) & 0x55555555);
			x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
			x = (x & 0x0f0f0f0f) + ((x >> 4) & 0x0f0f0f0f);
			x = (x & 0x00ff00ff) + ((x >> 8) & 0x00ff00ff);
			return (int)((x & 0x0000ffff) + ((x >> 16) & 0x0000ffff));
		}

		/// <summary>
		/// 指定領域の最頻色（ヘッダー背景色）をサンプリングします。
		/// UIスキンによって背景色が異なる場合でも周囲に馴染む色を動的に取得します。
		/// </summary>
		private static Color GetDominantColor(Bitmap bmp, Rectangle rect)
		{
			rect.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
			if (rect.Width <= 0 || rect.Height <= 0)
				return Color.FromArgb(32, 32, 32); // Why not throw: フォールバックとして標準の #202020 を返す

			var dict = new Dictionary<int, int>();

			for (int y = rect.Top; y < rect.Bottom; y++)
			{
				for (int x = rect.Left; x < rect.Right; x++)
				{
					Color c = bmp.GetPixel(x, y);
					int qR = (c.R / 4) * 4;
					int qG = (c.G / 4) * 4;
					int qB = (c.B / 4) * 4;
					int key = (qR << 16) | (qG << 8) | qB;

					dict.TryGetValue(key, out int count);
					dict[key] = count + 1;
				}
			}

			int maxKey = 0;
			int maxCount = -1;
			foreach (var kvp in dict)
			{
				if (kvp.Value > maxCount)
				{
					maxCount = kvp.Value;
					maxKey = kvp.Key;
				}
			}

			if (maxCount <= 0)
				return Color.FromArgb(32, 32, 32);

			int r = (maxKey >> 16) & 0xFF;
			int g = (maxKey >> 8) & 0xFF;
			int b = maxKey & 0xFF;
			return Color.FromArgb(r, g, b);
		}
	}
}
