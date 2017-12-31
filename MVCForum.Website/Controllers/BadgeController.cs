﻿namespace MvcForum.Web.Controllers
{
    using System;
    using System.Web.Mvc;
    using Core.ExtensionMethods;
    using Core.Interfaces.Services;
    using Core.Interfaces.UnitOfWork;
    using Core.Models.Enums;
    using ViewModels;
    using ViewModels.Badge;

    public partial class BadgeController : BaseController
    {
        private readonly IBadgeService _badgeService;
        private readonly IFavouriteService _favouriteService;
        private readonly IPostService _postService;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="badgeService"> </param>
        /// <param name="loggingService"> </param>
        /// <param name="unitOfWorkManager"> </param>
        /// <param name="postService"> </param>
        /// <param name="membershipService"> </param>
        /// <param name="localizationService"></param>
        /// <param name="roleService"> </param>
        /// <param name="settingsService"> </param>
        /// <param name="favouriteService"></param>
        /// <param name="cacheService"></param>
        public BadgeController(ILoggingService loggingService,
            IUnitOfWorkManager unitOfWorkManager,
            IBadgeService badgeService,
            IPostService postService,
            IMembershipService membershipService,
            ILocalizationService localizationService, IRoleService roleService,
            ISettingsService settingsService, IFavouriteService favouriteService, ICacheService cacheService)
            : base(loggingService, unitOfWorkManager, membershipService, localizationService, roleService,
                settingsService, cacheService)
        {
            _badgeService = badgeService;
            _postService = postService;
            _favouriteService = favouriteService;
        }


        [HttpPost]
        [Authorize]
        public void VoteUpPost(EntityIdViewModel voteUpBadgeViewModel)
        {
            using (var unitOfwork = UnitOfWorkManager.NewUnitOfWork())
            {
                try
                {
                    var loggedOnUser = User.GetMembershipUser(MembershipService, false);
                    var databaseUpdateNeededOne = _badgeService.ProcessBadge(BadgeType.VoteUp, loggedOnUser);
                    if (databaseUpdateNeededOne)
                    {
                        unitOfwork.SaveChanges();
                    }

                    var post = _postService.Get(voteUpBadgeViewModel.Id);
                    var databaseUpdateNeededTwo = _badgeService.ProcessBadge(BadgeType.VoteUp, post.User);
                    if (databaseUpdateNeededTwo)
                    {
                        unitOfwork.SaveChanges();
                    }

                    if (databaseUpdateNeededOne || databaseUpdateNeededTwo)
                    {
                        unitOfwork.Commit();
                    }
                }
                catch (Exception ex)
                {
                    unitOfwork.Rollback();
                    LoggingService.Error(ex);
                }
            }
        }

        [HttpPost]
        [Authorize]
        public void VoteDownPost(EntityIdViewModel voteUpBadgeViewModel)
        {
            using (var unitOfwork = UnitOfWorkManager.NewUnitOfWork())
            {
                try
                {
                    var loggedOnUser = User.GetMembershipUser(MembershipService, false);
                    var databaseUpdateNeededOne = _badgeService.ProcessBadge(BadgeType.VoteDown, loggedOnUser);
                    if (databaseUpdateNeededOne)
                    {
                        unitOfwork.SaveChanges();
                    }

                    var post = _postService.Get(voteUpBadgeViewModel.Id);
                    var databaseUpdateNeededTwo = _badgeService.ProcessBadge(BadgeType.VoteDown, post.User);

                    if (databaseUpdateNeededTwo)
                    {
                        unitOfwork.SaveChanges();
                    }

                    if (databaseUpdateNeededOne || databaseUpdateNeededTwo)
                    {
                        unitOfwork.Commit();
                    }
                }
                catch (Exception ex)
                {
                    unitOfwork.Rollback();
                    LoggingService.Error(ex);
                }
            }
        }

        [HttpPost]
        [Authorize]
        public void Post()
        {
            if (Request.IsAjaxRequest())
            {
                using (var unitOfwork = UnitOfWorkManager.NewUnitOfWork())
                {
                    try
                    {
                        var loggedOnUser = User.GetMembershipUser(MembershipService, false);
                        var databaseUpdateNeeded = _badgeService.ProcessBadge(BadgeType.Post, loggedOnUser);

                        if (databaseUpdateNeeded)
                        {
                            unitOfwork.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        unitOfwork.Rollback();
                        LoggingService.Error(ex);
                    }
                }
            }
        }

        [HttpPost]
        [Authorize]
        public void MarkAsSolution(EntityIdViewModel markAsSolutionBadgeViewModel)
        {
            using (var unitOfwork = UnitOfWorkManager.NewUnitOfWork())
            {
                try
                {
                    var post = _postService.Get(markAsSolutionBadgeViewModel.Id);
                    var databaseUpdateNeeded = _badgeService.ProcessBadge(BadgeType.MarkAsSolution, post.User) |
                                               _badgeService.ProcessBadge(BadgeType.MarkAsSolution, post.Topic.User);

                    if (databaseUpdateNeeded)
                    {
                        unitOfwork.Commit();
                    }
                }
                catch (Exception ex)
                {
                    unitOfwork.Rollback();
                    LoggingService.Error(ex);
                }
            }
        }

        [HttpPost]
        [Authorize]
        public void Favourite(EntityIdViewModel favouriteViewModel)
        {
            using (var unitOfwork = UnitOfWorkManager.NewUnitOfWork())
            {
                try
                {
                    var favourite = _favouriteService.Get(favouriteViewModel.Id);
                    var databaseUpdateNeeded = _badgeService.ProcessBadge(BadgeType.Favourite, favourite.Member) |
                                               _badgeService.ProcessBadge(BadgeType.Favourite, favourite.Post.User);

                    if (databaseUpdateNeeded)
                    {
                        unitOfwork.Commit();
                    }
                }
                catch (Exception ex)
                {
                    unitOfwork.Rollback();
                    LoggingService.Error(ex);
                }
            }
        }

        [HttpPost]
        [Authorize]
        public void ProfileBadgeCheck()
        {
            using (var unitOfwork = UnitOfWorkManager.NewUnitOfWork())
            {
                try
                {
                    var loggedOnUser = User.GetMembershipUser(MembershipService, false);
                    if (loggedOnUser != null)
                    {
                        var databaseUpdateNeeded = _badgeService.ProcessBadge(BadgeType.Profile, loggedOnUser);

                        if (databaseUpdateNeeded)
                        {
                            unitOfwork.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    unitOfwork.Rollback();
                    LoggingService.Error(ex);
                }
            }
        }

        [HttpPost]
        public void Time(EntityIdViewModel timeBadgeViewModel)
        {
            using (var unitOfwork = UnitOfWorkManager.NewUnitOfWork())
            {
                try
                {
                    var user = MembershipService.GetUser(timeBadgeViewModel.Id);
                    var databaseUpdateNeeded = _badgeService.ProcessBadge(BadgeType.Time, user);

                    if (databaseUpdateNeeded)
                    {
                        unitOfwork.Commit();
                    }
                }
                catch (Exception ex)
                {
                    unitOfwork.Rollback();
                    LoggingService.Error(ex);
                }
            }
        }

        public ActionResult AllBadges()
        {
            using (UnitOfWorkManager.NewUnitOfWork())
            {
                var allBadges = _badgeService.GetallBadges();

                // Localise the badge names
                foreach (var item in allBadges)
                {
                    var partialKey = string.Concat("Badge.", item.Name);
                    item.DisplayName = LocalizationService.GetResourceString(string.Concat(partialKey, ".Name"));
                    item.Description = LocalizationService.GetResourceString(string.Concat(partialKey, ".Desc"));
                }

                var badgesListModel = new AllBadgesViewModel
                {
                    AllBadges = allBadges
                };

                return View(badgesListModel);
            }
        }
    }
}