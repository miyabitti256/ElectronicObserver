using System.Drawing;
using System.Drawing.Imaging;
using Browser.CefOp;
using Xunit;

namespace ElectronicObserverTests
{
	public class AdmiralNameMaskHelperTests
	{
		private static Bitmap CreateMockBattleResultBitmap(int barX, Color namePixelColor, Color lvPixelColor)
		{
			var bmp = new Bitmap(1200, 720, PixelFormat.Format32bppArgb);
			using (var g = Graphics.FromImage(bmp))
			{
				// Dark result panel background
				using (var bgBrush = new SolidBrush(Color.FromArgb(32, 38, 42)))
				{
					g.FillRectangle(bgBrush, 0, 0, 1200, 720);
				}

				// Vertical white bar (3px wide, Y=116..264)
				using (var whiteBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
				{
					g.FillRectangle(whiteBrush, barX - 2, 116, 3, 149);
				}

				// Cyan 'Lv' label at barX + 280, Y=135
				using (var cyanBrush = new SolidBrush(Color.FromArgb(40, 190, 195)))
				{
					g.FillRectangle(cyanBrush, barX + 280, 133, 15, 10);
					// Cyan '艦隊名' label at barX + 20, Y=175
					g.FillRectangle(cyanBrush, barX + 20, 175, 40, 10);
				}

				// Admiral name text stroke at barX + 35, Y=138
				using (var nameBrush = new SolidBrush(namePixelColor))
				{
					g.FillRectangle(nameBrush, barX + 34, 137, 3, 3);
				}

				// HQ Level number text stroke at barX + 320, Y=138
				using (var lvBrush = new SolidBrush(lvPixelColor))
				{
					g.FillRectangle(lvBrush, barX + 319, 137, 3, 3);
				}
			}
			return bmp;
		}

		/// <summary>
		/// Verifies that IsHomeport returns false for combat screen images with sea texture backgrounds.
		/// </summary>
		[Fact]
		public void IsHomeport_ShouldReturnFalse_ForCombatScreenshots()
		{
			using (var bmp = new Bitmap(1200, 720, PixelFormat.Format32bppArgb))
			{
				using (var g = Graphics.FromImage(bmp))
				{
					using (var brush = new SolidBrush(Color.FromArgb(20, 110, 200)))
					{
						g.FillRectangle(brush, 0, 0, 1200, 720);
					}
				}

				bool isHomeport = AdmiralNameMaskHelper.IsHomeport(bmp);

				Assert.False(isHomeport);
			}
		}

		/// <summary>
		/// Verifies that ProcessScreenShot skips masking completely when in sortie on a combat screen.
		/// </summary>
		[Fact]
		public void ProcessScreenShot_ShouldSkipMasking_WhenInSortieOnCombatScreen()
		{
			using (var bmp = new Bitmap(1200, 720, PixelFormat.Format32bppArgb))
			{
				Color testColor = Color.FromArgb(255, 34, 120, 230);
				using (var g = Graphics.FromImage(bmp))
				{
					using (var brush = new SolidBrush(testColor))
					{
						g.FillRectangle(brush, 0, 0, 1200, 720);
					}
				}

				var result = AdmiralNameMaskHelper.ProcessScreenShot(
					bmp,
					maskAdmiralName: true,
					keepHQLevel: false,
					maskMode: 1,
					maskOnlyOnHomeport: true,
					isInSortie: true);

				Color pixel = result.GetPixel(200, 10);
				Assert.Equal(testColor.ToArgb(), pixel.ToArgb());
			}
		}

		/// <summary>
		/// Verifies that IsHomeport detects a valid homeport header with dark skin and cyan HQ label text.
		/// </summary>
		[Fact]
		public void IsHomeport_ShouldReturnTrue_ForValidHomeportCyanHeader()
		{
			using (var bmp = new Bitmap(1200, 720, PixelFormat.Format32bppArgb))
			{
				using (var g = Graphics.FromImage(bmp))
				{
					using (var bgBrush = new SolidBrush(Color.FromArgb(32, 32, 32)))
					{
						g.FillRectangle(bgBrush, 0, 0, 1200, 36);
					}

					using (var cyanBrush = new SolidBrush(Color.FromArgb(0, 180, 180)))
					{
						g.FillRectangle(cyanBrush, 450, 18, 10, 5);
					}
				}

				bool isHomeport = AdmiralNameMaskHelper.IsHomeport(bmp);

				Assert.True(isHomeport);
			}
		}

		/// <summary>
		/// Verifies that Battle Result screen masks both Admiral Name and HQ Level across slide positions (X=130 and X=76) even when in sortie.
		/// </summary>
		[Theory]
		[InlineData(130, 0)] // Phase 1 (fleet list), Mosaic
		[InlineData(76, 0)]  // Phase 2 (EXP gain), Mosaic
		[InlineData(103, 1)] // Mid-slide, Seamless Fill
		public void ProcessScreenShot_ShouldMaskBattleResultScreen_AcrossSlidePositions(int barX, int maskMode)
		{
			Color textWhite = Color.FromArgb(255, 250, 250, 250);
			using (var bmp = CreateMockBattleResultBitmap(barX, textWhite, textWhite))
			{
				var result = AdmiralNameMaskHelper.ProcessScreenShot(
					bmp,
					maskAdmiralName: true,
					keepHQLevel: false,
					maskMode: maskMode,
					maskOnlyOnHomeport: true,
					isInSortie: true);

				// Both admiral name pixel and HQ level pixel must be masked (no longer pure white text)
				Color nameAfter = result.GetPixel(barX + 35, 138);
				Color lvAfter = result.GetPixel(barX + 320, 138);
				Assert.NotEqual(textWhite.ToArgb(), nameAfter.ToArgb());
				Assert.NotEqual(textWhite.ToArgb(), lvAfter.ToArgb());

				// Vertical white bar and '艦隊名' must remain untouched
				Color barPixel = result.GetPixel(barX, 138);
				Assert.Equal(Color.FromArgb(255, 240, 240, 240).ToArgb(), barPixel.ToArgb());
			}
		}

		/// <summary>
		/// Verifies that keepHQLevel=true preserves the HQ Level number on the Battle Result screen while masking Admiral Name.
		/// </summary>
		[Fact]
		public void ProcessScreenShot_ShouldKeepHQLevelOnBattleResult_WhenKeepHQLevelIsTrue()
		{
			int barX = 76;
			Color textWhite = Color.FromArgb(255, 250, 250, 250);
			using (var bmp = CreateMockBattleResultBitmap(barX, textWhite, textWhite))
			{
				var result = AdmiralNameMaskHelper.ProcessScreenShot(
					bmp,
					maskAdmiralName: true,
					keepHQLevel: true,
					maskMode: 1,
					maskOnlyOnHomeport: true,
					isInSortie: true);

				// Admiral name should be masked, but HQ level must remain untouched
				Color nameAfter = result.GetPixel(barX + 35, 138);
				Color lvAfter = result.GetPixel(barX + 320, 138);
				Assert.NotEqual(textWhite.ToArgb(), nameAfter.ToArgb());
				Assert.Equal(textWhite.ToArgb(), lvAfter.ToArgb());
			}
		}
	}
}
