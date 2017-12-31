﻿namespace MvcForum.Web.Controllers.OAuthControllers
{
    using System;
    using System.Collections.Specialized;
    using System.Web.Mvc;
    using System.Web.Security;
    using Areas.Admin.ViewModels;
    using Core.Constants;
    using Core.Interfaces.Services;
    using Core.Interfaces.UnitOfWork;
    using Core.Models.Enums;
    using Core.Utilities;
    using Skybrud.Social.Google;
    using Skybrud.Social.Google.OAuth;
    using ViewModels;
    using ViewModels.Member;

    // Google uses OAuth 2.0 for authentication and communication. In order for users to authenticate with the Google API, 
    // you must specify the ID, secret and redirect URI of your Google app. 
    // You can create a new app at the following URL: https://console.developers.google.com/project

    public partial class GoogleOAuthController : BaseController
    {
        public GoogleOAuthController(ILoggingService loggingService,
            IUnitOfWorkManager unitOfWorkManager,
            IMembershipService membershipService,
            ILocalizationService localizationService,
            IRoleService roleService,
            ISettingsService settingsService,
            ICacheService cacheService)
            : base(loggingService,
                unitOfWorkManager,
                membershipService,
                localizationService,
                roleService,
                settingsService,
                cacheService)
        {
        }

        public string ReturnUrl =>
            string.Concat(SettingsService.GetSettings().ForumUrl.TrimEnd('/'), Url.Action("GoogleLogin"));

        public string Callback { get; private set; }

        public string ContentTypeAlias { get; private set; }

        public string PropertyAlias { get; private set; }

        public string Feature { get; private set; }

        /// <summary>
        ///     Gets the authorizing code from the query string (if specified).
        /// </summary>
        public string AuthCode => Request.QueryString["code"];

        public string AuthState => Request.QueryString["state"];

        public string AuthErrorReason => Request.QueryString["error_reason"];

        public string AuthError => Request.QueryString["error"];

        public string AuthErrorDescription => Request.QueryString["error_description"];

        public ActionResult GoogleLogin()
        {
            var resultMessage = new GenericMessageViewModel();

            Callback = Request.QueryString["callback"];
            ContentTypeAlias = Request.QueryString["contentTypeAlias"];
            PropertyAlias = Request.QueryString["propertyAlias"];
            Feature = Request.QueryString["feature"];

            if (AuthState != null)
            {
                var stateValue = Session["MvcForum_" + AuthState] as NameValueCollection;
                if (stateValue != null)
                {
                    Callback = stateValue["Callback"];
                    ContentTypeAlias = stateValue["ContentTypeAlias"];
                    PropertyAlias = stateValue["PropertyAlias"];
                    Feature = stateValue["Feature"];
                }
            }

            if (string.IsNullOrEmpty(SiteConstants.Instance.GooglePlusAppId) ||
                string.IsNullOrEmpty(SiteConstants.Instance.GooglePlusAppSecret))
            {
                resultMessage.Message = "You need to add the Google app credentials";
                resultMessage.MessageType = GenericMessages.danger;
            }
            else
            {
                // Configure the OAuth client based on the options of the prevalue options
                var client = new GoogleOAuthClient
                {
                    ClientId = SiteConstants.Instance.GooglePlusAppId,
                    ClientSecret = SiteConstants.Instance.GooglePlusAppSecret,
                    RedirectUri = ReturnUrl
                };

                // Session expired?
                if (AuthState != null && Session["MvcForum_" + AuthState] == null)
                {
                    resultMessage.Message = "Session Expired";
                    resultMessage.MessageType = GenericMessages.danger;
                }

                // Check whether an error response was received from Google
                if (AuthError != null)
                {
                    resultMessage.Message = AuthErrorDescription;
                    resultMessage.MessageType = GenericMessages.danger;
                    if (AuthState != null)
                    {
                        Session.Remove("MvcForum_" + AuthState);
                    }
                }

                // Redirect the user to the Google login dialog
                if (AuthCode == null)
                {
                    // Generate a new unique/random state
                    var state = Guid.NewGuid().ToString();

                    // Save the state in the current user session
                    Session["MvcForum_" + state] = new NameValueCollection
                    {
                        {"Callback", Callback},
                        {"ContentTypeAlias", ContentTypeAlias},
                        {"PropertyAlias", PropertyAlias},
                        {"Feature", Feature}
                    };

                    // Declare the scope
                    var scope = new[]
                    {
                        GoogleScopes.OpenId,
                        GoogleScopes.Email,
                        GoogleScopes.Profile
                    };

                    // Construct the authorization URL
                    var url = client.GetAuthorizationUrl(state, scope, GoogleAccessType.Offline,
                        GoogleApprovalPrompt.Force);

                    // Redirect the user
                    return Redirect(url);
                }

                var info = new GoogleAccessTokenResponse();
                try
                {
                    info = client.GetAccessTokenFromAuthorizationCode(AuthCode);
                }
                catch (Exception ex)
                {
                    resultMessage.Message = $"Unable to acquire access token<br/>{ex.Message}";
                    resultMessage.MessageType = GenericMessages.danger;
                }

                try
                {
                    // Initialize the Google service
                    var service = GoogleService.CreateFromRefreshToken(client.ClientIdFull, client.ClientSecret,
                        info.RefreshToken);

                    // Get information about the authenticated user
                    var user = service.GetUserInfo();
                    using (UnitOfWorkManager.NewUnitOfWork())
                    {
                        var userExists = MembershipService.GetUserByEmail(user.Email);

                        if (userExists != null)
                        {
                            // Users already exists, so log them in
                            FormsAuthentication.SetAuthCookie(userExists.UserName, true);
                            resultMessage.Message = LocalizationService.GetResourceString("Members.NowLoggedIn");
                            resultMessage.MessageType = GenericMessages.success;
                            ShowMessage(resultMessage);
                            return RedirectToAction("Index", "Home");
                        }
                        // Not registered already so register them
                        var viewModel = new MemberAddViewModel
                        {
                            Email = user.Email,
                            LoginType = LoginType.Google,
                            Password = StringUtils.RandomString(8),
                            UserName = user.Name,
                            SocialProfileImageUrl = user.Picture,
                            UserAccessToken = info.RefreshToken
                        };

                        // Store the viewModel in TempData - Which we'll use in the register logic
                        TempData[AppConstants.MemberRegisterViewModel] = viewModel;

                        return RedirectToAction("SocialLoginValidator", "Members");
                    }
                }
                catch (Exception ex)
                {
                    resultMessage.Message = $"Unable to get user information<br/>{ex.Message}";
                    resultMessage.MessageType = GenericMessages.danger;
                    LoggingService.Error(ex);
                }
            }

            ShowMessage(resultMessage);
            return RedirectToAction("LogOn", "Members");
        }
    }
}