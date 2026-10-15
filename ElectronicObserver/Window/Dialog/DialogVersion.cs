using ElectronicObserver.Resource;
using ElectronicObserver.Utility;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ElectronicObserver.Window.Dialog
{
	public partial class DialogVersion : Form
	{
		public DialogVersion()
		{
			InitializeComponent();

			TextVersion.Text = string.Format("{0} (ver. {1})", SoftwareInformation.VersionJapanese, SoftwareInformation.InstalledVersion);

			// この Fork (改変者) の情報
			TextAuthorFork.Text = SoftwareInformation.ForkAuthorName;
			TextInformation4.Text = SoftwareInformation.RepositoryUrl;
			LabelForkVersion.Text = SoftwareInformation.InstalledVersion;
		}

		private void TextAuthor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("https://twitter.com/andanteyk");
		}

		private void ButtonClose_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void TextInformation_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://electronicobserver.blog.fc2.com/");
		}

		private void TextInformation2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("https://github.com/yosxpeee/ElectronicObserver");
		}

		private void label9_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("https://twitter.com/74EO_iwakawa");
		}

		private void TextInformation3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start(SoftwareInformation.BaseRepositoryUrl);
		}

		private void TextAuthorFork_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start(SoftwareInformation.ForkAuthorUrl);
		}

		private void TextInformation4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start(SoftwareInformation.RepositoryUrl);
		}

		private void DialogVersion_Load(object sender, EventArgs e)
		{
			this.Icon = ResourceManager.Instance.AppIcon;

			// ベースにしている本家 (dais-k 版) のバージョン
			LabelBaseVersion.Text = SoftwareInformation.BaseVersionEnglish;

			// version.txt の 3 行目以降に書かれた更新履歴
			string notes = SoftwareInformation.InstalledVersionNotes;
			TextHistory.Text = notes.Length > 0 ? notes : "(更新履歴はありません)";
		}

	}
}
