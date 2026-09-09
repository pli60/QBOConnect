using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QBOLibrary.Models;

namespace QBOLibrary.Auth
{
    public class QboAuth
    {
        private static readonly HttpClient _client;

        // Guards refresh inside this process; the token store guards it across machines
        private static readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        private readonly IQboTokenStore _tokenStore;
        private QboTokenModel _cached;

        public string Owner { get; }
        // Surfaces why a refresh failed instead of returning a bare null
        public string LastAuthError { get; private set; }

        static QboAuth()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(60);
        }

        public QboAuth(IQboTokenStore tokenStore)
        {
            _tokenStore = tokenStore;

            string owner = Environment.MachineName + ":" + Process.GetCurrentProcess().Id;
            Owner = owner.Length > 50 ? owner.Substring(0, 50) : owner;
        }

        public static string BuildAuthorizeUrl(string state)
        {
            return QboConfig.AuthorizeUrl
                + "?client_id=" + Uri.EscapeDataString(QboConfig.ClientId ?? string.Empty)
                + "&response_type=code"
                + "&scope=" + Uri.EscapeDataString(QboConfig.Scope)
                + "&redirect_uri=" + Uri.EscapeDataString(QboConfig.RedirectUri ?? string.Empty)
                + "&state=" + Uri.EscapeDataString(state ?? string.Empty);
        }

        public string GetRealmId()
        {
            QboTokenModel token = _cached ?? _tokenStore.Get();
            if (token != null)
            {
                _cached = token;
            }
            return token?.RealmId;
        }

        public void InvalidateAccessToken()
        {
            _cached = null;
        }

        // Exchanges the one-time authorization code and persists the result
        public async Task<QboResultModel<QboTokenModel>> ExchangeCodeAsync(string code, string realmId)
        {
            if (!QboConfig.IsConfigured)
            {
                return QboResultModel<QboTokenModel>.Fail(0, "NOT_CONFIGURED",
                    "ClientId, ClientSecret and RedirectUri must be set before authorizing.");
            }

            Dictionary<string, string> form = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", QboConfig.RedirectUri }
            };

            QboResultModel<QboTokenModel> result = await PostTokenAsync(form, realmId).ConfigureAwait(false);
            if (result.Success)
            {
                _tokenStore.SaveAuthorized(result.Data, Owner);
                _cached = result.Data;
            }
            return result;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            QboTokenModel token = _tokenStore.Get();
            if (token == null)
            {
                LastAuthError = "No QBO_TOKEN row for environment " + QboConfig.Environment + ".";
                return null;
            }
            if (token.IsAccessValid)
            {
                _cached = token;
                return token.AccessToken;
            }

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                // Re-check - another thread may have refreshed while we waited
                token = _tokenStore.Get();
                if (token == null || !token.IsRefreshValid)
                {
                    LastAuthError = token == null
                        ? "No QBO_TOKEN row."
                        : "Refresh token expired on " + token.RefreshExpires + ".";
                    return null;
                }
                if (token.IsAccessValid)
                {
                    _cached = token;
                    return token.AccessToken;
                }

                if (_tokenStore.TryLockForRefresh(Owner, QboConfig.StaleLockSeconds))
                {
                    try
                    {
                        QboResultModel<QboTokenModel> refreshed = await RefreshAsync(token).ConfigureAwait(false);
                        if (!refreshed.Success)
                        {
                            LastAuthError = "Refresh rejected: " + refreshed.FullError;
                            _tokenStore.ReleaseLock(Owner);
                            return null;
                        }

                        _tokenStore.SaveRefreshed(refreshed.Data, Owner);
                        _cached = refreshed.Data;
                        LastAuthError = null;
                        return refreshed.Data.AccessToken;
                    }
                    catch
                    {
                        _tokenStore.ReleaseLock(Owner);
                        throw;
                    }
                }

                // Someone else holds the lock - poll for the token they write
                for (int i = 0; i < QboConfig.RefreshWaitAttempts; i++)
                {
                    await Task.Delay(QboConfig.RefreshWaitMs).ConfigureAwait(false);
                    token = _tokenStore.Get();
                    if (token != null && token.IsAccessValid)
                    {
                        _cached = token;
                        return token.AccessToken;
                    }
                }
                LastAuthError = "Another process held the refresh lock and no new token appeared.";
                return null;
            }
            finally
            {
                _gate.Release();
            }
        }

        private Task<QboResultModel<QboTokenModel>> RefreshAsync(QboTokenModel current)
        {
            Dictionary<string, string> form = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", current.RefreshToken }
            };
            return PostTokenAsync(form, current.RealmId, current.RefreshExpires);
        }

        private async Task<QboResultModel<QboTokenModel>> PostTokenAsync(
            Dictionary<string, string> form, string realmId, DateTime? existingRefreshExpires = null)
        {
            string basic = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(QboConfig.ClientId + ":" + QboConfig.ClientSecret));

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, QboConfig.TokenUrl))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new FormUrlEncodedContent(form);

                try
                {
                    using (HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false))
                    {
                        string text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode)
                        {
                            return QboResultModel<QboTokenModel>.Fail(
                                (int)response.StatusCode, "OAUTH",
                                "Token request failed.", text, null, text);
                        }

                        JObject json = JObject.Parse(text);

                        string accessToken = ReadValue(json, "access_token", "accessToken");
                        string refreshToken = ReadValue(json, "refresh_token", "refreshToken");
                        int expiresIn = ReadInt(json, "expires_in", "expiresIn", 3600);
                        int refreshExpiresIn = ReadInt(json, "x_refresh_token_expires_in",
                                                        "xRefreshTokenExpiresIn", 0);

                        if (string.IsNullOrEmpty(accessToken))
                        {
                            return QboResultModel<QboTokenModel>.Fail((int)response.StatusCode, "OAUTH",
                                "Token response did not contain an access token.", text, null, text);
                        }

                        QboTokenModel token = new QboTokenModel
                        {
                            Environment = QboConfig.Environment,
                            RealmId = realmId,
                            AccessToken = accessToken,
                            AccessExpires = DateTime.Now.AddSeconds(expiresIn),
                            RefreshToken = refreshToken,
                            // Intuit omits this on some refresh responses - keep the known value
                            RefreshExpires = refreshExpiresIn > 0
                                ? DateTime.Now.AddSeconds(refreshExpiresIn)
                                : existingRefreshExpires
                        };

                        return QboResultModel<QboTokenModel>.Ok(token, (int)response.StatusCode, null, text);
                    }
                }
                catch (Exception ex)
                {
                    return QboResultModel<QboTokenModel>.Fail(0, "TRANSPORT", ex.Message);
                }
            }
        }

        private static string ReadValue(JObject json, params string[] names)
        {
            foreach (string name in names)
            {
                JToken token = json[name];
                if (token != null && token.Type != JTokenType.Null)
                {
                    return token.ToString();
                }
            }
            return null;
        }

        private static int ReadInt(JObject json, string name, string altName, int fallback)
        {
            string raw = ReadValue(json, name, altName);
            int parsed;
            return int.TryParse(raw, out parsed) ? parsed : fallback;
        }
    }
}
