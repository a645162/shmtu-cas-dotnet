using System.Net;
using Flurl.Http;
using HtmlAgilityPack;

// namespace shmtu.cas.auth.common;

namespace shmtu.cas.auth.common
{
    public static class CasAuth
    {
        public static async Task<string> GetExecution(
            string url = "https://cas.shmtu.edu.cn/cas/login",
            string cookie = ""
        )
        {
            try
            {
                var response = await url
                    .WithCookie("Cookie", cookie)
                    .AllowHttpStatus([302])
                    .SendAsync(HttpMethod.Get);

                var responseCode = (HttpStatusCode)response.StatusCode;

                if (responseCode == HttpStatusCode.OK)
                {
                    var htmlCode =
                        await response.ResponseMessage.Content.ReadAsStringAsync();

                    var document = new HtmlDocument();
                    document.LoadHtml(htmlCode);
                    var element =
                        document.DocumentNode.SelectSingleNode("//input[@name='execution']");
                    var value =
                        element?.GetAttributeValue("value", "") ?? "";
                    return value.Trim();
                }

                Console.WriteLine($"Get execution string error:{response.StatusCode}");
                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get execution string error:{ex.Message}");
                return "";
            }
        }

        public static async
            Task<(int, string, string)>
            CasLogin(
                string url,
                string username, string password,
                string validateCode,
                string execution,
                string cookie
            )
        {
            try
            {
                var response = await url
                    .AllowHttpStatus([302])
                    .WithHeaders(new
                    {
                        Host = "cas.shmtu.edu.cn",
                        ContentType = "application/x-www-form-urlencoded",
                        Connection = "keep-alive",
                        AcceptEncoding = "gzip, deflate, br",
                        Accept = "*/*",
                        Cookie = cookie.Trim()
                    })
                    .PostUrlEncodedAsync(new
                    {
                        username = username.Trim(),
                        password = password.Trim(),
                        validateCode = validateCode.Trim(),
                        execution = execution.Trim(),
                        _eventId = "submit",
                        geolocation = ""
                    });

                var responseCode = (HttpStatusCode)response.StatusCode;

                if (responseCode == HttpStatusCode.Redirect)
                {
                    var location =
                        response.ResponseMessage
                            .Headers
                            .GetValues("Location").FirstOrDefault() ?? "";
                    var newCookie =
                        response.ResponseMessage
                            .Headers
                            .GetValues("Set-Cookie").FirstOrDefault() ?? "";

                    return (response.StatusCode, location, newCookie);
                }

                var htmlCode = await response.ResponseMessage.Content.ReadAsStringAsync();
                var document = new HtmlDocument();
                document.LoadHtml(htmlCode);
                var element =
                    document.DocumentNode.SelectSingleNode("#loginErrorsPanel");
                var errorText = element?.InnerText ?? "";
                Console.WriteLine($"登录失败，错误信息：{errorText}");

                if (errorText.Contains("account is not recognized"))
                {
                    Console.WriteLine("用户名或密码错误");
                    return ((int)CasAuthStatus.PasswordError, htmlCode, "");
                }

                if (errorText.Contains("reCAPTCHA"))
                {
                    Console.WriteLine("验证码错误");
                    return ((int)CasAuthStatus.ValidateCodeError, htmlCode, "");
                }
                    
                return (response.StatusCode, htmlCode, errorText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (0, "", "");
            }
        }

        public static async
            Task<(int, string, string)>
            CasRedirect(string url, string cookie)
        {
            try
            {
                var response = await url
                    .WithCookie("Cookie", cookie).GetAsync();

                var responseCode = (HttpStatusCode)response.StatusCode;

                if (responseCode == HttpStatusCode.Redirect)
                {
                    var location =
                        response.ResponseMessage
                            .Headers
                            .GetValues("Location").FirstOrDefault() ?? "";
                    var newCookie =
                        response.ResponseMessage
                            .Headers
                            .GetValues("Set-Cookie").FirstOrDefault() ?? "";

                    return (response.StatusCode, location, newCookie);
                }

                Console.WriteLine($"请求失败，状态码：{response.StatusCode}");
                return (response.StatusCode, "", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (CasAuthStatus.Failure.ToInt(), "", "");
            }
        }
    }
}