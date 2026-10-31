using System.Drawing;
using System.Drawing.Imaging;
using Browser.CefOp;
using Xunit;

namespace ElectronicObserverTests
{
	public class AdmiralNameMaskHelperTests
	{
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
					// Fill background with typical combat sea blue color
					using (var brush = new SolidBrush(Color.FromArgb(20, 110, 200)))
					{
						g.FillRectangle(brush, 0, 0, 1200, 720);
					}
				}

				bool isHomeport = AdmiralNameMaskHelper.IsHomeport(bmp);

				// Combat screen sea texture must not be detected as homeport
				Assert.False(isHomeport);
			}
		}

		/// <summary>
		/// Verifies that ProcessScreenShot skips masking completely when in sortie.
		/// </summary>
		[Fact]
		public void ProcessScreenShot_ShouldSkipMasking_WhenInSortie()
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

				// When isInSortie is true and maskOnlyOnHomeport is true
				var result = AdmiralNameMaskHelper.ProcessScreenShot(
					bmp,
					maskAdmiralName: true,
					keepHQLevel: false,
					maskMode: 1, // Blackout
					maskOnlyOnHomeport: true,
					isInSortie: true);

				// Pixel at admiral name area (X=200, Y=10) should remain unmodified
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
					// Dark header bar background (Y=0..36)
					using (var bgBrush = new SolidBrush(Color.FromArgb(32, 32, 32)))
					{
						g.FillRectangle(bgBrush, 0, 0, 1200, 36);
					}

					// Cyan text pixels for "艦隊司令部Lv." (about 50 pixels within X: 420..540, Y: 15..25)
					using (var cyanBrush = new SolidBrush(Color.FromArgb(0, 180, 180)))
					{
						g.FillRectangle(cyanBrush, 450, 18, 10, 5); // 50 pixels
					}
				}

				bool isHomeport = AdmiralNameMaskHelper.IsHomeport(bmp);

				Assert.True(isHomeport);
			}
		}
	}
}
