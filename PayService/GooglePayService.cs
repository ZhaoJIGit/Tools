using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web;
using Twines.Common;
using Twines.Domain.Service.IService;
using Twines.Result;
using ZXing;
using ZXing.Aztec.Internal;

namespace Twines.Domain.Service.Service
{
    /// <summary>
    /// 谷歌支付服务
    /// </summary>
    public class GooglePayService : IGooglePayService
    {
        /// <summary>
        /// 回调从定向地址
        /// </summary>
        private string RedirectUri;
        /// <summary>
        /// 取消token地址
        /// </summary>
        private string RevokeTokenUrl;
        private string Version;
        /// <summary>
        /// 产品集合
        /// </summary>
        private string InappproductsListUri;
        /// <summary>
        /// 产品
        /// </summary>
        private string InappProductPurchaseUri;
        /// <summary>
        /// 订阅
        /// </summary>
        private string SubscriptionsUri;
        /// <summary>
        /// 退订取消服务
        /// </summary>
        private string VoidedPurchasesUri;
        /// <summary>
        /// 订阅的元数据
        /// </summary>
        public string SubscriptionsV2Uri;
        /// <summary>
        /// 确认对应用内商品的购买
        /// </summary>
        public string ProductsAcknowledgeUri;
        /// <summary>
        /// 消费购买应用内商品
        /// </summary>
        public string ProductsConsumeUri;
        private GoogleConfigOption googleConfig;
        ILogger<GooglePayService> _logger;
        private readonly IHttpClientFactory _httpClientFactoryFactory;
        public GooglePayService(ILogger<GooglePayService> logger, IOptions<GoogleConfigOption> options, IHttpClientFactory httpClientFactoryFactory)
        {
            _logger = logger;
            googleConfig = options.Value;
            _httpClientFactoryFactory = httpClientFactoryFactory;
            RedirectUri = $"{googleConfig.ApiHost}/Notify/GoogleCallBack";
            RevokeTokenUrl = $"{googleConfig.ApiHost}/Notify/GoogleCallBack";
            Version = "v3";
            InappproductsListUri = $"{googleConfig.BaseUri}{googleConfig.GooglePackageName}/inappproducts";
            InappProductPurchaseUri = $"{googleConfig.BaseUri}{googleConfig.GooglePackageName}/purchases/products/{{0}}/tokens/{{1}}";
            SubscriptionsUri = $"{googleConfig.BaseUri}{googleConfig.GooglePackageName}/purchases/subscriptions/{{0}}/tokens/{{1}}";
            VoidedPurchasesUri = $"{googleConfig.BaseUri}{googleConfig.GooglePackageName}/purchases/voidedpurchases";
            SubscriptionsV2Uri = $"{googleConfig.BaseUri}{googleConfig.GooglePackageName}/purchases/subscriptionsv2/tokens/{{0}}";
            ProductsAcknowledgeUri = $"{googleConfig.BaseUri}{googleConfig.GooglePackageName}/purchases/products/{{0}}/tokens/{{1}}:acknowledge";
            ProductsConsumeUri = $"{googleConfig.BaseUri}{googleConfig.GooglePackageName}/purchases/products/{{0}}/tokens/{{1}}:consume";
        }


        #region Token相关
        public AuthorizationCodeFlow CreateFlow(IDataStore dataStore = null, IEnumerable<string> scopes = null, IHttpClientFactory httpClientFactory = null)
        {
            string nonce = DateTimeHelper.Now.Ticks.ToString();
            var secrets = new ClientSecrets() { ClientId = googleConfig.GoogleApiClientId, ClientSecret = googleConfig.GoogleApiClientSecret };
            var initializer = new GoogleAuthorizationCodeFlow.Initializer()
            {
                RevokeTokenUrl = RevokeTokenUrl,
                IncludeGrantedScopes = true,
                LoginHint = googleConfig.GoogleApiLoginHint,
                Nonce = nonce,
                UserDefinedQueryParams = null,
                ClientSecrets = secrets,
                Scopes = new List<string> { "https://www.googleapis.com/auth/androidpublisher" }
            };
            var flow = new GoogleAuthorizationCodeFlow(initializer);
            return flow;
        }

        public async Task<TokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            TokenResponse tokenResponse = null;
            try
            {
                var post = string.Format("code={0}&client_id={1}&client_secret={2}&redirect_uri={3}&grant_type=authorization_code",
                                        code,
                                        HttpUtility.HtmlEncode(googleConfig.GoogleApiClientId),
                                        googleConfig.GoogleApiClientSecret,
                                        HttpUtility.HtmlEncode(RedirectUri));
                var responseStr = await GetHttpResponseAsync(HttpMethod.Post, googleConfig.TokenUrl, "", post, "application/x-www-form-urlencoded");
                if (responseStr.NotNull())
                {
                    tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseStr);
                }
            }
            catch (Exception ex)
            {
                tokenResponse = null;
                throw ex;
            }
            return tokenResponse;
        }

        public async Task<string> GetAccessTokenFromJSONKeyAsync(string jsonKeyFilePath, params string[] scopes)
        {
            using (var stream = new FileStream(jsonKeyFilePath, FileMode.Open, FileAccess.Read))
            {
                return await GoogleCredential
                    .FromStream(stream) // Loads key file  
                    .CreateScoped(scopes) // Gathers scopes requested  
                    .UnderlyingCredential // Gets the credentials  
                    .GetAccessTokenForRequestAsync(); // Gets the Access Token  
            }
        }

        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
        {
            TokenResponse tokenResponse = null;
            try
            {
                var post = string.Format("client_id={0}&client_secret={1}&grant_type=refresh_token&refresh_token={2}",
                                        HttpUtility.HtmlEncode(googleConfig.GoogleApiClientId),
                                        googleConfig.GoogleApiClientSecret,
                                        refreshToken);
                var responseStr = await GetHttpResponseAsync(HttpMethod.Post, googleConfig.TokenUrl, "", post, "application/x-www-form-urlencoded");
                if (responseStr.NotNull())
                {
                    tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseStr);
                }
                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RefreshTokenAsync:{ex.Message}-->{ex.StackTrace},请求参数：refreshToken={refreshToken}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }
        public void GetGoogleCodeAsync()
        {
            var follow = CreateFlow();
            var response = follow.CreateAuthorizationCodeRequest(RedirectUri);
            var codeReceiver = new LocalServerCodeReceiver();
            var res = codeReceiver.ReceiveCodeAsync(response, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<Root> GetGoogleGencode(decimal Longitude, decimal Latitude, string apikey, string language = "EN", int timeout = 5000)
        {
            var result = new Root();
            var GoogleGeocodeUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={Latitude},{Longitude}&key={apikey}&language={language}";
            try
            {
                var responseStr = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                if (responseStr.NotNull())
                {
                    result = JsonConvert.DeserializeObject<Root>(responseStr);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(("GoogleGeocodeHelper->GetGoogleGencode"), GoogleGeocodeUrl);
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
            return result;
        }

        #endregion

        #region 一次性产品 钻石

        /// <summary>
        /// 查看应用内商品的购买和消耗状态
        /// </summary>
        /// <param name="accessToken">通过googleapi.json获取的token</param>
        /// <param name="productId">应用内商品 SKU（例如“com.some.thing.inapp1”）</param>
        /// <param name="token">购买应用内商品时向用户设备提供的令牌</param>
        /// <returns></returns>
        public async Task<FullProductPurchase> GetInappProductPurchase(string accessToken, string productId, string token)
        {
            FullProductPurchase result = null;
            try
            {
                var responseStr = await GetHttpResponseAsync(HttpMethod.Get, string.Format(InappProductPurchaseUri, productId, token), accessToken);
                if (!string.IsNullOrEmpty(responseStr))
                {
                    result = JsonConvert.DeserializeObject<FullProductPurchase>(responseStr);
                }
                //LogHelper.Log.Info($"GoogleHelper->GetInappProductPurchase;accessToken={accessToken};productId={productId};token={token};retuanStr={retuanStr};result={result.ToJson()}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetInappProductPurchase:{ex.Message}-->{ex.StackTrace},请求参数：accessToken={accessToken},productId={productId},token={token}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }
        #endregion

        #region 取消服务

        /// <summary>
        /// 列出已取消、已退款或已退款的购买交易。
        /// </summary>
        /// <param name="accessToken">通过googleapi.json获取的token</param>
        /// <param name="nextToken">定义要返回的页面的令牌，通常取自 TokenPagination。此方法只能在启用令牌分页的情况下使用。</param>
        /// <param name="maxResults">定义列出操作应返回的结果数量。默认数量取决于资源集合</param>
        /// <returns></returns>
        public async Task<GoogleVoided> GetVoidedPurchases(string accessToken, string nextToken = "", int maxResults = 0)
        {
            GoogleVoided result = null;
            try
            {
                var requestUrl = VoidedPurchasesUri;
                string concat = "?";
                if (maxResults > 0)
                {
                    requestUrl += string.Concat(concat, "maxResults=", maxResults);
                    concat = "&";
                }
                if (!string.IsNullOrEmpty(nextToken))
                {
                    requestUrl += string.Concat(concat, "token=", nextToken);
                    concat = "&";
                }
                var responseStr = await GetHttpResponseAsync(HttpMethod.Get, requestUrl, accessToken);
                if (!string.IsNullOrEmpty(responseStr))
                {
                    result = JsonConvert.DeserializeObject<GoogleVoided>(responseStr);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetVoidedPurchases:{ex.Message}-->{ex.StackTrace},请求参数：accessToken={accessToken},nextToken={nextToken},maxResults={maxResults}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }

        #endregion

        #region 订阅相关
        /// <summary>
        /// 获取订阅详情，检查用户的订阅购买是否有效，并返回其过期时间
        /// </summary>
        /// <param name="accessToken">通过googleapi.json获取的token</param>
        /// <param name="subscriptionId">购买的订阅 ID（例如“monthly001”）</param>
        /// <param name="token">购买订阅时向用户设备提供的令牌</param>
        /// <param name="product">服务器环境,是否需要代理服务处理</param>
        /// <returns></returns>
        /// <exception cref="CustomException"></exception>
        public async Task<SubscriptionPurchase> GetSubscriptionPurchase(string accessToken, string subscriptionId, string token, string product)
        {
            SubscriptionPurchase result = null;
            try
            {
                var responseStr = string.Empty;
                if (product != "CD")
                {
                    responseStr = await GetHttpResponseAsync(HttpMethod.Get, string.Format(SubscriptionsUri, subscriptionId, token), accessToken);
                }
                else
                {
                    var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/GetSubscriptionPurchase?accessToken={accessToken}&subscriptionId={subscriptionId}&token={token}";
                    responseStr = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                }
                if (!string.IsNullOrEmpty(responseStr))
                {
                    result = JsonConvert.DeserializeObject<SubscriptionPurchase>(responseStr);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetSubscriptionPurchase:{ex.Message}-->{ex.StackTrace},请求参数：accessToken={accessToken},subscriptionId={subscriptionId},token={token}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }

        /// <summary>
        /// 获取关于订阅的元数据
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="token"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public async Task<SubscriptionPurchaseV2> GetSubscriptionsV2Purchases(string accessToken, string token, string product)
        {
            SubscriptionPurchaseV2 result = null;
            try
            {
                var responseStr = string.Empty;
                if (product != "CD")
                {
                    responseStr = await GetHttpResponseAsync(HttpMethod.Get, string.Format(SubscriptionsV2Uri, token), accessToken);
                }
                else
                {
                    var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/GetSubscriptionsV2Purchases?accessToken={accessToken}&token={token}";
                    responseStr = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                }
                if (!string.IsNullOrEmpty(responseStr))
                {
                    result = JsonConvert.DeserializeObject<SubscriptionPurchaseV2>(responseStr);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetSubscriptionsV2Purchases:{ex.Message}-->{ex.StackTrace},请求参数：accessToken={accessToken},token={token}");
                throw;
            }
        }
        /// <summary>
        /// 确认对应用内商品的购买(vip)
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="productId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> ProductsAcknowledge(string accessToken, string productId, string token)
        {
            bool result = false;
            try
            {
                var responseStr = await GetHttpResponseAsync(HttpMethod.Post, string.Format(ProductsAcknowledgeUri, productId, token), accessToken);
                //如果成功，则响应正文为空
                if (string.IsNullOrEmpty(responseStr))
                {
                    result = true;
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ProductsAcknowledge:{ex.Message}-->{ex.StackTrace},请求参数：accessToken={accessToken},productId={productId},token={token}");
                throw;
            }
        }

        /// <summary>
        /// 消费购买应用内商品(钻石)
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="productId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> ProductsConsume(string accessToken, string productId, string token)
        {
            bool result = false;
            try
            {
                var responseStr = await GetHttpResponseAsync(HttpMethod.Post, string.Format(ProductsConsumeUri, productId, token), accessToken);
                //如果成功，则响应正文为空
                if (string.IsNullOrEmpty(responseStr))
                {
                    result = true;
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ProductsConsume:{ex.Message}-->{ex.StackTrace},请求参数：accessToken={accessToken},productId={productId},token={token}");
                throw;
            }
        }
        #endregion

        #region 代理服务相关
        public async Task<Root> GetGoogleGencodeInfo(string apikey, string product, decimal Longitude, decimal Latitude, string language = "EN", int timeout = 5000)
        {
            var result = new Root();
            var jsonStr = string.Empty;

            var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/GetGoogleGencode?Latitude={Latitude}&Longitude={Longitude}&language={language}";
            try
            {
                if (product != "CD")
                {
                    GoogleGeocodeUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={Latitude},{Longitude}&key={apikey}&language={language}";
                }
                var responseStr = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                if (!string.IsNullOrEmpty(responseStr))
                {
                    result = JsonConvert.DeserializeObject<Root>(responseStr);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetGoogleGencodeInfo:{ex.Message}-->{ex.StackTrace},请求参数：apikey:{apikey},product:{product},Longitude:{Longitude},Latitude:{Latitude},language:{language}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }

        public async Task<TokenResponse> ExchangeCodeForTokenAsync(string product, string code)
        {
            TokenResponse tokenResponse = null;
            var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/GetGoogleExchangeCodeForTokenAsync?code={code}";
            try
            {
                if (product != "CD")
                {
                    return await ExchangeCodeForTokenAsync(code);
                }
                var responseStr = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                if (!string.IsNullOrEmpty(responseStr))
                {
                    tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseStr);
                }
                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ExchangeCodeForTokenAsync：{ex.Message}-->{ex.StackTrace},请求参数：product={product},code={code}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }

        public async Task<string> GetGoogleAccessToken(string product, int timeout = 5000)
        {
            var token = string.Empty;
            try
            {
                if (product != "CD")
                {
                    token = await GetAccessTokenFromJSONKeyAsync("./Config/googleapi.json", "https://www.googleapis.com/auth/androidpublisher");
                }
                else
                {
                    var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/GoogleServiceAccountAccessToken";
                    token = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                }
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetGoogleAccessToken：{ex.Message}-->{ex.StackTrace},请求参数：product={product}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }

        public async Task GetGoogleApiTokenInfo(string product, int timeout = 5000)
        {
            try
            {
                if (product != "CD")
                {
                    GetGoogleCodeAsync();
                    return;
                }
                else
                {
                    var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/GetGoogleCallBack";
                    var responseStr = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetGoogleAccessToken：{ex.Message}-->{ex.StackTrace},请求参数：product={product}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }

        public async Task<FullProductPurchase> GetInappProductPurchase(string accessToken, string productId, string token, string product, int timeout = 5000)
        {
            FullProductPurchase result = null;
            string retuanStr = string.Empty;
            try
            {
                if (product != "CD")
                {
                    result = await GetInappProductPurchase(accessToken, productId, token);
                }
                else
                {
                    var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/GetInappProductPurchase?accessToken={accessToken}&productId={productId}&token={token}";
                    var responseStr = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                    if (!string.IsNullOrEmpty(responseStr))
                    {
                        result = JsonConvert.DeserializeObject<FullProductPurchase>(responseStr);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetInappProductPurchase：{ex.Message}--->{ex.StackTrace},请求参数：accessToken={accessToken};productId={productId};token={token};product={product}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }

        /// <summary>
        /// 获取产品列表(通过代理获取代理)
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="product"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<InappproductsListResponse> GetInappproductsList(string accessToken, string product, int timeout = 5000)
        {
            InappproductsListResponse result = null;
            try
            {
                if (product != "CD")
                {
                    result = await GetInappproductsList(accessToken);
                }
                else
                {
                    var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/GetInappproductsList?accessToken={accessToken}";
                    var responseStr = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                    if (!string.IsNullOrEmpty(responseStr))
                    {
                        result = JsonConvert.DeserializeObject<Google.Apis.AndroidPublisher.v3.Data.InappproductsListResponse>(responseStr);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetInappproductsList：{ex.Message}--->{ex.StackTrace},请求参数：accessToken={accessToken};product={product}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }

        /// <summary>
        /// 获取商品列表
        /// </summary>
        /// <param name="accessToken">通过googleapi.json获取的token</param>
        /// <returns></returns>
        public async Task<InappproductsListResponse> GetInappproductsList(string accessToken)
        {
            Google.Apis.AndroidPublisher.v3.Data.InappproductsListResponse result = null;
            try
            {
                var responseStr = await GetHttpResponseAsync(HttpMethod.Get, InappproductsListUri, accessToken);
                if (!string.IsNullOrEmpty(responseStr))
                {
                    var obj = JObject.Parse(responseStr);
                    var error = obj["error"];
                    if (error != null)
                    {
                        _logger.LogError($"GetInappproductsList,请求参数:{accessToken},返回内容:{responseStr}");
                    }
                    else
                    {
                        result = JsonConvert.DeserializeObject<Google.Apis.AndroidPublisher.v3.Data.InappproductsListResponse>(responseStr);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetInappproductsList：{ex.Message}--->{ex.StackTrace},请求参数：accessToken={accessToken};");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }

        /// <summary>
        /// VoidedPurchase 资源表示购买交易已取消/已退款/已退款。
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="product"></param>
        /// <param name="timeout"></param>
        /// <param name="nextToken"></param>
        /// <param name="maxResults"></param>
        /// <returns></returns>
        public async Task<GoogleVoided> GetVoidedPurchases(string accessToken, string product, int timeout = 5000, string nextToken = "", int maxResults = 0)
        {
            var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/GetVoidedPurchases?accessToken={accessToken}";
            GoogleVoided result = null;
            try
            {
                if (product != "CD")
                {
                    result = await GetVoidedPurchases(accessToken, nextToken, maxResults);
                }
                else
                {
                    GoogleGeocodeUrl += $"&packageName={googleConfig.GooglePackageName}";
                    var requestUrl = GoogleGeocodeUrl;
                    if (maxResults > 0)
                    {
                        requestUrl += string.Concat("&", "maxResults=", maxResults);
                    }
                    if (!string.IsNullOrEmpty(nextToken))
                    {
                        requestUrl += string.Concat("&", "token=", nextToken);
                    }
                    var responseStr = await GetHttpResponseAsync(HttpMethod.Get, requestUrl, "");
                    if (!string.IsNullOrEmpty(responseStr))
                    {
                        result = JsonConvert.DeserializeObject<GoogleVoided>(responseStr);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetVoidedPurchases：{ex.Message}--->{ex.StackTrace},请求参数：accessToken={accessToken};product={product};nextToken={nextToken};maxResults={maxResults}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }

        public async Task<TokenResponse> RefreshTokenAsync(string product, string refreshToken)
        {
            TokenResponse tokenResponse = null;
            var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/GetGoogleRefreshTokenAsync?refreshToken={refreshToken}";
            try
            {
                if (product != "CD")
                {
                    return await RefreshTokenAsync(refreshToken);
                }
                var responseStr = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                if (!string.IsNullOrEmpty(responseStr))
                {
                    tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseStr);
                }
                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RefreshTokenAsync：{ex.Message}--->{ex.StackTrace},请求参数：product={product};refreshToken={refreshToken}");
                throw new CustomException(TwinesErrorCode.OperationFailure);
            }
        }

        /// <summary>
        /// 确认对应用内商品的购买(vip)
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="productId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> ProductsAcknowledge(string accessToken, string productId, string token, string product)
        {
            bool result = false;
            try
            {
                if (product != "CD")
                {
                    result = await ProductsAcknowledge(accessToken, productId, token);
                }
                else
                {
                    var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/ProductsAcknowledge?accessToken={accessToken}&productId={productId}&token={token}";
                    var responseStr = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                    if (!string.IsNullOrEmpty(responseStr))
                    {
                        result = bool.Parse(responseStr);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ProductsAcknowledge:{ex.Message}-->{ex.StackTrace},请求参数：accessToken={accessToken},productId={productId},token={token}");
                throw;
            }
        }

        /// <summary>
        /// 消费购买应用内商品(钻石)
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="productId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> ProductsConsume(string accessToken, string productId, string token, string product)
        {
            bool result = false;
            try
            {
                if (product != "CD")
                {
                    result = await ProductsAcknowledge(accessToken, productId, token);
                }
                else
                {
                    var GoogleGeocodeUrl = $"{googleConfig.GoogleProxyHostUrl}/Notify/ProductsConsume?accessToken={accessToken}&productId={productId}&token={token}";
                    var responseStr = await GetHttpResponseAsync(HttpMethod.Get, GoogleGeocodeUrl, "");
                    if (!string.IsNullOrEmpty(responseStr))
                    {
                        result = bool.Parse(responseStr);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ProductsConsume:{ex.Message}-->{ex.StackTrace},请求参数：accessToken={accessToken},productId={productId},token={token}");
                throw;
            }
        }
        #endregion

        #region 发送http请求        
        /// <summary>
        /// 发送http请求
        /// </summary>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <param name="paramJson"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private async Task<string> GetHttpResponseAsync(HttpMethod method, string url, string token, string paramJson = null, string contentType = "application/json")
        {
            string responseStr = null;
            var client = _httpClientFactoryFactory.CreateClient();
            //client.Timeout = TimeSpan.FromSeconds(timeOut);
            try
            {
                //添加请求头
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }
                var requestMessage = new HttpRequestMessage(method, url);
                if (!string.IsNullOrEmpty(paramJson))
                {
                    requestMessage.Content = new StringContent(paramJson, Encoding.UTF8, contentType);
                }
                responseStr = await client.SendAsync(requestMessage).Result.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetHttpResponseAsync：{ex.Message}-->{ex.StackTrace},请求参数：method={method},url={url},token={token},paramJson={paramJson},contentType={contentType}");
                //throw;
            }
            finally
            {
                // 关闭 HttpClient
                client?.Dispose();
            }
            return responseStr;
        }
        #endregion
    }
}
