using Axantum.AxCrypt.Abstractions;
using Axantum.AxCrypt.Api;
using Axantum.AxCrypt.Api.Model;
using Axantum.AxCrypt.Common;
using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.Extensions;
using Axantum.AxCrypt.Core.Service;
using AxCrypt.Content;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;

using static Axantum.AxCrypt.Abstractions.TypeResolve;

namespace Axantum.AxCrypt.Core.UI.ViewModel
{
    public sealed class SignupSignInViewModel : ViewModelBase
    {
        public string UserEmail { get { return GetProperty<string>(nameof(UserEmail)); } set { SetProperty(nameof(UserEmail), value); } }

        public bool StopAndExit { get { return GetProperty<bool>(nameof(StopAndExit)); } set { SetProperty(nameof(StopAndExit), value); } }

        public bool AlreadyVerified { get { return GetProperty<bool>(nameof(StopAndExit)); } set { SetProperty(nameof(StopAndExit), value); } }

        public bool TopControlsEnabled { get { return GetProperty<bool>(nameof(TopControlsEnabled)); } set { SetProperty(nameof(TopControlsEnabled), value); } }

        public ApiVersion Version { get { return GetProperty<ApiVersion>(nameof(Version)); } set { SetProperty(nameof(Version), value); } }

        public IAsyncAction DoAll { get { return new AsyncDelegateAction<object>((o) => DoAllAsync()); } }

        public Func<CancelEventArgs, Task> CreateAccount { get; set; }

        public Func<CancelEventArgs, Task> VerifyAccount { get; set; }

        public Func<CancelEventArgs, Task> RequestEmail { get; set; }

        public Func<Task> SignInCommandAsync { get; set; }

        public Func<Task> RestoreWindow { get; set; }

        private ISignIn _signinState;

        private NameOf _welcomeMessage;

        private NameOf _startTrialMessage;

        public SignupSignInViewModel(ISignIn signIn, NameOf welcomeMessage, NameOf startTrialMessage)
        {
            _signinState = signIn;
            _welcomeMessage = welcomeMessage;
            _startTrialMessage = startTrialMessage;

            InitializePropertyValues();
            BindPropertyChangedEvents();
            SubscribeToModelEvents();
        }

        private static void InitializePropertyValues()
        {
        }

        private static void BindPropertyChangedEvents()
        {
        }

        private static void SubscribeToModelEvents()
        {
        }

        private async Task DoAllAsync()
        {
            CancelEventArgs e = new CancelEventArgs();
            await OnRestoreWindow(e);
            if (_signinState.IsSigningIn)
            {
                return;
            }

            _signinState.IsSigningIn = true;
            try
            {
                do
                {
                    await WrapMessageDialogsAsync(async () =>
                    {
                        await DoDialogsActionAsync();
                        if (StopAndExit)
                        {
                            return;
                        }

                        await SignInCommandAsync();
                        UserEmail = New<UserSettings>().UserEmail;
                    });
                    if (StopAndExit)
                    {
                        return;
                    }
                } while (String.IsNullOrEmpty(UserEmail));
            }
            finally
            {
                _signinState.IsSigningIn = false;
            }

            if (!New<KnownIdentities>().IsLoggedOn)
            {
                return;
            }

            await New<PremiumManager>().PlanStateWithTryPremium(New<KnownIdentities>().DefaultEncryptionIdentity, _startTrialMessage);

            if (Resolve.UserSettings.IsFirstSignIn)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.InformationTitle, Texts.InternetNotRequiredInformation);
                Resolve.UserSettings.IsFirstSignIn = false;
                return;
            }

            AccountTip tip = new AccountTip();
            try
            {
                tip = await New<AxCryptApiClient>().GetAccountTipAsync(AppTypes.AxCryptWindows2);
            }
            catch (ApiException aex)
            {
                await aex.HandleApiExceptionAsync();
            }
            catch (UnauthorizedException)
            {
                tip = new AccountTip();
            }

            if (!string.IsNullOrEmpty(tip.Message))
            {
                await tip.ShowPopup();
            }
        }

        private async Task WrapMessageDialogsAsync(Func<Task> dialogFunctionAsync)
        {
            TopControlsEnabled = false;
            if (Version != ApiVersion.Zero && Version != new ApiVersion())
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageServerUpdateTitle, Texts.MessageServerUpdateText);
            }
            while (true)
            {
                try
                {
                    await dialogFunctionAsync();
                    if (StopAndExit)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is ApplicationExitException)
                    {
                        throw;
                    }
                    New<IReport>().Exception(ex);
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageUnexpectedErrorTitle, Texts.MessageUnexpectedErrorText.InvariantFormat(ex.Innermost().Message));
                    continue;
                }
                finally
                {
                    TopControlsEnabled = true;
                }
                break;
            }
            return;
        }

        public async Task DoDialogsActionAsync()
        {
            AccountStatus status;
            PopupButtons dialogResult;
            do
            {
                dialogResult = PopupButtons.Ok;
                status = await EnsureEmailAccountAsync();
                if (StopAndExit)
                {
                    return;
                }

                switch (status)
                {
                    case AccountStatus.NotFound:
                        await New<LogOnIdentity, IAccountService>(LogOnIdentity.Empty).SignupAsync(EmailAddress.Parse(UserEmail), CultureInfo.CurrentUICulture);
                        await CheckAccountAsync();
                        status = await VerifyAccountOnline();
                        break;

                    case AccountStatus.InvalidName:
                        dialogResult = await New<IPopup>().ShowAsync(PopupButtons.OkCancelExit, Texts.MessageInvalidSignUpEmailTitle, Texts.MessageInvalidSignUpEmailText.InvariantFormat(UserEmail));
                        UserEmail = string.Empty;
                        break;

                    case AccountStatus.Unverified:
                        status = await VerifyAccountOnline();
                        break;

                    case AccountStatus.Verified:
                        break;

                    case AccountStatus.Offline:
                        New<AxCryptOnlineState>().IsOnline = New<IInternetState>().Connected;
                        CancelEventArgs e = new CancelEventArgs();
                        await OnCreateAccount(e);
                        if (!e.Cancel)
                        {
                            status = AccountStatus.Verified;
                        }
                        break;

                    case AccountStatus.Unknown:
                    case AccountStatus.Unauthenticated:
                    case AccountStatus.DefinedByServer:
                        dialogResult = await New<IPopup>().ShowAsync(PopupButtons.OkCancelExit, Texts.MessageUnexpectedErrorTitle, Texts.MessageUnexpectedErrorText);
                        UserEmail = string.Empty;
                        break;

                    default:
                        break;
                }
                if (dialogResult == PopupButtons.Exit)
                {
                    StopAndExit = true;
                    return;
                }
                if (dialogResult == PopupButtons.Cancel)
                {
                    return;
                }
            } while (status != AccountStatus.Verified);
        }

        private async Task<AccountStatus> EnsureEmailAccountAsync()
        {
            AccountStatus status;
            if (!string.IsNullOrEmpty(UserEmail))
            {
                status = await GetCurrentStatusAsync();
                switch (status)
                {
                    case AccountStatus.Unknown:
                    case AccountStatus.InvalidName:
                    case AccountStatus.Unverified:
                    case AccountStatus.NotFound:
                    case AccountStatus.Offline:
                        break;

                    default:
                        return status;
                }
            }

            await AskForEmailAddressToUse();
            if (StopAndExit)
            {
                return AccountStatus.Unknown;
            }

            status = await New<LogOnIdentity, IAccountService>(LogOnIdentity.Empty).Refresh().StatusAsync(EmailAddress.Parse(UserEmail));
            if (status == AccountStatus.Verified)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.TitleSignInToAxCrypt, Texts.HaveAccountInfo.InvariantFormat(UserEmail));
            }
            return status;
        }

        private async Task<AccountStatus> GetCurrentStatusAsync()
        {
            EmailAddress email;
            if (!EmailAddress.TryParse(UserEmail, out email))
            {
                return AccountStatus.InvalidName;
            }
            AccountStatus status = await New<LogOnIdentity, IAccountService>(LogOnIdentity.Empty).StatusAsync(email);
            return status;
        }

        private async Task AskForEmailAddressToUse()
        {
            CancelEventArgs e = new CancelEventArgs();
            await OnRequestEmail(e);
            if (e.Cancel)
            {
                StopAndExit = true;
            }
        }

        private async Task<AccountStatus> VerifyAccountOnline()
        {
            if (!await IsVerified())
            {
                return AccountStatus.Unverified;
            }

            if (AlreadyVerified)
            {
                AlreadyVerified = false;
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.InformationTitle, Texts.AlreadyVerifiedInfo);
            }

            PopupButtons result = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.WelcomeToAxCryptTitle, Texts.ResourceManager.GetString(_welcomeMessage.Name));
            if (result == PopupButtons.Ok)
            {
                New<IBrowser>().OpenUri(new Uri(Texts.LinkToGettingStarted));
            }
            return AccountStatus.Verified;
        }

        private async Task<bool> IsVerified()
        {
            if (AlreadyVerified)
            {
                return true;
            }

            CancelEventArgs e = new CancelEventArgs();
            await OnVerifyAccount(e);
            return !e.Cancel;
        }

        private async Task CheckAccountAsync()
        {
            try
            {
                AccountStatus status = await New<LogOnIdentity, IAccountService>(LogOnIdentity.Empty).Refresh().StatusAsync(EmailAddress.Parse(UserEmail));
                AlreadyVerified = status.HasFlag(AccountStatus.Verified);
            }
            catch (Exception ex)
            {
                New<IReport>().Exception(ex);
            }
        }

        private async Task OnCreateAccount(CancelEventArgs e)
        {
            await CreateAccount(e);
        }

        private async Task OnVerifyAccount(CancelEventArgs e)
        {
            await VerifyAccount(e);
        }

        private async Task OnRequestEmail(CancelEventArgs e)
        {
            await RequestEmail(e);
        }

        private async Task OnRestoreWindow(EventArgs e)
        {
            await RestoreWindow();
        }
    }
}