using Axantum.AxCrypt.Core;
using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.Service;
using Axantum.AxCrypt.Core.Session;
using Axantum.AxCrypt.Core.UI;
using Axantum.AxCrypt.Core.UI.ViewModel;
using Axantum.AxCrypt.Forms;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Axantum.AxCrypt.Abstractions.TypeResolve;
using Texts = AxCrypt.Content.Texts;

namespace Axantum.AxCrypt
{
    public partial class ManageAccountDialog : StyledMessageBase
    {
        private ManageAccountViewModel _viewModel;

        public ManageAccountDialog()
        {
            InitializeComponent();
        }

        public static async Task<ManageAccountDialog> CreateAsync(Form parent)
        {
            ManageAccountDialog mad = new ManageAccountDialog();

            mad.InitializeStyle(parent);

            AccountStorage userKeyPairs = new AccountStorage(New<LogOnIdentity, IAccountService>(Resolve.KnownIdentities.DefaultEncryptionIdentity));
            mad._viewModel = await ManageAccountViewModel.CreateAsync(userKeyPairs);
            mad._viewModel.BindPropertyChanged<IEnumerable<AccountProperties>>(nameof(ManageAccountViewModel.AccountProperties), mad.ListAccountEmails);

            return mad;
        }

        protected override void InitializeContentResources()
        {
            Text = Texts.DialogManageAxcryptIdTitle;

            _changePassphraseButton.Text = Texts.ButtonChangePasswordText;
            _dateHeader.Text = Texts.ColumnTimestampHeader;
        }

        private void ListAccountEmails(IEnumerable<AccountProperties> emails)
        {
            _accountEmailsListView.Items.Clear();
            foreach (AccountProperties email in emails)
            {
                ListViewItem item = new ListViewItem(email.Timestamp.ToLocalTime().ToString(CultureInfo.CurrentCulture));
                item.Name = "Timestamp";

                _accountEmailsListView.Items.Add(item);
            }

            if (_accountEmailsListView.Items.Count == 0)
            {
                _emailLabel.Text = String.Empty;
                return;
            }

            _accountEmailsListView.Columns[0].Width = _accountEmailsListView.ClientSize.Width;
            _emailLabel.Text = emails.First().EmailAddress;
        }

        private async void _changePassphraseButton_Click(object sender, EventArgs e)
        {
            await this.ChangePasswordDialogAsync(_viewModel);
        }

        private void ManageAccountDialog_Load(object sender, EventArgs e)
        {
        }

        private void _accountEmailsListView_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
    }
}