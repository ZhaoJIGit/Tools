using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Twines.Common;
using Twines.Common.Models.AppleStoreModels;
using Twines.Common.Options;
using Twines.CommonModel.AppleInApp;
using Twines.Domain.Service.IService;
using ZXing;
using ZXing.Aztec.Internal;

namespace Twines.Domain.Service.Service
{
    public class AppleStoreService : IAppleStoreService
    {
        private readonly ILogger<AppleStoreService> _logger;
        private readonly AppleJwtConfigOption _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string PrivateKey;
        private readonly string KeyId;
        private readonly string IssuerId;
        private readonly string BundleId ;
        private readonly string AppstoreAudience;
        private readonly int MaxTokenAge; 
        private readonly bool IsSandBox;
        // https://www.apple.com/certificateauthority/
        // https://www.apple.com/certificateauthority/AppleRootCA-G3.cer
        private const string APPLE_ROOT_CA_G3_THUMBPRINT = "b52cb02fd567e0359fe8fa4d4c41037970fe01b0";
        public AppleStoreService(ILogger<AppleStoreService> logger, IOptions<AppleJwtConfigOption> options, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _config = options.Value;
            _httpClientFactory = httpClientFactory;
            KeyId = options.Value.KeyId;
            IssuerId = options.Value.Issuer;
            BundleId = options.Value.BundleID;
            AppstoreAudience = options.Value.Audience;
            MaxTokenAge = options.Value.Expiration;
            PrivateKey = options.Value.PrivateKey;
            IsSandBox = options.Value.Environment == "Sandbox";
        }

        
        /// <summary>
        /// 获取AppleStoreJwt令牌
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetAppleStoreJwtToken()
        {
            return this.GetToken();
        }
        /// <summary>
        /// 根据transactionId获取交易信息
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="token"></param>
        /// <param name="isSandBox"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<(AppleInAppTransactionInfo, bool)> GetTransactionInfo(string transactionId,string token)
        {
            try
            {
                var result = "";
                var isSuccess = false;
                var resultModel = new AppleInAppTransactionInfo();
                var urlProduction = $"https://api.storekit.itunes.apple.com/inApps/v1/transactions/{transactionId}";
                var urlSandbox = $"https://api.storekit-sandbox.itunes.apple.com/inApps/v1/transactions/{transactionId}";
                var requestUri = IsSandBox ? urlSandbox : urlProduction;
                using (var httpclient = _httpClientFactory.CreateClient())
                {
                    if (token.NotNull()) httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
                    httpclient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentCulture.Name));
                    _logger.LogInformation("开始请求接口");
                    var response = await httpclient.SendAsync(requestMessage);
                    response.EnsureSuccessStatusCode();
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            _logger.LogInformation("令牌过期，重新获取令牌");
                            token = await GetAppleStoreJwtToken();
                            _logger.LogInformation("重新请求接口");
                            return await GetTransactionInfo(transactionId, token);
                        }
                        else
                        {
                            return (new AppleInAppTransactionInfo(),false);
                        }
                    }
                    result = await response.Content.ReadAsStringAsync();
                }

                var responseModel = JsonConvert.DeserializeObject<AppleInAppTransactionSignResponse>(result);
                if (responseModel.SignedTransactionInfo.NotNull())
                {
                    isSuccess = true;
                }
                resultModel = DecodeJWS<AppleInAppTransactionInfo>(responseModel.SignedTransactionInfo);

                return new(resultModel, isSuccess);
                //return await ReadAppleStoreJwtToken<AppleInAppTransactionInfo>(resobj.SignedTransactionInfo);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"请求发生错误，transactionId:{transactionId},token:{token},isSandBox:{IsSandBox}");

                return (new AppleInAppTransactionInfo(), false);
            }

        }
        /// <summary>
        /// 根据orderId获取交易信息
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="token"></param>
        /// <param name="isSandBox"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<AppleInAppOrderInfo> GetOrderInfo(string orderId, string token)
        {
            try
            {
                var result = "";
                var urlProduction = $"https://api.storekit.itunes.apple.com/inApps/v1/lookup/{orderId}";
                var urlSandbox = $"https://api.storekit-sandbox.itunes.apple.com/inApps/v1/lookup/{orderId}";//沙盒环境好像不能用（文档这么写的）
                var requestUri = IsSandBox ? urlSandbox : urlProduction;
                using (var httpclient = _httpClientFactory.CreateClient())
                {
                    if (token.NotNull()) httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
                    httpclient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentCulture.Name));
                    _logger.LogInformation("开始请求接口");
                    var response = await httpclient.SendAsync(requestMessage);
                    response.EnsureSuccessStatusCode();
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            _logger.LogInformation("令牌过期，重新获取令牌");
                            token = await GetAppleStoreJwtToken();
                            _logger.LogInformation("重新请求接口");
                            return await GetOrderInfo(orderId, token);
                        }
                        else
                        {
                            return new AppleInAppOrderInfo();
                        }
                    }
                    result = await response.Content.ReadAsStringAsync();
                }
                return JsonConvert.DeserializeObject<AppleInAppOrderInfo>(result);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"请求发生错误，orderId:{orderId},token:{token},isSandBox:{IsSandBox}");

                return new AppleInAppOrderInfo();
            }

        }
        /// <summary>
        /// 读取接口返回JWS 的payload(弃用)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jws"></param>
        /// <returns></returns>
        public async Task<T> ReadAppleStoreJwtToken<T>(string jws)
        {
            try
            {
                // 分割JWS为Header, Payload, Signature  
                string[] parts = jws.Split('.');
                if (parts.Length != 3)
                {
                    throw new Exception("Invalid JWS format");
                }
                // 将Base64Url编码转换为标准的Base64编码  
                string headerBase64 = parts[0].Replace('-', '+').Replace('_', '/');
                string payloadBase64 = parts[1].Replace('-', '+').Replace('_', '/');
                string signatureBase64 = parts[2]; // Signature不需要替换，因为它是二进制的  

                // 解码Header和Payload  
                //byte[] headerBytes = Convert.FromBase64String(headerBase64);
                var length = payloadBase64.Length;
                var padCount = length % 4;
                if (padCount != 0)
                {
                    payloadBase64 = payloadBase64.PadRight(length + 1, '=');
                }
                byte[] payloadBytes = Convert.FromBase64String(payloadBase64);

                // 解析JSON（这里只是示例，你可能需要更复杂的处理）  
                //string headerJson = Encoding.UTF8.GetString(headerBytes);
                string payloadJson = Encoding.UTF8.GetString(payloadBytes);
                _logger.LogWarning(payloadJson);
                var payloadObj = JsonConvert.DeserializeObject<T>(payloadJson);
                return payloadObj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

        #region 令牌相关
        private ECDsa GetEllipticCurveAlgorithm()
        {
            try
            {
                var privateKey = this.PrivateKey;
                var ss = Convert.FromBase64String(privateKey);
                var aqq = Asn1Object.FromByteArray(ss);
                var sss = Org.BouncyCastle.Security.PrivateKeyFactory.CreateKey(ss);
                var keyParams = (Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)Org.BouncyCastle.Security.PrivateKeyFactory.CreateKey(ss);

                var normalizedEcPoint = keyParams.Parameters.G.Multiply(keyParams.D).Normalize();

                return ECDsa.Create(new ECParameters
                {
                    Curve = ECCurve.CreateFromValue(keyParams.PublicKeyParamSet.Id),
                    D = keyParams.D.ToByteArrayUnsigned(),
                    Q =
            {
                X = normalizedEcPoint.XCoord.GetEncoded(),
                Y = normalizedEcPoint.YCoord.GetEncoded()
            }
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public ECDsaSecurityKey GetEcdsaSecuritKey()
        {
            var signatureAlgorithm = GetEllipticCurveAlgorithm();
            var eCDsaSecurityKey = new ECDsaSecurityKey(signatureAlgorithm)
            {
                KeyId = this.KeyId
            };

            return eCDsaSecurityKey;
        }

        private string GetToken()
        {
            // Reuse previously created token if it hasn't expired.


            // Tokens must expire after at most 1 hour.
            var now = DateTime.Now;
            var expiry = now.AddSeconds(MaxTokenAge);

            ECDsaSecurityKey eCDsaSecurityKey = GetEcdsaSecuritKey();

            var handler = new JsonWebTokenHandler();
            string jwt = handler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = this.IssuerId,
                Audience = this.AppstoreAudience,
                NotBefore = now,
                Expires = expiry,
                IssuedAt = now,
                Claims = new Dictionary<string, object> {
                    { "bid", this.BundleId },
                    { "nonce", Guid.NewGuid().ToString("N") }
                },
                SigningCredentials = new SigningCredentials(eCDsaSecurityKey, SecurityAlgorithms.EcdsaSha256)
            });


            return jwt;
        }
        #endregion
        #region 解析相关
        /// <summary>
        /// Decodes and verifies an object signed by the App Store according to JWS.
        /// See: https://developer.apple.com/documentation/appstoreserverapi/jwstransaction
        /// </summary>
        /// <param name="token"></param>
        public T DecodeJWS<T>(string token)
        {
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(token);

                var x5cList = ((List<object>)jwtSecurityToken.Header["x5c"])?.Select(o => o.ToString()!).ToList()
                    ?? throw new Exception("Header 'x5c' not found.");

                if (x5cList == null)
                {
                    return default(T);
                }

                var certs = ValidateCertificate(x5cList);
                var jsonStr = JsonConvert.SerializeObject(jwtSecurityToken.Payload);
                var result = JsonConvert.DeserializeObject<T>(jsonStr);

                return result;
            }
            catch (Exception ex)
            {
                return default(T);
            }
            
        }

        /// <summary>
        /// Validates a certificate chain provided in the x5c field of a decoded header of a JWS.
        /// The certificates must be valid and have come from Apple.
        /// </summary>
        /// <param name="certificates"></param>
        /// <returns></returns>
        private static List<X509Certificate2> ValidateCertificate(List<string> certificates)
        {
            if (certificates.Count == 0)
                throw new Exception("不合法");

            var x509certs = certificates.Select(c => new X509Certificate2((Convert.FromBase64String(c)))).ToList();

            // Check dates
            var now = DateTime.Now;
            var datesValid = x509certs.All(c => c.NotBefore < now && now < c.NotAfter);
            if (!datesValid)
                throw new Exception("不合法");

            // Check that each certificate, except for the last, is issued by the subsequent one.
            if (certificates.Count >= 2)
            {
                for (var i = 0; i < x509certs.Count - 1; i++)
                {
                    if (x509certs[i].Issuer != x509certs[i + 1].Subject)
                    {
                        throw new Exception("不合法");
                    }
                }
            }

            // Ensure that the last certificate in the chain is the expected Apple root CA.
            if (!x509certs.Last().Thumbprint.Equals(APPLE_ROOT_CA_G3_THUMBPRINT, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception("不合法");
            }

            return x509certs;
        }
        #endregion
    }
}
