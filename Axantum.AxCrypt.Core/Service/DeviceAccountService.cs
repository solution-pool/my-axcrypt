using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Axantum.AxCrypt.Abstractions;
using Axantum.AxCrypt.Api;
using Axantum.AxCrypt.Api.Model;
using Axantum.AxCrypt.Common;
using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.Crypto.Asymmetric;
using Axantum.AxCrypt.Core.Extensions;
using Axantum.AxCrypt.Core.Runtime;
using Axantum.AxCrypt.Core.Session;
using Axantum.AxCrypt.Core.UI;

using static Axantum.AxCrypt.Abstractions.TypeResolve;

namespace Axantum.AxCrypt.Core.Service
{
    /// <summary>
    /// Implement account service functionality for a device, using a local and a remote service instance. This
    /// class determines the interaction and behavior when the services cooperate to provide a robust service
    /// despite possible remote outages.
    /// An instance operates on behalf an identity, or an anonymous one.
    /// </summary>
    public class DeviceAccountService : IAccountService
    {
        private IAccountService _localService;

        private IAccountService _remoteService;

        public DeviceAccountService(IAccountService localService, IAccountService remoteService)
        {
            _localService = localService;
            _remoteService = remoteService;
        }

        public IAccountService Refresh()
        {
            return this;
        }

        public async Task<bool> HasAccountsAsync()
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    return await _remoteService.HasAccountsAsync();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }
            return await _localService.HasAccountsAsync();
        }

        public LogOnIdentity Identity
        {
            get
            {
                return _remoteService.Identity;
            }
        }

        public async Task<Offers> OffersAsync()
        {
            if (New<AxCryptOnlineState>().IsOnline && Identity != LogOnIdentity.Empty)
            {
                try
                {
                    return await _remoteService.OffersAsync().Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }
            return await _localService.OffersAsync().Free();
        }

        public async Task StartPremiumTrialAsync()
        {
            if (New<AxCryptOnlineState>().IsOnline && Identity != LogOnIdentity.Empty)
            {
                try
                {
                    await _remoteService.StartPremiumTrialAsync().Free();
                    return;
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }
            await _localService.StartPremiumTrialAsync().Free();
        }

        public async Task<bool> ChangePassphraseAsync(Passphrase passphrase)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    return await _remoteService.ChangePassphraseAsync(passphrase).Free();
                }
                catch (UnauthorizedException uaex)
                {
                    New<IReport>().Exception(uaex);
                    return false;
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }
            return await _localService.ChangePassphraseAsync(passphrase).Free();
        }

        /// <summary>
        /// Fetches the user user account.
        /// </summary>
        /// <returns>
        /// The complete user account information.
        /// </returns>
        public async Task<UserAccount> AccountAsync()
        {
            UserAccount localAccount = await _localService.AccountAsync().Free();
            if (New<AxCryptOnlineState>().IsOffline || Identity.Passphrase == Passphrase.Empty)
            {
                return localAccount;
            }

            try
            {
                UserAccount remoteAccount;
                remoteAccount = await _remoteService.AccountAsync().Free();
                if (remoteAccount.AccountKeys.Count == 0)
                {
                    return localAccount;
                }

                UserAccount mergedAccount = remoteAccount.MergeWith(localAccount);

                if (mergedAccount != remoteAccount)
                {
                    await _remoteService.SaveAsync(mergedAccount).Free();
                }
                if (mergedAccount != localAccount)
                {
                    await _localService.SaveAsync(mergedAccount).Free();
                }
                return mergedAccount;
            }
            catch (ApiException aex)
            {
                await aex.HandleApiExceptionAsync();
            }
            return localAccount;
        }

        public async Task<IList<UserKeyPair>> ListAsync()
        {
            IList<UserKeyPair> localKeys = await _localService.ListAsync().Free();
            if (New<AxCryptOnlineState>().IsOffline)
            {
                return localKeys;
            }

            try
            {
                IList<UserKeyPair> remoteKeys = new List<UserKeyPair>();
                try
                {
                    remoteKeys = await _remoteService.ListAsync().Free();
                }
                catch (PasswordException pex)
                {
                    if (localKeys.Count == 0)
                    {
                        throw;
                    }
                    New<IReport>().Exception(pex);
                    return localKeys;
                }

                IList<UserKeyPair> allKeys = remoteKeys.Union(localKeys).ToList();
                if (allKeys.Count() == 0)
                {
                    UserKeyPair currentKeyPair = await _remoteService.CurrentKeyPairAsync().Free();
                    if (currentKeyPair != null)
                    {
                        remoteKeys.Add(currentKeyPair);
                    }
                    allKeys = remoteKeys;
                }
                if (allKeys.Count() > remoteKeys.Count())
                {
                    await _remoteService.SaveAsync(allKeys).Free();
                }
                if (allKeys.Count() > localKeys.Count())
                {
                    await _localService.SaveAsync(allKeys).Free();
                }
                return allKeys;
            }
            catch (ApiException aex)
            {
                await aex.HandleApiExceptionAsync();
            }
            return localKeys;
        }

        public async Task<UserKeyPair> CurrentKeyPairAsync()
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    UserKeyPair currentUserKeyPair = await _remoteService.CurrentKeyPairAsync().Free();
                    return currentUserKeyPair;
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }
            return await _localService.CurrentKeyPairAsync().Free();
        }

        public async Task PasswordResetAsync(string verificationCode)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                await _remoteService.PasswordResetAsync(verificationCode).Free();
                return;
            }
            await _localService.PasswordResetAsync(verificationCode).Free();
        }

        public async Task<UserPublicKey> OtherPublicKeyAsync(EmailAddress email)
        {
            return await OtherUserPublicKeysAsync(() => _localService.OtherPublicKeyAsync(email), () => _remoteService.OtherPublicKeyAsync(email)).Free();
        }

        public async Task<UserPublicKey> OtherUserInvitePublicKeyAsync(EmailAddress email, CustomMessageParameters customParameters)
        {
            return await OtherUserPublicKeysAsync(() => _localService.OtherUserInvitePublicKeyAsync(email, null), () => _remoteService.OtherUserInvitePublicKeyAsync(email, customParameters)).Free();
        }

        private async Task<UserPublicKey> OtherUserPublicKeysAsync(Func<Task<UserPublicKey>> localServiceOtherUserPublicKey, Func<Task<UserPublicKey>> remoteServiceOtherUserPublicKey)
        {
            UserPublicKey publicKey = await localServiceOtherUserPublicKey().Free();
            if (New<AxCryptOnlineState>().IsOffline)
            {
                return NonNullPublicKey(publicKey);
            }

            try
            {
                publicKey = await remoteServiceOtherUserPublicKey().Free();
                using (KnownPublicKeys knownPublicKeys = New<KnownPublicKeys>())
                {
                    knownPublicKeys.AddOrReplace(publicKey);
                }
            }
            catch (ApiException aex)
            {
                await aex.HandleApiExceptionAsync();
            }

            return publicKey;
        }

        private static UserPublicKey NonNullPublicKey(UserPublicKey publicKey)
        {
            if (publicKey != null)
            {
                return publicKey;
            }
            throw new OfflineApiException("Can't find other non-cached public key when offline.");
        }

        public async Task SaveAsync(UserAccount account)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    await _remoteService.SaveAsync(account).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }
            await _localService.SaveAsync(account).Free();
        }

        public async Task SaveAsync(IEnumerable<UserKeyPair> keyPairs)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    await _remoteService.SaveAsync(keyPairs).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }
            await _localService.SaveAsync(keyPairs).Free();
        }

        public async Task SignupAsync(EmailAddress email, CultureInfo culture)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    await _remoteService.SignupAsync(email, culture).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }
            await _localService.SignupAsync(email, culture).Free();
        }

        public async Task<AccountStatus> StatusAsync(EmailAddress email)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    return await _remoteService.StatusAsync(email).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }
            AccountStatus status = await _localService.StatusAsync(email).Free();
            if (status == AccountStatus.NotFound)
            {
                return AccountStatus.Offline;
            }
            return status;
        }

        public async Task SendFeedbackAsync(string subject, string message)
        {
            if (New<AxCryptOnlineState>().IsOnline && Identity != LogOnIdentity.Empty)
            {
                try
                {
                    await _remoteService.SendFeedbackAsync(subject, message).Free();
                    return;
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }

            await _localService.SendFeedbackAsync(subject, message).Free();
        }
    }
}