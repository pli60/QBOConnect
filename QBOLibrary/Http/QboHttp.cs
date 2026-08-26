using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QBOLibrary.Auth;
using QBOLibrary.Models;

namespace QBOLibrary.Http
{
    public class QboHttp
    {
        private static readonly HttpClient _client;
        private static readonly Random _random = new Random();
        private static readonly object _randomLock = new object();

        private static readonly int[] RetryableStatuses = { 429, 500, 502, 503, 504 };

        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly QboAuth _auth;
        private readonly IQboLogStore _logStore;

        static QboHttp()
        {
            // Intuit requires TLS 1.2 or higher
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            _client = new HttpClient();
            _client.Timeout = Timeout.InfiniteTimeSpan;   // per-request token controls the timeout
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("LIMS-QBOConnect/1.0");
        }

        public QboHttp(QboAuth auth, IQboLogStore logStore)
        {
            _auth = auth;
            _logStore = logStore;
        }

        public Task<QboResultModel<JObject>> GetAsync(string path, string entity, string operation, string limsKey)
        {
            return SendAsync(HttpMethod.Get, path, null, entity, operation, limsKey);
        }

        public Task<QboResultModel<JObject>> PostAsync(string path, object body, string entity, string operation, string limsKey)
        {
            return SendAsync(HttpMethod.Post, path, body, entity, operation, limsKey);
        }

        private async Task<QboResultModel<JObject>> SendAsync(HttpMethod method, string path, object body,
                                                              string entity, string operation, string limsKey)
        {
            string requestJson = body == null
                ? null
                : JsonConvert.SerializeObject(body, SerializerSettings);

            QboResultModel<JObject> result = null;
            bool refreshForced = false;

            for (int attempt = 0; attempt <= QboConfig.MaxRetries; attempt++)
            {
                string accessToken = await _auth.GetAccessTokenAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(accessToken))
                {
                    result = QboResultModel<JObject>.Fail(0, "NO_TOKEN",
                        "No valid QuickBooks Online access token is available. Reconnect the company.");
                    break;
                }

                result = await SendOnceAsync(method, path, requestJson, accessToken).ConfigureAwait(false);

                if (result.Success)
                {
                    break;
                }

                // One forced refresh on a 401, then give up rather than loop
                if (result.HttpStatus == 401 && !refreshForced)
                {
                    refreshForced = true;
                    _auth.InvalidateAccessToken();
                    continue;
                }

                if (!RetryableStatuses.Contains(result.HttpStatus) && result.HttpStatus != 0)
                {
                    break;
                }
                if (attempt >= QboConfig.MaxRetries)
                {
                    break;
                }

                await BackoffAsync(attempt).ConfigureAwait(false);
            }

            WriteLog(result, entity, operation, limsKey, requestJson);
            return result;
        }

        private async Task<QboResultModel<JObject>> SendOnceAsync(HttpMethod method, string path,
                                                                  string requestJson, string accessToken)
        {
            string url = BuildUrl(path);

            using (HttpRequestMessage request = new HttpRequestMessage(method, url))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (requestJson != null)
                {
                    request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                }

                try
                {
                    using (CancellationTokenSource cts =
                        new CancellationTokenSource(TimeSpan.FromSeconds(QboConfig.RequestTimeoutSeconds)))
                    using (HttpResponseMessage response =
                        await _client.SendAsync(request, cts.Token).ConfigureAwait(false))
                    {
                        string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        string intuitTid = ReadHeader(response, "intuit_tid");
                        int status = (int)response.StatusCode;

                        JObject json = TryParse(responseText);

                        if (response.IsSuccessStatusCode)
                        {
                            return QboResultModel<JObject>.Ok(json, status, intuitTid, responseText);
                        }
                        return BuildFault(status, json, intuitTid, responseText);
                    }
                }
                catch (OperationCanceledException)
                {
                    return QboResultModel<JObject>.Fail(0, "TIMEOUT",
                        "The QuickBooks Online request timed out after " + QboConfig.RequestTimeoutSeconds + " seconds.");
                }
                catch (Exception ex)
                {
                    return QboResultModel<JObject>.Fail(0, "TRANSPORT", ex.Message);
                }
            }
        }

        private string BuildUrl(string path)
        {
            string realmId = _auth.GetRealmId();
            string url = QboConfig.BaseUrl + "/v3/company/" + realmId + "/" + path;
            url += url.Contains("?") ? "&" : "?";
            url += "minorversion=" + QboConfig.MinorVersion;
            return url;
        }

        private static QboResultModel<JObject> BuildFault(int status, JObject json, string intuitTid, string raw)
        {
            JToken error = json?["Fault"]?["Error"]?.FirstOrDefault();

            if (error == null)
            {
                return QboResultModel<JObject>.Fail(status, status.ToString(),
                    "QuickBooks Online returned HTTP " + status + ".", null, intuitTid, raw);
            }

            return QboResultModel<JObject>.Fail(
                status,
                error["code"]?.ToString(),
                error["Message"]?.ToString(),
                error["Detail"]?.ToString(),
                intuitTid,
                raw);
        }

        private static JObject TryParse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            try
            {
                return JObject.Parse(text);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string ReadHeader(HttpResponseMessage response, string name)
        {
            IEnumerable<string> values;
            if (response.Headers.TryGetValues(name, out values))
            {
                return values.FirstOrDefault();
            }
            return null;
        }

        private static async Task BackoffAsync(int attempt)
        {
            int jitter;
            lock (_randomLock)
            {
                jitter = _random.Next(0, 250);
            }
            int delay = (int)(Math.Pow(2, attempt) * QboConfig.RetryBaseDelayMs) + jitter;
            await Task.Delay(delay).ConfigureAwait(false);
        }

        private void WriteLog(QboResultModel<JObject> result, string entity, string operation,
                              string limsKey, string requestJson)
        {
            if (_logStore == null || result == null)
            {
                return;
            }

            _logStore.Write(new QboLogModel
            {
                Environment = QboConfig.Environment,
                Entity = entity,
                Operation = operation,
                LimsKey = limsKey,
                HttpStatus = result.HttpStatus,
                IntuitTid = result.IntuitTid,
                ErrorCode = result.ErrorCode,
                ErrorMsg = result.FullError,
                RequestJson = QboConfig.LogRequestBodies ? requestJson : null,
                ResponseJson = result.Success ? null : result.RawResponse,
                CreatedBy = _auth.Owner
            });
        }
    }
}
