using MySqlX.XDevAPI;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace GXUploader.Helpers
{
    public static class PrismAuthSessionHelper
    {
        private const string DefaultAppId = "Prism-API-Explorer";
        private const string DefaultWs = "webclient";

        private static string _cachedAuthSession = "";
        private static string _cachedSeasonSid = "";
        private static string _cachedSubsidiarySid = "";
        private static DateTime _cachedAtUtc = DateTime.MinValue;

        private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(20);
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public static string SeasonSid => _cachedSeasonSid;
        public static string SubsidiarySid => _cachedSubsidiarySid;
        public static async Task<string> GetAuthSessionAsync(HttpClient http, bool forceRefresh = false, string appId = DefaultAppId, string ws = DefaultWs, CancellationToken ct = default)
        {
            if (http == null) throw new ArgumentNullException(nameof(http));

            //if (!forceRefresh && IsCacheValid())
            //    return _cachedAuthSession;

            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!forceRefresh && IsCacheValid())
                    return _cachedAuthSession;

                var cfg = PrismConfigRepository.Load();

                string usr = (cfg.Username ?? "").Trim();
                string pwd = (cfg.Password ?? "").Trim();
                string host = (cfg.Host ?? "").Trim();
                int port = cfg.Port <= 0 ? 80 : cfg.Port;
                string workstation = (cfg.Workstation ?? "").Trim();

                // =========================
                // 1. PRISM CUSTOM FLOW
                // =========================
                if (usr.Equals("prism_custom", StringComparison.OrdinalIgnoreCase))
                {
                    string loginUrl =
                    $"http://{host}:{port}/api/security/login" +
                    $"?appid={WebUtility.UrlEncode(appId)}" +
                    $"&pwd={WebUtility.UrlEncode(pwd)}" +
                    $"&usr={WebUtility.UrlEncode(usr)}" +
                    $"&ws={WebUtility.UrlEncode(ws)}";

                    using var req = new HttpRequestMessage(HttpMethod.Get, loginUrl);
                    req.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, version=2");

                    using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);

                    var body = resp.Content != null
                        ? await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false)
                        : "";

                    if (!resp.IsSuccessStatusCode)
                        throw new Exception($"Prism login failed: {body}");

                    string authSessionHeader = TryGetHeader(resp, "auth-session");
                    string session = !string.IsNullOrWhiteSpace(authSessionHeader)
                        ? authSessionHeader.Trim()
                        : TryExtractAuthSessionFromJson(body)?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(session))
                        throw new Exception("auth-session not returned from Prism login.");

                    _cachedAuthSession = session;

                    _cachedSeasonSid =
                        TryExtractSeasonSidFromAuthSession(_cachedAuthSession)
                        ?? TryExtractSeasonSidFromJson(body)
                        ?? "";

                    _cachedSubsidiarySid =
                        TryExtractSubsidiarySidFromAuthSession(_cachedAuthSession)
                        ?? TryExtractSubsidiarySidFromJson(body)
                        ?? "";

                    _cachedAtUtc = DateTime.UtcNow;

                    //return _cachedAuthSession;
                }
                else
                {
                    string authUrl =
                        $"http://{host}:{port}/v1/rest/auth" +
                        $"?usr={WebUtility.UrlEncode(usr)}" +
                        $"&pwd={WebUtility.UrlEncode(pwd)}";

                    string authNonce = null;
                    string authSession = null;

                    // =========================
                    // STEP 1: GET NONCE
                    // =========================
                    using (var step1 = new HttpRequestMessage(HttpMethod.Get, authUrl))
                    {
                        step1.Headers.TryAddWithoutValidation("Accept", "application/json");

                        using var resp1 = await http.SendAsync(step1, ct).ConfigureAwait(false);

                        if (!resp1.IsSuccessStatusCode)
                            throw new Exception(await resp1.Content.ReadAsStringAsync(ct));

                        authNonce = TryGetHeader(resp1, "auth-nonce");

                        if (string.IsNullOrWhiteSpace(authNonce))
                            throw new Exception("auth-nonce not found.");
                    }

                    // =========================
                    // STEP 2: COMPUTE RESPONSE
                    // =========================
                    string nonceResponse = GenerateNonceResponse(authNonce);

                    // =========================
                    // STEP 3: AUTH LOGIN
                    // =========================
                    using (var step2 = new HttpRequestMessage(HttpMethod.Get, authUrl))
                    {
                        step2.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");

                        step2.Headers.TryAddWithoutValidation("auth-nonce", authNonce);
                        step2.Headers.TryAddWithoutValidation("auth-nonce-response", nonceResponse);

                        using var resp2 = await http.SendAsync(step2, ct).ConfigureAwait(false);

                        var body2 = await resp2.Content.ReadAsStringAsync(ct);

                        if (!resp2.IsSuccessStatusCode)
                            throw new Exception($"Auth failed: {body2}");

                        authSession =
                            TryGetHeader(resp2, "auth-session") ??
                            TryGetHeader(resp2, "auth-session-v9");

                        if (string.IsNullOrWhiteSpace(authSession))
                            throw new Exception("auth-session not returned.");
                    }

                    // =========================
                    // STEP 4: SIT (WORKSTATION BINDING)
                    // =========================
                    string sitUrl =
                        $"http://{host}:{port}/v1/rest/sit?ws={WebUtility.UrlEncode(workstation)}";

                    using (var step3 = new HttpRequestMessage(HttpMethod.Get, sitUrl))
                    {
                        step3.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
                        step3.Headers.TryAddWithoutValidation("auth-session", authSession);

                        using var resp3 = await http.SendAsync(step3, ct).ConfigureAwait(false);

                        var body3 = await resp3.Content.ReadAsStringAsync(ct);

                        if (!resp3.IsSuccessStatusCode)
                            throw new Exception($"SIT failed: {body3}");
                    }

                    // =========================
                    // STEP 5: SESSION CALL
                    // =========================
                    string sessionUrl = $"http://{host}:{port}/v1/rest/session";

                    using (var step4 = new HttpRequestMessage(HttpMethod.Get, sessionUrl))
                    {
                        step4.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
                        step4.Headers.TryAddWithoutValidation("auth-session", authSession);

                        using var resp4 = await http.SendAsync(step4, ct).ConfigureAwait(false);

                        var body4 = await resp4.Content.ReadAsStringAsync(ct);

                        if (!resp4.IsSuccessStatusCode)
                            throw new Exception($"Session fetch failed: {body4}");

                        var json = JsonDocument.Parse(body4);
                        var root = json.RootElement;

                        var first = root.ValueKind == JsonValueKind.Array
                            ? root[0]
                            : root;

                        _cachedAuthSession = authSession;

                        _cachedSeasonSid =
                            first.TryGetProperty("seasonsid", out var season)
                                ? season.GetString()
                                : null;

                        _cachedSubsidiarySid =
                            first.TryGetProperty("subsidiarysid", out var subsidiarysid)
                                ? subsidiarysid.GetString()
                                : null;
                    }
                }

                _cachedAtUtc = DateTime.UtcNow;
                return _cachedAuthSession;
            }
            finally
            {
                _lock.Release();
            }
        }

        private static string GenerateNonceResponse(string authNonce)
        {
            if (!long.TryParse(authNonce, out long nonce))
                throw new Exception("Invalid auth-nonce");

            long result = (nonce / 13L) % 99999L * 17L;

            return result.ToString();
        }

        private static string TryExtractSessionTokenFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                // ✅ API returns ARRAY
                if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var obj = root[0];

                    if (obj.TryGetProperty("token", out var token))
                        return token.GetString();
                }

                // fallback: object response
                if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (root.TryGetProperty("token", out var token))
                        return token.GetString();

                    if (root.TryGetProperty("auth-session", out var alt))
                        return alt.GetString();
                }
            }
            catch
            {
                // ignore parse errors
            }

            return null;
        }

        public static void ClearCache()
        {
            _cachedAuthSession = "";
            _cachedSeasonSid = "";
            _cachedSubsidiarySid = "";
            _cachedAtUtc = DateTime.MinValue;
        }

        private static bool IsCacheValid()
        {
            if (string.IsNullOrWhiteSpace(_cachedAuthSession)) return false;
            return (DateTime.UtcNow - _cachedAtUtc) < CacheLifetime;
        }

        private static string TryGetHeader(HttpResponseMessage resp, string headerName)
        {
            if (resp.Headers.TryGetValues(headerName, out var values))
                return values.FirstOrDefault() ?? "";

            if (resp.Content != null &&
                resp.Content.Headers.TryGetValues(headerName, out var cvalues))
                return cvalues.FirstOrDefault() ?? "";

            return "";
        }

        private static string? TryExtractAuthSessionFromJson(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                string[] keys =
                {
                    "auth-session",
                    "auth_session",
                    "authsession",
                    "session",
                    "authSession"
                };

                foreach (var k in keys)
                {
                    if (root.TryGetProperty(k, out var p))
                        return p.ValueKind == JsonValueKind.String ? p.GetString() : p.ToString();
                }
            }
            catch { }

            return null;
        }

        private static string? TryExtractSeasonSidFromAuthSession(string authSession)
        {
            if (string.IsNullOrWhiteSpace(authSession)) return null;

            var s = authSession.Trim();
            if (!(s.StartsWith("{") && s.EndsWith("}"))) return null;

            return TryExtractSeasonSidFromJson(s);
        }

        private static string? TryExtractSubsidiarySidFromAuthSession(string authSession)
        {
            if (string.IsNullOrWhiteSpace(authSession)) return null;

            var s = authSession.Trim();
            if (!(s.StartsWith("{") && s.EndsWith("}"))) return null;

            return TryExtractSubsidiarySidFromJson(s);
        }

        private static string? TryExtractSeasonSidFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object) continue;

                        var sid = TryGetSeasonSidFromObject(item);
                        if (!string.IsNullOrWhiteSpace(sid))
                            return sid;
                    }

                    return null;
                }

                if (root.ValueKind == JsonValueKind.Object)
                    return TryGetSeasonSidFromObject(root);

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string? TryExtractSubsidiarySidFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object) continue;

                        var sid = TryGetSubsidiarySidFromObject(item);
                        if (!string.IsNullOrWhiteSpace(sid))
                            return sid;
                    }

                    return null;
                }

                if (root.ValueKind == JsonValueKind.Object)
                    return TryGetSubsidiarySidFromObject(root);

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string? TryGetSeasonSidFromObject(JsonElement obj)
        {
            string[] keys =
            {
                "seasonsid",
                "seasonSid",
                "SeasonSid",
                "SeasonSID",
                "seasonSID"
            };

            foreach (var k in keys)
            {
                if (obj.TryGetProperty(k, out var p))
                {
                    if (p.ValueKind == JsonValueKind.String) return p.GetString();
                    return p.ToString();
                }
            }

            return null;
        }

        private static string? TryGetSubsidiarySidFromObject(JsonElement obj)
        {
            string[] keys =
            {
                "subsidiarysid",
                "subsidiarySid",
                "SubsidiarySid",
                "SubsidiarySID",
                "subsidiarySID"
            };

            foreach (var k in keys)
            {
                if (obj.TryGetProperty(k, out var p))
                {
                    if (p.ValueKind == JsonValueKind.String) return p.GetString();
                    return p.ToString();
                }
            }

            return null;
        }
    }
}